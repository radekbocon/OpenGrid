const OpenGrid = (function () {
    'use strict';

    let wsHost = location.hostname;
    let wsPort = location.port;
    let ws = null;
    let reconnectTimer = null;
    let isConnected = false;
    let wakeLock = null;
    let sleepVideo = null;
    let sleepVideoAttempted = false;

    function keepAwake() {
        requestWakeLock();
        startSleepVideo();
    }

    function requestWakeLock() {
        if (!navigator.wakeLock) return;
        if (wakeLock) return;
        navigator.wakeLock.request('screen').then(function (lock) {
            wakeLock = lock;
            console.log('OpenGrid: wake lock ACQUIRED');
            lock.addEventListener('release', function () {
                wakeLock = null;
                if (isConnected) requestWakeLock();
            });
        }).catch(function () {
        });
    }

    function startSleepVideo() {
        if (sleepVideo) return;
        var canvas = document.createElement('canvas');
        canvas.width = 2;
        canvas.height = 2;
        var ctx = canvas.getContext('2d');
        ctx.fillStyle = '#000';
        ctx.fillRect(0, 0, 2, 2);
        var stream = canvas.captureStream();
        sleepVideo = document.createElement('video');
        sleepVideo.srcObject = stream;
        sleepVideo.loop = true;
        sleepVideo.muted = true;
        sleepVideo.playsInline = true;
        sleepVideo.style.cssText = 'position:fixed;bottom:0;left:0;width:2px;height:2px;opacity:0.01;z-index:-1';
        sleepVideo.setAttribute('aria-hidden', 'true');
        document.body.appendChild(sleepVideo);
        tryPlayVideo();
    }

    function tryPlayVideo() {
        if (!sleepVideo || sleepVideoAttempted) return;
        sleepVideo.play().then(function () {
            console.log('OpenGrid: sleep video playing');
            sleepVideoAttempted = true;
        }).catch(function () {
        });
    }

    function stopSleepVideo() {
        if (!sleepVideo) return;
        sleepVideo.pause();
        sleepVideo.srcObject = null;
        try {
            document.body.removeChild(sleepVideo);
        } catch {
        }
        sleepVideo = null;
        sleepVideoAttempted = false;
    }

    function releaseWakeLock() {
        if (wakeLock) {
            wakeLock.release().catch(function () {
            });
            wakeLock = null;
        }
        stopSleepVideo();
    }

    document.addEventListener('click', function () {
        if (isConnected) {
            requestWakeLock();
            tryPlayVideo();
        }
    });
    document.addEventListener('touchstart', function () {
        if (isConnected) {
            requestWakeLock();
            tryPlayVideo();
        }
    });
    document.addEventListener('keydown', function () {
        if (isConnected) {
            requestWakeLock();
            tryPlayVideo();
        }
    });

    function buildWsUrl(host, port) {
        return port ? 'ws://' + host + ':' + port + '/ws/telemetry' : 'ws://' + host + '/ws/telemetry';
    }

    function connectWs(host, p) {
        if (ws) {
            ws.onclose = null;
            ws.close();
        }

        try {
            ws = new WebSocket(buildWsUrl(host, p));
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
            keepAwake();
            if (OpenGrid.onConnected) OpenGrid.onConnected();
            updateStatusUI();
        };

        ws.onmessage = function (e) {
            try {
                const data = JSON.parse(e.data);
                if (OpenGrid.onTelemetry) OpenGrid.onTelemetry(data);
            } catch (err) {
                console.error('OpenGrid: failed to parse telemetry', err);
            }
        };

        ws.onerror = function () {
            isConnected = false;
            releaseWakeLock();
            if (OpenGrid.onError) OpenGrid.onError();
            updateStatusUI();
        };

        ws.onclose = function () {
            isConnected = false;
            releaseWakeLock();
            if (OpenGrid.onDisconnected) OpenGrid.onDisconnected();
            updateStatusUI();
            scheduleReconnect(host, p);
        };
    }

    function updateStatusUI() {
        const dot = document.getElementById('og-dot');
        const txt = document.getElementById('og-status-txt');
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
        try {
            invokeCSharpAction("ToggleFullscreen");
        }
        catch{
            // ignored
        }
        if (isFullscreen()) exitFullscreen();
        else requestFullscreen();
    }

    document.addEventListener('visibilitychange', function () {
        if (document.visibilityState === 'visible' && isConnected) {
            keepAwake();
        }
    });

    function connect(options) {
        options = options || {};
        if (options.host) wsHost = options.host;
        if (options.port) wsPort = options.port;
        connectWs(wsHost, wsPort);
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

    function reconnect() {
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
        connectWs(wsHost, wsPort);
    }

    function fetchApi(path, callback) {
        const xhr = new XMLHttpRequest();
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
        reconnect: reconnect,
        fetchApi: fetchApi,
        toggleFullscreen: toggleFullscreen,
        isFullscreen: isFullscreen,
        updateStatusUI: updateStatusUI,
        onTelemetry: null,
        onConnected: null,
        onDisconnected: null,
        onError: null
    };
})();
