(function () {
    var dictionaries = {
        vi: {
            tempIn: 'Nhiệt độ vào',
            tempOut: 'Nhiệt độ ra',
            tempOutWater: 'Nhiệt độ nước ra',
            tempAmbi: 'Nhiệt độ môi trường',
            preWater: 'Áp lực nước',
            runStop: 'Start - Stop',
            fault: 'Trạng thái',
            systemStatus: 'TRẠNG THÁI HỆ THỐNG',
            legendRunning: 'Hoạt động',
            legendStopped: 'Dừng',
            legendError: 'Lỗi',
            totalMachines: 'TỔNG SỐ MÁY',
            runningLabel: 'ĐANG HOẠT ĐỘNG',
            stoppedLabel: 'ĐANG DỪNG',
            errorLabel: 'ĐANG LỖI',
            RUN: 'Start',
            STOP: 'Stop',
            START: 'Start',
            Normal: 'Bình thường',
            exportTitle: 'Truy xuất dữ liệu',
            exportExcel: 'Xuất Excel',
            exportFrom: 'Từ ngày',
            exportTo: 'Đến ngày'
        },
        en: {
            tempIn: 'Inlet Temp',
            tempOut: 'Outlet Temp',
            tempOutWater: 'Water Outlet Temp',
            tempAmbi: 'Ambient Temp',
            preWater: 'Water Pressure',
            runStop: 'Run - Stop',
            fault: 'Fault',
            systemStatus: 'SYSTEM STATUS',
            legendRunning: 'Running',
            legendStopped: 'Stopped',
            legendError: 'Error',
            totalMachines: 'TOTAL MACHINES',
            runningLabel: 'RUNNING',
            stoppedLabel: 'STOPPED',
            errorLabel: 'ERROR',
            RUN: 'RUN',
            STOP: 'STOP',
            START: 'START',
            Normal: 'Normal',
            exportTitle: 'Data Export',
            exportExcel: 'Export Excel',
            exportFrom: 'From',
            exportTo: 'To'
        }
    };
    var language = 'vi';
    try {
        language = localStorage.getItem('ccsLang') || 'vi';
    } catch (e) { }
    window.ccsT = function (key) {
        var dictionary = dictionaries[language] || dictionaries.vi;
        return key in dictionary ? dictionary[key] : key;
    };

    document.querySelectorAll('[data-i18n]').forEach(function (el) {
        var text = window.ccsT(el.getAttribute('data-i18n'));
        if (el.tagName === 'INPUT' || el.tagName === 'TEXTAREA') {
            el.placeholder = text;
        } else {
            el.textContent = text;
        }
    });
})();
