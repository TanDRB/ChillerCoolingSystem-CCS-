(function () {
    var root = document.getElementById('weightPage');
    if (!root) return;
    var conn = document.getElementById('wtConn');
    var live = document.getElementById('wtLive');
    var liveText = document.getElementById('wtLiveText');
    var readAt = document.getElementById('wtReadAt');
    var lengthCell = document.getElementById('wtLen');
    var weightCells = [document.getElementById('wtN1'), document.getElementById('wtN2'), document.getElementById('wtN3')];
    function setLive(mode, text) {
        live.setAttribute('data-live', mode);
        liveText.textContent = text;
    }

    function showConnectionError(message) {
        conn.textContent = message;
        conn.classList.remove('d-none');
    }

    function render(data) {
        (data.weights || []).forEach(function (weight, i) {
            weightCells[i].textContent = weight ? weight.weightKg.toFixed(1) : '--';
            weightCells[i].title = weight ? 'Cân lúc ' + weight.at : '';
        });

        lengthCell.textContent = window.ccsFormatNumber(data.lengthMm);
        readAt.textContent = data.readAt || '--:--:--';
        conn.classList.add('d-none');
        if (data.configured === false) {
            readAt.textContent = '--:--:--';
            setLive('idle', 'Chưa có dữ liệu');
        } else if (!data.connectionOk) {
            showConnectionError(data.message || 'Không kết nối được tới PLC / Kepware.');
            setLive('false', 'Mất kết nối PLC');
        } else if (data.tagOk === false) {
            setLive('idle', 'Chờ dữ liệu từ PLC');
        } else {
            setLive('true', 'Trực tiếp');
        }
    }

    function showServerError(message) {
        showConnectionError(message);
        setLive('false', 'Mất kết nối máy chủ');
    }

    window.ccsPoll('/Weight/' + encodeURIComponent(root.getAttribute('data-key')) + '/Data', render, showServerError);
})();
