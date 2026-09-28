using System.Globalization;
using ChillerCoolingSystem_CCS_.Models.Dto;
using ChillerCoolingSystem_CCS_.Repositories;
using ClosedXML.Excel;

namespace ChillerCoolingSystem_CCS_.Services
{
    public class ExportService : IExportService
    {
        private readonly IHistoryRepository _historyRepository;
        private readonly IMachineService _machineService;

        // File Excel sinh ở server nên nhãn cột tách riêng khỏi CCS_I18N (phía JS).
        private static readonly Dictionary<string, string> ColumnLabelsVi = new()
        {
            ["tempIn"] = "Nhiệt độ vào (°C)",
            ["tempOut"] = "Nhiệt độ ra (°C)",
            ["tempOutWater"] = "Nhiệt độ nước ra (°C)",
            ["tempAmbi"] = "Nhiệt độ môi trường (°C)",
            ["preWater"] = "Áp lực nước (kg/cm²)",
            ["runStop"] = "Run - Stop",
            ["fault"] = "Fault",
        };

        public ExportService(IHistoryRepository historyRepository, IMachineService machineService)
        {
            _historyRepository = historyRepository;
            _machineService = machineService;
        }

        public async Task<byte[]> ExportMachineDataAsync(IEnumerable<string> machineKeys, DateTime fromLocal, DateTime toLocal)
        {
            var keys = machineKeys.ToList();

            // fromLocal/toLocal là ngày chọn trên lịch (giờ local) — lấy trọn
            // ngày rồi đổi UTC để so khớp TimestampUtc lưu trong DB.
            if (fromLocal > toLocal)
            {
                (fromLocal, toLocal) = (toLocal, fromLocal);
            }

            var fromUtc = DateTime.SpecifyKind(fromLocal.Date, DateTimeKind.Local).ToUniversalTime();
            var toUtc = DateTime.SpecifyKind(toLocal.Date.AddDays(1).AddTicks(-1), DateTimeKind.Local).ToUniversalTime();

            var allMachines = await _machineService.GetAllAsync();
            var machines = allMachines.Where(m => keys.Contains(m.Key)).ToList();

            var entries = await _historyRepository.GetHistoryForMachinesAsync(keys, fromUtc, toUtc);
            var alarms = await _historyRepository.GetAlarmsForMachinesAsync(keys, fromUtc, toUtc);

            using var workbook = new XLWorkbook();
            var usedSheetNames = new HashSet<string>();

            foreach (var machine in machines)
            {
                AddMachineSheet(workbook, usedSheetNames, machine, entries.Where(e => e.MachineKey == machine.Key));
            }

            AddAlarmsSheet(workbook, usedSheetNames, machines, alarms);

            using var stream = new MemoryStream();
            workbook.SaveAs(stream);
            return stream.ToArray();
        }

        private static void AddMachineSheet(
            XLWorkbook workbook,
            HashSet<string> usedSheetNames,
            MachineDto machine,
            IEnumerable<Models.Entities.TagHistoryEntry> machineEntries)
        {
            // Cờ ErrorFlag (TempError1/2) chỉ dùng để cảnh báo trên UI, không
            // phải một thông số độc lập nên không xuất thành cột riêng.
            var dataParams = machine.Parameters.Where(p => p.Kind != "ErrorFlag").ToList();

            var sheet = workbook.Worksheets.Add(UniqueSheetName(machine.Name, usedSheetNames));

            sheet.Cell(1, 1).Value = "Thời gian";
            for (var i = 0; i < dataParams.Count; i++)
            {
                sheet.Cell(1, i + 2).Value = Label(dataParams[i]);
            }
            sheet.Row(1).Style.Font.Bold = true;

            var rows = machineEntries
                .GroupBy(e => e.TimestampUtc)
                .OrderBy(g => g.Key)
                .ToList();

            if (rows.Count == 0)
            {
                var emptyCell = sheet.Cell(2, 1);
                emptyCell.Value = "Không có dữ liệu trong khoảng thời gian đã chọn";
                emptyCell.Style.Font.Italic = true;
                if (dataParams.Count > 0)
                {
                    sheet.Range(2, 1, 2, dataParams.Count + 1).Merge();
                }
            }

            var row = 2;
            foreach (var group in rows)
            {
                var timeCell = sheet.Cell(row, 1);
                timeCell.Value = group.Key.ToLocalTime();
                timeCell.Style.DateFormat.Format = "dd/MM/yyyy HH:mm:ss";

                var byTag = group.ToDictionary(e => e.TagName, e => e.Value);

                for (var i = 0; i < dataParams.Count; i++)
                {
                    var p = dataParams[i];
                    byTag.TryGetValue(p.ParameterKey, out var rawValue);
                    SetValueCell(sheet.Cell(row, i + 2), p, rawValue);
                }

                row++;
            }

            sheet.SheetView.FreezeRows(1);
            sheet.Columns().AdjustToContents();
        }

        private static void AddAlarmsSheet(
            XLWorkbook workbook,
            HashSet<string> usedSheetNames,
            List<MachineDto> machines,
            List<Models.Entities.AlarmEvent> alarms)
        {
            var sheet = workbook.Worksheets.Add(UniqueSheetName("Cảnh báo", usedSheetNames));
            var namesByKey = machines.ToDictionary(m => m.Key, m => m.Name);

            sheet.Cell(1, 1).Value = "Máy";
            sheet.Cell(1, 2).Value = "Bắt đầu";
            sheet.Cell(1, 3).Value = "Kết thúc";
            sheet.Cell(1, 4).Value = "Trạng thái";
            sheet.Row(1).Style.Font.Bold = true;

            if (alarms.Count == 0)
            {
                var emptyCell = sheet.Cell(2, 1);
                emptyCell.Value = "Không có cảnh báo trong khoảng thời gian đã chọn";
                emptyCell.Style.Font.Italic = true;
                sheet.Range(2, 1, 2, 4).Merge();
            }

            var row = 2;
            foreach (var alarm in alarms.OrderBy(a => a.StartUtc))
            {
                sheet.Cell(row, 1).Value = namesByKey.TryGetValue(alarm.MachineKey, out var name) ? name : alarm.MachineKey;

                var startCell = sheet.Cell(row, 2);
                startCell.Value = alarm.StartUtc.ToLocalTime();
                startCell.Style.DateFormat.Format = "dd/MM/yyyy HH:mm:ss";

                if (alarm.EndUtc.HasValue)
                {
                    var endCell = sheet.Cell(row, 3);
                    endCell.Value = alarm.EndUtc.Value.ToLocalTime();
                    endCell.Style.DateFormat.Format = "dd/MM/yyyy HH:mm:ss";
                }

                sheet.Cell(row, 4).Value = alarm.EndUtc.HasValue ? "Đã xử lý" : "Đang hoạt động";
                row++;
            }

            sheet.SheetView.FreezeRows(1);
            sheet.Columns().AdjustToContents();
        }

        private static void SetValueCell(IXLCell cell, MachineParameterDto p, string? rawValue)
        {
            if (rawValue == null)
            {
                return;
            }

            if (p.Kind == "Status")
            {
                cell.Value = rawValue == "1" ? "RUN" : "STOP";
                return;
            }

            if (p.Kind == "Fault")
            {
                cell.Value = rawValue == "0" ? "Bình thường" : $"Lỗi ({rawValue})";
                return;
            }

            if (double.TryParse(rawValue, NumberStyles.Float, CultureInfo.InvariantCulture, out var number))
            {
                cell.Value = number;
                return;
            }

            cell.Value = rawValue;
        }

        private static string Label(MachineParameterDto p) =>
            ColumnLabelsVi.TryGetValue(p.LabelKey, out var label) ? label : p.LabelKey;

        // Tên sheet Excel: tối đa 31 ký tự, không chứa \ / * ? [ ] : và không trùng nhau.
        private static string UniqueSheetName(string name, HashSet<string> used)
        {
            var sanitized = new string(name.Where(c => "\\/*?[]:".IndexOf(c) < 0).ToArray()).Trim();
            if (sanitized.Length > 28)
            {
                sanitized = sanitized[..28];
            }
            if (sanitized.Length == 0)
            {
                sanitized = "Sheet";
            }

            var candidate = sanitized;
            var suffix = 1;
            while (!used.Add(candidate))
            {
                candidate = $"{sanitized}_{suffix++}";
            }

            return candidate;
        }
    }
}
