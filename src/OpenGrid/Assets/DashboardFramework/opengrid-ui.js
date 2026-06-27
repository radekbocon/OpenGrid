(function () {
    'use strict';

    const link = document.createElement('link');
    link.rel = 'stylesheet';
    link.href = '/framework/opengrid-ui.css';
    document.head.appendChild(link);

    let drawer;
    let isOpen = false;
    let autoHideTimer = null;
    let hasAutoShown = false;

    function buildUI() {
        drawer = document.createElement('div');
        drawer.id = 'og-drawer';

        const fsRow = document.createElement('div');
        fsRow.className = 'og-row';
        fsRow.id = 'og-fs-row';
        fsRow.innerHTML =
            '<svg viewBox="0 0 24 24"><path d="M7 14H5v5h5v-2H7v-3zm-2-4h2V7h3V5H5v5zm12 7h-3v2h5v-5h-2v3zM14 5v2h3v3h2V5h-5z"/></svg>' +
            '<span class="og-lbl" id="og-fs-label">Enter Fullscreen</span>' +
            '<span class="og-keys">F11</span>';

        const rcRow = document.createElement('div');
        rcRow.className = 'og-row';
        rcRow.id = 'og-rc-row';
        rcRow.innerHTML =
            '<svg viewBox="0 0 24 24"><path d="M17.65 6.35C16.2 4.9 14.21 4 12 4c-4.42 0-7.99 3.58-7.99 8s3.57 8 7.99 8c3.73 0 6.84-2.55 7.73-6h-2.08c-.82 2.33-3.04 4-5.65 4-3.31 0-6-2.69-6-6s2.69-6 6-6c1.66 0 3.14.69 4.22 1.78L13 11h7V4l-2.35 2.35z"/></svg>' +
            '<span class="og-lbl">Reconnect</span>';

        drawer.appendChild(fsRow);
        drawer.appendChild(rcRow);

        const statusBar = document.createElement('div');
        statusBar.className = 'og-status';
        statusBar.id = 'og-status-bar';
        statusBar.innerHTML =
            '<span class="og-dot" id="og-dot"></span>' +
            '<span id="og-status-txt">Disconnected</span>';
        drawer.appendChild(statusBar);

        document.body.appendChild(drawer);

        fsRow.addEventListener('click', function (e) {
            e.stopPropagation();
            closeDrawer();
            OpenGrid.toggleFullscreen();
        });

        rcRow.addEventListener('click', function (e) {
            e.stopPropagation();
            closeDrawer();
            OpenGrid.reconnect();
        });

        autoShow();
    }

    function autoShow() {
        if (hasAutoShown) return;
        hasAutoShown = true;
        openDrawer();
        autoHideTimer = setTimeout(function () {
            closeDrawer();
            autoHideTimer = null;
        }, 5000);
    }

    function updateFsLabel() {
        const lbl = document.getElementById('og-fs-label');
        if (!lbl) return;
        lbl.textContent = OpenGrid.isFullscreen() ? 'Exit Fullscreen' : 'Enter Fullscreen';
    }

    function clearAutoHide() {
        if (autoHideTimer) {
            clearTimeout(autoHideTimer);
            autoHideTimer = null;
        }
    }

    function openDrawer() {
        clearAutoHide();
        isOpen = true;
        if (drawer) drawer.classList.add('og-open');
        updateFsLabel();
        OpenGrid.updateStatusUI();
    }

    function closeDrawer() {
        clearAutoHide();
        isOpen = false;
        if (drawer) {
            drawer.classList.remove('og-open');
            drawer.style.transform = '';
        }
    }

    document.addEventListener('click', function (e) {
        if (!drawer) return;
        if (isOpen) {
            if (!drawer.contains(e.target)) {
                closeDrawer();
            }
        } else {
            openDrawer();
        }
    });

    document.addEventListener('keydown', function (e) {
        if (e.key === 'F11') {
            e.preventDefault();
            OpenGrid.toggleFullscreen();
        }
        if (e.key === 'o' || e.key === 'O') {
            if (!e.ctrlKey && !e.metaKey && !e.altKey) {
                e.preventDefault();
                if (isOpen) closeDrawer();
                else openDrawer();
            }
        }
    });

    document.addEventListener('fullscreenchange', updateFsLabel);
    document.addEventListener('webkitfullscreenchange', updateFsLabel);
    document.addEventListener('mozfullscreenchange', updateFsLabel);
    document.addEventListener('MSFullscreenChange', updateFsLabel);

    if (document.body) buildUI();
    else document.addEventListener('DOMContentLoaded', buildUI);
})();
