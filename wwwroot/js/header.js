(function () {
    var RELOAD_AFTER_MS = 6 * 60 * 60 * 1000;
    var FULLSCREEN_KEY = 'ccsWantFullscreen';
    var RESTORE_EVENTS = ['pointerdown', 'keydown', 'touchstart'];
    var unloading = false;
    function rememberFullscreen(on) {
        try {
            if (on) localStorage.setItem(FULLSCREEN_KEY, '1');
            else localStorage.removeItem(FULLSCREEN_KEY);
        } catch (e) { }
    }

    function wantsFullscreen() {
        try {
            return localStorage.getItem(FULLSCREEN_KEY) === '1';
        } catch (e) {
            return false;
        }
    }

    function requestFullscreen() {
        return document.documentElement.requestFullscreen().catch(function () { });
    }

    function initPlantDropdown() {
        var button = document.getElementById('plantDdBtn');
        var list = document.getElementById('plantDdList');
        if (!button || !list) return;
        function setOpen(open) {
            list.hidden = !open;
            button.setAttribute('aria-expanded', open ? 'true' : 'false');
        }

        button.addEventListener('click', function (e) {
            e.stopPropagation();
            setOpen(list.hidden);
        });

        document.addEventListener('click', function (e) {
            if (!list.contains(e.target)) setOpen(false);
        });

        document.addEventListener('keydown', function (e) {
            if (e.key === 'Escape' && !list.hidden) {
                setOpen(false);
                button.focus();
            }
        });
    }

    // Trình duyệt luôn thoát toàn màn hình khi tải trang khác (vd. đổi xưởng): ghi nhớ rồi thử vào lại.
    function restoreFullscreen() {
        if (!wantsFullscreen() || document.fullscreenElement) return;
        var hint = null;
        function removeHint() {
            if (hint) {
                hint.remove();
                hint = null;
            }
        }

        function onGesture() {
            RESTORE_EVENTS.forEach(function (name) { document.removeEventListener(name, onGesture, true); });
            removeHint();
            if (!document.fullscreenElement) requestFullscreen();
        }

        document.documentElement.requestFullscreen().catch(function () {
            RESTORE_EVENTS.forEach(function (name) { document.addEventListener(name, onGesture, true); });
            hint = document.createElement('div');
            hint.className = 'fs-restore-hint';
            hint.textContent = 'Chạm vào màn hình để trở lại toàn màn hình';
            document.body.appendChild(hint);
            setTimeout(removeHint, 8000);
        });
    }

    function initFullscreen() {
        var button = document.getElementById('fullscreenBtn');
        if (!button) return;
        var expandIcon = document.getElementById('fsExpandIcon');
        var compressIcon = document.getElementById('fsCompressIcon');
        window.addEventListener('beforeunload', function () { unloading = true; });
        window.addEventListener('pagehide', function () { unloading = true; });
        button.addEventListener('click', function () {
            if (!document.fullscreenElement) {
                requestFullscreen();
            } else {
                rememberFullscreen(false);
                document.exitFullscreen().catch(function () { });
            }
        });

        document.addEventListener('fullscreenchange', function () {
            var isFullscreen = !!document.fullscreenElement;
            expandIcon.style.display = isFullscreen ? 'none' : '';
            compressIcon.style.display = isFullscreen ? '' : 'none';
            button.title = isFullscreen ? 'Thoát toàn màn hình' : 'Toàn màn hình';
            if (isFullscreen) rememberFullscreen(true);
            else if (!unloading) rememberFullscreen(false);
        });

        restoreFullscreen();
    }

    initPlantDropdown();
    initFullscreen();
    setTimeout(function () { location.reload(); }, RELOAD_AFTER_MS);
})();
