// Trang cân Extruder: hỏi dữ liệu mới mỗi giây (polling) và cập nhật N1/N2/N3, chiều dài.
(function () {
    'use strict';

    var root = document.getElementById('weightPage');
    if (!root) return;

    var key = root.getAttribute('data-key');
    var POLL_MS = 1000;
    var TIMEOUT_MS = 4000;

    var conn = document.getElementById('wtConn');
    var live = document.getElementById('wtLive');
    var liveText = document.getElementById('wtLiveText');
    var readAt = document.getElementById('wtReadAt');
    var lenEl = document.getElementById('wtLen');
    var cells = [document.getElementById('wtN1'), document.getElementById('wtN2'), document.getElementById('wtN3')];

    function fmt(v) {
        if (v === null || v === undefined) return '--';
        return Number.isInteger(v) ? String(v) : v.toFixed(1);
    }

    function setLive(ok, text) {
        live.setAttribute('data-live', ok ? 'true' : 'false');
        liveText.textContent = text;
    }

    function render(data) {
        (data.weights || []).forEach(function (w, i) {
            var cell = cells[i];
            if (!cell) return;
            cell.textContent = w ? w.weightKg.toFixed(1) : '--';
            cell.title = w ? 'Cân lúc ' + w.at : '';
        });

        lenEl.textContent = fmt(data.lengthMm);
        readAt.textContent = data.readAt || '--:--:--';

        if (data.configured === false) {
            // Trạm đã dựng sẵn giao diện nhưng chưa gắn tag: không phải lỗi kết nối.
            conn.classList.add('d-none');
            live.setAttribute('data-live', 'idle');
            liveText.textContent = 'Chưa có dữ liệu';
            readAt.textContent = '--:--:--';
            return;
        }

        if (data.connectionOk && data.tagOk === false) {
            // Kết nối Kepware tốt nhưng tag chưa có giá trị (PLC chưa có chương trình / tag chưa gán): không phải lỗi mạng.
            conn.classList.add('d-none');
            live.setAttribute('data-live', 'idle');
            liveText.textContent = 'Chờ dữ liệu từ PLC';
        } else if (data.connectionOk) {
            conn.classList.add('d-none');
            setLive(true, 'Trực tiếp');
        } else {
            conn.textContent = data.message || 'Không kết nối được tới PLC / Kepware.';
            conn.classList.remove('d-none');
            setLive(false, 'Mất kết nối PLC');
        }
    }

    function showServerError(message) {
        conn.textContent = message;
        conn.classList.remove('d-none');
        setLive(false, 'Mất kết nối máy chủ');
    }

    var busy = false;

    async function poll() {
        if (busy || document.hidden) return; // không chồng yêu cầu, không tải khi tab bị ẩn
        busy = true;
        var controller = new AbortController();
        var timer = setTimeout(function () { controller.abort(); }, TIMEOUT_MS);
        try {
            var res = await fetch('/Weight/' + encodeURIComponent(key) + '/Data', {
                cache: 'no-store',
                signal: controller.signal
            });
            if (!res.ok) throw new Error('HTTP ' + res.status);
            render(await res.json());
        } catch (err) {
            showServerError('Không nhận được dữ liệu từ máy chủ web (' + (err && err.name === 'AbortError' ? 'quá thời gian chờ' : err.message) + ').');
        } finally {
            clearTimeout(timer);
            busy = false;
        }
    }

    poll();
    setInterval(poll, POLL_MS);
    document.addEventListener('visibilitychange', function () { if (!document.hidden) poll(); });
})();
