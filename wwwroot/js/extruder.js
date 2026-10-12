(function () {
    var root = document.getElementById('extruderPage');
    if (!root) return;
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
    var RESULT_TEXT = { Ok: 'OK', Ng: 'NG', NoData: '--' };
    function formatDeviation(value) {
        var text = window.ccsFormatNumber(value);
        return value > 0 ? '+' + text : text;
    }

    function setLive(ok, text) {
        live.setAttribute('data-live', ok ? 'true' : 'false');
        liveText.textContent = text;
    }

    function setRunState(runState) {
        state.setAttribute('data-state', runState);
        start.setAttribute('data-state', runState);
        stateText.textContent = STATE_TEXT[runState];
        startText.textContent = START_TEXT[runState];
    }

    function setRow(tr, result, actual, deviation) {
        tr.setAttribute('data-result', result);
        tr.querySelector('.c-actual').textContent = window.ccsFormatNumber(actual);
        tr.querySelector('.c-dev').textContent = formatDeviation(deviation);
        tr.querySelector('.result-text').textContent = RESULT_TEXT[result];
        tr.querySelector('.status-text').textContent = STATUS_TEXT[result];
    }

    function render(data) {
        setRunState(data.runState || 'Unknown');
        (data.points || []).forEach(function (point, i) {
            if (rows[i]) setRow(rows[i], point.result, point.actual, point.deviation);
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
        setRunState('Unknown');
        rows.forEach(function (tr) { setRow(tr, 'NoData', null, null); });
    }

    window.ccsPoll('/Extruder/' + encodeURIComponent(root.getAttribute('data-key')) + '/Data', render, showServerError);
})();
