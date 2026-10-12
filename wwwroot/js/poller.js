(function () {
    var POLL_MS = 1000;
    var TIMEOUT_MS = 4000;
    window.ccsPoll = function (url, onData, onError) {
        var busy = false;
        async function tick() {
            if (busy || document.hidden) return;
            busy = true;
            var controller = new AbortController();
            var timer = setTimeout(function () { controller.abort(); }, TIMEOUT_MS);
            try {
                var res = await fetch(url, { cache: 'no-store', signal: controller.signal });
                if (!res.ok) throw new Error('HTTP ' + res.status);
                onData(await res.json());
            } catch (err) {
                var reason = err && err.name === 'AbortError' ? 'quá thời gian chờ' : err.message;
                onError('Không nhận được dữ liệu từ máy chủ web (' + reason + ').');
            } finally {
                clearTimeout(timer);
                busy = false;
            }
        }

        tick();
        setInterval(tick, POLL_MS);
        document.addEventListener('visibilitychange', function () { if (!document.hidden) tick(); });
    };

    window.ccsFormatNumber = function (value) {
        if (value === null || value === undefined) return '--';
        return Number.isInteger(value) ? String(value) : value.toFixed(1);
    };
})();
