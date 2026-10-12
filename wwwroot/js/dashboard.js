(function () {
    var dataElement = document.getElementById('ccsMachineData');
    if (!dataElement) return;
    var machines = JSON.parse(dataElement.dataset.machines);
    var machineKeys = machines.map(function (m) { return m.key; });
    function camel(text) {
        return text.length === 0 ? text : text[0].toLowerCase() + text.slice(1);
    }

    function toDateInputValue(date) {
        return date.getFullYear() + '-' + String(date.getMonth() + 1).padStart(2, '0') + '-' + String(date.getDate()).padStart(2, '0');
    }

    function setText(id, text) {
        var el = document.getElementById(id);
        if (el) el.textContent = text;
    }

    function setRowAlert(el, on) {
        (el.closest('.param-row') || el).classList.toggle('blink-alert', on);
    }

    function setIconTone(id, tone) {
        var el = document.getElementById(id);
        if (!el) return;
        el.classList.remove('ic-green', 'ic-red', 'ic-gray');
        el.classList.add('ic-' + tone);
    }

    function isErrorTag(errorTag) {
        return !!errorTag && errorTag.good && errorTag.value !== '0';
    }

    function setValueCell(id, tag, errorTag, unit) {
        var el = document.getElementById(id);
        if (!el || !tag) return;
        var isError = isErrorTag(errorTag);
        var hasValue = tag.good && tag.value !== null && tag.value !== undefined;
        el.textContent = hasValue ? tag.value + (unit ? ' ' + unit : '') : '--';
        el.classList.toggle('red', !tag.good || isError);
        setRowAlert(el, isError);
    }

    function setRunStopCell(prefix, tag) {
        var el = document.getElementById(prefix + '-runStop');
        var pill = document.getElementById(prefix + '-status-pill');
        var pillText = document.getElementById(prefix + '-status-text');
        if (!tag || !tag.good) {
            if (el) {
                el.textContent = '--';
                el.classList.remove('green', 'red');
            }
            setIconTone(prefix + '-runStop-icon', 'gray');
            return;
        }
        var isRun = tag.value === '1';
        if (el) {
            el.textContent = isRun ? window.ccsT('RUN') : window.ccsT('STOP');
            el.classList.toggle('green', isRun);
            el.classList.toggle('red', !isRun);
        }
        setIconTone(prefix + '-runStop-icon', isRun ? 'green' : 'red');
        if (pill) {
            pill.classList.toggle('status-run', isRun);
            pill.classList.toggle('status-stop', !isRun);
        }
        if (pillText) pillText.textContent = isRun ? window.ccsT('START') : window.ccsT('STOP');
    }

    function setFaultCell(prefix, tag) {
        var el = document.getElementById(prefix + '-fault');
        if (!el) return;
        if (!tag || !tag.good) {
            el.textContent = '--';
            el.classList.remove('green', 'red');
            setRowAlert(el, false);
            setIconTone(prefix + '-fault-icon', 'gray');
            return;
        }
        var isNormal = tag.value === '0';
        el.textContent = isNormal ? window.ccsT('Normal') : window.ccsT('legendError') + ' (' + tag.value + ')';
        el.classList.toggle('green', isNormal);
        el.classList.toggle('red', !isNormal);
        setRowAlert(el, !isNormal);
        setIconTone(prefix + '-fault-icon', isNormal ? 'green' : 'red');
    }

    function classifyMachine(snapshot, key) {
        var runStop = snapshot[key + 'RunStop'];
        var fault = snapshot[key + 'Fault'];
        if (!runStop || !runStop.good || !fault || !fault.good || fault.value !== '0') return 'error';
        return runStop.value === '1' ? 'running' : 'stopped';
    }

    function formatPercent(count, total) {
        return total <= 0 ? '0.0%' : (count / total * 100).toFixed(1) + '%';
    }

    function updateSummary(snapshot) {
        var counts = { running: 0, stopped: 0, error: 0 };
        machineKeys.forEach(function (key) {
            counts[classifyMachine(snapshot, key)]++;
        });

        var total = machineKeys.length;
        setText('stat-total', String(total));
        Object.keys(counts).forEach(function (status) {
            setText('legend-' + status, String(counts[status]));
            setText('stat-' + status, String(counts[status]));
            setText('stat-' + status + '-pct', formatPercent(counts[status], total));
        });
    }

    function applyParameter(machine, parameter, snapshot) {
        var tag = snapshot[machine.key + parameter.parameterKey];
        if (parameter.kind === 'Status') {
            setRunStopCell(machine.key, tag);
        } else if (parameter.kind === 'Fault') {
            setFaultCell(machine.key, tag);
        } else if (parameter.kind === 'Temperature') {
            setValueCell(machine.key + '-' + camel(parameter.parameterKey), tag, null, parameter.unit);
        } else if (parameter.kind === 'Pressure') {
            var errorParameter = machine.parameters.filter(function (x) {
                return x.kind === 'ErrorFlag' && x.linkedParameterKey === parameter.parameterKey;
            })[0];

            var errorTag = errorParameter ? snapshot[machine.key + errorParameter.parameterKey] : null;
            setValueCell(machine.key + '-' + camel(parameter.parameterKey), tag, errorTag, parameter.unit);
        }
    }

    function applySnapshot(snapshot) {
        if (!snapshot) return;
        machines.forEach(function (machine) {
            machine.parameters.forEach(function (parameter) {
                applyParameter(machine, parameter, snapshot);
            });
        });

        updateSummary(snapshot);
    }

    function initExport() {
        var exportBtn = document.getElementById('exportExcelBtn');
        var fromInput = document.getElementById('exportFrom');
        var toInput = document.getElementById('exportTo');
        if (!exportBtn || !fromInput || !toInput) return;
        var today = new Date();
        var weekAgo = new Date();
        weekAgo.setDate(today.getDate() - 6);
        fromInput.value = toDateInputValue(weekAgo);
        toInput.value = toDateInputValue(today);
        toInput.max = toDateInputValue(today);
        exportBtn.addEventListener('click', function () {
            if (!fromInput.value || !toInput.value) return;
            var query = 'fromDate=' + encodeURIComponent(fromInput.value) + '&toDate=' + encodeURIComponent(toInput.value) +
                machineKeys.map(function (key) { return '&machineKeys=' + encodeURIComponent(key); }).join('');
            window.location.href = '/Home/ExportExcel?' + query;
        });
    }

    function initRealtime() {
        if (!window.signalR) return;
        var connection = new signalR.HubConnectionBuilder()
            .withUrl('/hubs/chiller-realtime')
            .withAutomaticReconnect([0, 1000, 3000, 5000])
            .build();
        connection.on('MachineUpdate', applySnapshot);
        function start() {
            connection.start().catch(function () { setTimeout(start, 5000); });
        }

        start();
    }

    window.ccsKeyWhiteBackground('.ct-photo img, .chiller-photo img');
    initExport();
    initRealtime();
})();
