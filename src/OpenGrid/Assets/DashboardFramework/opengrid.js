var OpenGrid = (function () {
    'use strict';

    var ws = null;
    var port = 8190;
    var hostName = 'localhost';
    var reconnectTimer = null;
    var isConnected = false;

    // --- Top drawer UI ---
    (function initUI() {
        try {
            var style = document.createElement('style');
            style.textContent =
                '#og-handle{position:fixed;top:0;left:50%;z-index:9999;width:48px;height:4px;margin-left:-24px;background:rgba(255,255,255,0.1);border-radius:0 0 3px 3px;cursor:pointer;transition:background .15s,height .15s;touch-action:none}' +
                '#og-handle:hover{background:rgba(255,255,255,0.25);height:6px}' +
                '#og-handle::after{content:"";position:absolute;top:-12px;left:-24px;right:-24px;bottom:-12px}' +
                '#og-drawer{position:fixed;top:0;left:0;right:0;background:#0a0a0a;border-bottom:1px solid #1a1a1a;z-index:9998;transform:translateY(-100%);transition:transform .2s ease;font-family:"Segoe UI",system-ui,sans-serif;font-size:13px;line-height:1;padding:18px 16px 10px;touch-action:none}' +
                '#og-drawer.og-open{transform:translateY(0)}' +
                '#og-drawer .og-bar{width:28px;height:3px;background:rgba(255,255,255,0.15);border-radius:2px;margin:0 auto 12px;display:block}' +
                '#og-drawer .og-row{display:flex;align-items:center;gap:10px;padding:8px 4px;cursor:pointer;color:#888;transition:color .1s,background .1s;border-radius:2px;border-bottom:1px solid #111}' +
                '#og-drawer .og-row:last-child{border-bottom:none}' +
                '#og-drawer .og-row:hover{color:#fff;background:#111}' +
                '#og-drawer .og-row svg{width:18px;height:18px;fill:currentColor;flex-shrink:0}' +
                '#og-drawer .og-row .og-lbl{font-size:13px}' +
                '#og-drawer .og-row .og-keys{font-size:10px;color:#444;margin-left:auto;font-family:"Courier New",monospace}' +
                '#og-drawer .og-status{display:flex;align-items:center;gap:8px;padding:6px 4px;font-size:11px;color:#555;border-top:1px solid #111;margin-top:4px;padding-top:8px}' +
                '#og-drawer .og-dot{width:7px;height:7px;border-radius:50%;background:#444;flex-shrink:0}' +
                '#og-drawer .og-dot.on{background:#22cc44}' +
                '#og-drawer .og-dot.off{background:#e00}';
            document.head.appendChild(style);

            var drawer, handle, isOpen = false;
            var touchStartY = 0, isDragging = false;

            function buildUI() {
                handle = document.createElement('div');
                handle.id = 'og-handle';

                drawer = document.createElement('div');
                drawer.id = 'og-drawer';

                var bar = document.createElement('div');
                bar.className = 'og-bar';
                drawer.appendChild(bar);

                // Fullscreen
                var fsRow = document.createElement('div');
                fsRow.className = 'og-row';
                fsRow.id = 'og-fs-row';
                fsRow.innerHTML =
                    '<svg viewBox="0 0 24 24"><path d="M7 14H5v5h5v-2H7v-3zm-2-4h2V7h3V5H5v5zm12 7h-3v2h5v-5h-2v3zM14 5v2h3v3h2V5h-5z"/></svg>' +
                    '<span class="og-lbl" id="og-fs-label">Enter Fullscreen</span>' +
                    '<span class="og-keys">F11</span>';

                // Reconnect
                var rcRow = document.createElement('div');
                rcRow.className = 'og-row';
                rcRow.id = 'og-rc-row';
                rcRow.innerHTML =
                    '<svg viewBox="0 0 24 24"><path d="M17.65 6.35C16.2 4.9 14.21 4 12 4c-4.42 0-7.99 3.58-7.99 8s3.57 8 7.99 8c3.73 0 6.84-2.55 7.73-6h-2.08c-.82 2.33-3.04 4-5.65 4-3.31 0-6-2.69-6-6s2.69-6 6-6c1.66 0 3.14.69 4.22 1.78L13 11h7V4l-2.35 2.35z"/></svg>' +
                    '<span class="og-lbl">Reconnect</span>';

                drawer.appendChild(fsRow);
                drawer.appendChild(rcRow);

                // Status bar
                var statusBar = document.createElement('div');
                statusBar.className = 'og-status';
                statusBar.id = 'og-status-bar';
                statusBar.innerHTML =
                    '<span class="og-dot" id="og-dot"></span>' +
                    '<span id="og-status-txt">Disconnected</span>';
                drawer.appendChild(statusBar);

                document.body.appendChild(handle);
                document.body.appendChild(drawer);

                handle.addEventListener('click', function () {
                    if (isOpen) closeDrawer();
                    else openDrawer();
                });

                fsRow.addEventListener('click', function () {
                    closeDrawer();
                    toggleFullscreen();
                });

                rcRow.addEventListener('click', function () {
                    closeDrawer();
                    if (ws) {
                        if (reconnectTimer) {
                            clearTimeout(reconnectTimer);
                            reconnectTimer = null;
                        }
                        ws.onclose = null;
                        ws.close();
                        ws = null;
                    }
                    isConnected = false;
                    connectWs(hostName, port);
                });
            }

            function updateFsLabel() {
                var lbl = document.getElementById('og-fs-label');
                if (!lbl) return;
                lbl.textContent = isFullscreen() ? 'Exit Fullscreen' : 'Enter Fullscreen';
            }

            function updateStatus() {
                var dot = document.getElementById('og-dot');
                var txt = document.getElementById('og-status-txt');
                if (!dot || !txt) return;
                if (isConnected) {
                    dot.className = 'og-dot on';
                    txt.textContent = 'Connected';
                } else {
                    dot.className = 'og-dot off';
                    txt.textContent = 'Disconnected';
                }
            }

            function openDrawer() {
                isOpen = true;
                if (drawer) drawer.classList.add('og-open');
                updateFsLabel();
                updateStatus();
            }

            function closeDrawer() {
                isOpen = false;
                if (drawer) {
                    drawer.classList.remove('og-open');
                    drawer.style.transform = '';
                }
            }

            // Touch: swipe from top
            document.addEventListener('touchstart', function (e) {
                if (!drawer) return;
                if (e.touches.length !== 1) return;
                var y = e.touches[0].clientY;
                if (y > 50 && !isOpen) return;
                touchStartY = y;
                isDragging = true;
                drawer.style.transition = 'none';
                if (!isOpen) {
                    drawer.style.transform = 'translateY(-100%)';
                }
            }, { passive: true });

            document.addEventListener('touchmove', function (e) {
                if (!isDragging || !drawer) return;
                var y = e.touches[0].clientY;
                var dy = y - touchStartY;
                if (!isOpen) {
                    var pct = Math.max(-100, Math.min(0, -100 + (dy / 120) * 100));
                    drawer.style.transform = 'translateY(' + pct + '%)';
                } else {
                    var pct = Math.min(0, dy);
                    drawer.style.transform = 'translateY(' + pct + 'px)';
                }
            }, { passive: true });

            document.addEventListener('touchend', function (e) {
                if (!isDragging || !drawer) return;
                isDragging = false;
                drawer.style.transition = 'transform .2s ease';
                var y = e.changedTouches[0].clientY;
                var dy = y - touchStartY;
                if (!isOpen) {
                    if (dy > 60) openDrawer();
                    else closeDrawer();
                } else {
                    if (dy < -40) closeDrawer();
                    else openDrawer();
                }
            }, { passive: true });

            // Close on click outside when open
            document.addEventListener('click', function (e) {
                if (!isOpen || !drawer || !handle) return;
                if (drawer.contains(e.target) || handle.contains(e.target)) return;
                closeDrawer();
            });

            document.addEventListener('keydown', function (e) {
                if (e.key === 'F11') { e.preventDefault(); toggleFullscreen(); }
            });

            document.addEventListener('fullscreenchange', updateFsLabel);
            document.addEventListener('webkitfullscreenchange', updateFsLabel);
            document.addEventListener('mozfullscreenchange', updateFsLabel);
            document.addEventListener('MSFullscreenChange', updateFsLabel);

            if (document.body) buildUI();
            else document.addEventListener('DOMContentLoaded', buildUI);

        } catch (e) {
            console.error('OpenGrid: UI init failed', e);
        }
    })();

    function isFullscreen() {
        return !!(document.fullscreenElement ||
            document.webkitFullscreenElement ||
            document.mozFullScreenElement ||
            document.msFullscreenElement);
    }

    function requestFullscreen(el) {
        el = el || document.documentElement;
        if (el.requestFullscreen) return el.requestFullscreen();
        if (el.webkitRequestFullscreen) return el.webkitRequestFullscreen();
        if (el.mozRequestFullScreen) return el.mozRequestFullScreen();
        if (el.msRequestFullscreen) return el.msRequestFullscreen();
    }

    function exitFullscreen() {
        if (document.exitFullscreen) return document.exitFullscreen();
        if (document.webkitExitFullscreen) return document.webkitExitFullscreen();
        if (document.mozCancelFullScreen) return document.mozCancelFullScreen();
        if (document.msExitFullscreen) return document.msExitFullscreen();
    }

    function toggleFullscreen() {
        if (isFullscreen()) exitFullscreen();
        else requestFullscreen();
    }

    // --- WebSocket ---
    function connect(options) {
        options = options || {};
        port = options.port || port;
        hostName = options.host || location.hostname;
        connectWs(hostName, port);
    }

    function connectWs(host, p) {
        if (ws) {
            ws.onclose = null;
            ws.close();
        }

        try {
            ws = new WebSocket('ws://' + host + ':' + p + '/ws/telemetry');
        } catch (e) {
            scheduleReconnect(host, p);
            return;
        }

        ws.onopen = function () {
            isConnected = true;
            if (reconnectTimer) {
                clearTimeout(reconnectTimer);
                reconnectTimer = null;
            }
            if (OpenGrid.onConnected) OpenGrid.onConnected();
            updateStatusUI();
        };

        ws.onmessage = function (e) {
            try {
                var data = JSON.parse(e.data);
                if (OpenGrid.onTelemetry) OpenGrid.onTelemetry(data);
            } catch (err) {
                console.error('OpenGrid: failed to parse telemetry', err);
            }
        };

        ws.onerror = function () {
            isConnected = false;
            if (OpenGrid.onError) OpenGrid.onError();
            updateStatusUI();
        };

        ws.onclose = function () {
            isConnected = false;
            if (OpenGrid.onDisconnected) OpenGrid.onDisconnected();
            updateStatusUI();
            scheduleReconnect(host, p);
        };
    }

    function updateStatusUI() {
        var dot = document.getElementById('og-dot');
        var txt = document.getElementById('og-status-txt');
        if (!dot || !txt) return;
        if (isConnected) {
            dot.className = 'og-dot on';
            txt.textContent = 'Connected';
        } else {
            dot.className = 'og-dot off';
            txt.textContent = 'Disconnected';
        }
    }

    function scheduleReconnect(host, p) {
        if (reconnectTimer) return;
        reconnectTimer = setTimeout(function () {
            reconnectTimer = null;
            connectWs(host, p);
        }, 2000);
    }

    function disconnect() {
        if (reconnectTimer) {
            clearTimeout(reconnectTimer);
            reconnectTimer = null;
        }
        if (ws) {
            ws.onclose = null;
            ws.close();
            ws = null;
        }
        isConnected = false;
    }

    function fetchApi(path, callback) {
        var xhr = new XMLHttpRequest();
        xhr.open('GET', '/api/' + path, true);
        xhr.onload = function () {
            if (xhr.status === 200 && callback) {
                callback(JSON.parse(xhr.responseText));
            }
        };
        xhr.send();
    }

    return {
        connect: connect,
        disconnect: disconnect,
        fetchApi: fetchApi,
        toggleFullscreen: toggleFullscreen,
        isFullscreen: isFullscreen,
        onTelemetry: null,
        onConnected: null,
        onDisconnected: null,
        onError: null
    };
})();
