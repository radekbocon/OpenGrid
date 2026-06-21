(function () {
    'use strict';

    const style = document.createElement('style');
    style.textContent =
        '#og-handle{position:fixed;top:0;left:50%;z-index:9999;width:72px;height:10px;margin-left:-36px;background:rgba(255,255,255,0.12);border-radius:0 0 4px 4px;cursor:pointer;transition:background .15s,height .15s;touch-action:none}' +
        '#og-handle:hover{background:rgba(255,255,255,0.3);height:14px}' +
        '#og-handle::after{content:"";position:absolute;top:-16px;left:-30px;right:-30px;bottom:-16px}' +
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

    let drawer, handle;
    let isOpen = false;
    let touchStartY = 0;
    let isDragging = false;
    let autoHideTimer = null;
    let hasAutoShown = false;

    function buildUI() {
        handle = document.createElement('div');
        handle.id = 'og-handle';

        drawer = document.createElement('div');
        drawer.id = 'og-drawer';

        const bar = document.createElement('div');
        bar.className = 'og-bar';
        drawer.appendChild(bar);

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

        document.body.appendChild(handle);
        document.body.appendChild(drawer);

        handle.addEventListener('click', function () {
            if (isOpen) closeDrawer();
            else openDrawer();
        });

        fsRow.addEventListener('click', function () {
            closeDrawer();
            OpenGrid.toggleFullscreen();
        });

        rcRow.addEventListener('click', function () {
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

    document.addEventListener('touchstart', function (e) {
        if (!drawer) return;
        if (e.touches.length !== 1) return;
        touchStartY = e.touches[0].clientY;
        isDragging = false;
        drawer.style.transition = 'none';
    }, { passive: true });

    document.addEventListener('touchmove', function (e) {
        if (!drawer) return;
        const y = e.touches[0].clientY;
        const dy = y - touchStartY;
        if (Math.abs(dy) < 10) return;

        if (!isDragging) {
            isDragging = true;
            if (!isOpen) {
                drawer.style.transform = 'translateY(-100%)';
            }
        }

        if (!isOpen) {
            if (dy > 0) {
                const pct = Math.max(-100, Math.min(0, -100 + (dy / 120) * 100));
                drawer.style.transform = 'translateY(' + pct + '%)';
            }
        } else {
            const pct = Math.min(0, dy);
            drawer.style.transform = 'translateY(' + pct + 'px)';
        }
    }, { passive: true });

    document.addEventListener('touchend', function (e) {
        if (!isDragging || !drawer) return;
        isDragging = false;
        drawer.style.transition = 'transform .2s ease';
        const y = e.changedTouches[0].clientY;
        const dy = y - touchStartY;
        if (!isOpen) {
            if (dy > 60) openDrawer();
            else closeDrawer();
        } else {
            if (dy < -40) closeDrawer();
            else openDrawer();
        }
    }, { passive: true });

    document.addEventListener('click', function (e) {
        if (!isOpen || !drawer || !handle) return;
        if (drawer.contains(e.target) || handle.contains(e.target)) return;
        closeDrawer();
    });

    document.addEventListener('keydown', function (e) {
        if (e.key === 'F11') { e.preventDefault(); OpenGrid.toggleFullscreen(); }
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
