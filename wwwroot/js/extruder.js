// Trang giám sát Extruder: hỏi dữ liệu mới mỗi giây (polling) và cập nhật bảng.
(function () {
    'use strict';

    var root = document.getElementById('extruderPage');
    if (!root) return;

    var key = root.getAttribute('data-key');
    var POLL_MS = 1000;
    var TIMEOUT_MS = 4000;

    var rows = document.querySelectorAll('#extRows tr');
    var conn = document.getElementById('extConn');
    var state = document.getElementById('extState');
    var stateText = document.getElementById('extStateText');
    var start = document.getElementById('extStart');
    var startText = document.getElementById('extStartText');
    var live = document.getElementById('extLive');
    var liveText = document.getElementById('extLiveText');
    var readAt = document.getElementById('extReadAt');

    var STATE_TEXT = { Running: 'ĐANG HOẠT ĐỘNG', Stopped: 'ĐÃ DỪNG', Unknown: 'KHÔNG CÓ TÍN HIỆU' };
    var START_TEXT = { Running: 'START', Stopped: 'STOP', Unknown: '--' };
    var STATUS_TEXT = { Ok: 'Bình thường', Ng: 'Vượt ngưỡng', NoData: 'Chưa có dữ liệu' };

    function fmt(v) {
        if (v === null || v === undefined) return '--';
        return Number.isInteger(v) ? String(v) : v.toFixed(1);
    }

    function fmtDev(v) {
        if (v === null || v === undefined) return '--';
        var s = fmt(v);
        return v > 0 ? '+' + s : s;
    }

    function setLive(ok, text) {
        live.setAttribute('data-live', ok ? 'true' : 'false');
        liveText.textContent = text;
    }

    function render(data) {
        var runState = data.runState || 'Unknown';
        state.setAttribute('data-state', runState);
        start.setAttribute('data-state', runState);
        stateText.textContent = STATE_TEXT[runState] || STATE_TEXT.Unknown;
        startText.textContent = START_TEXT[runState] || '--';

        (data.points || []).forEach(function (p, i) {
            var tr = rows[i];
            if (!tr) return;
            tr.setAttribute('data-result', p.result);
            tr.querySelector('.c-actual').textContent = fmt(p.actual);
            tr.querySelector('.c-dev').textContent = fmtDev(p.deviation);
            tr.querySelector('.result-text').textContent = p.result === 'Ok' ? 'OK' : p.result === 'Ng' ? 'NG' : '--';
            tr.querySelector('.status-text').textContent = STATUS_TEXT[p.result] || STATUS_TEXT.NoData;
        });

        readAt.textContent = data.readAt || '--:--:--';

        if (data.connectionOk) {
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
        state.setAttribute('data-state', 'Unknown');
        start.setAttribute('data-state', 'Unknown');
        stateText.textContent = STATE_TEXT.Unknown;
        startText.textContent = START_TEXT.Unknown;
        rows.forEach(function (tr) {
            tr.setAttribute('data-result', 'NoData');
            tr.querySelector('.c-actual').textContent = '--';
            tr.querySelector('.c-dev').textContent = '--';
            tr.querySelector('.result-text').textContent = '--';
            tr.querySelector('.status-text').textContent = STATUS_TEXT.NoData;
        });
    }

    var busy = false;

    async function poll() {
        if (busy || document.hidden) return; // không chồng yêu cầu, không tải khi tab bị ẩn
        busy = true;
        var controller = new AbortController();
        var timer = setTimeout(function () { controller.abort(); }, TIMEOUT_MS);
        try {
            var res = await fetch('/Extruder/' + encodeURIComponent(key) + '/Data', {
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
