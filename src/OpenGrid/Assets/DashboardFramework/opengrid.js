var OpenGrid = (function () {
    'use strict';

    const port = 8190;
    const hostName = 'localhost';
    let ws = null;
    let reconnectTimer = null;
    let isConnected = false;

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
                const data = JSON.parse(e.data);
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
        if (isFullscreen()) exitFullscreen();
        else requestFullscreen();
    }

    function connect(options) {
        options = options || {};
        const p = options.port || port;
        const h = options.host || location.hostname;
        connectWs(h, p);
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
        connectWs(hostName, port);
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
        onTelemetry: null,
        onConnected: null,
        onDisconnected: null,
        onError: null
    };
})();
