using ChillerCoolingSystem_CCS_.Monitoring;
using Microsoft.AspNetCore.Mvc;

namespace ChillerCoolingSystem_CCS_.Controllers
{
    /// <summary>Giám sát nhiệt độ máy đùn (Extruder) realtime từ PLC qua Kepware.</summary>
    public class ExtruderController : Controller
    {
        private readonly IExtruderMonitoringService _monitoring;

        public ExtruderController(IExtruderMonitoringService monitoring)
        {
            _monitoring = monitoring;
        }

        [HttpGet("Extruder/{id=1}")]
        public IActionResult Index(string id)
        {
            var extruder = _monitoring.FindExtruder(id);
            return extruder is null
                ? NotFound()
                : Redirect($"/?plant={Uri.EscapeDataString(extruder.PlantKey ?? "extruder")}&id={Uri.EscapeDataString(extruder.Key)}");
        }

        /// <summary>Dữ liệu cho trang polling (mỗi giây một lần).</summary>
        [HttpGet("Extruder/{id}/Data")]
        public async Task<IActionResult> Data(string id, CancellationToken cancellationToken)
        {
            var snapshot = await _monitoring.GetSnapshotAsync(id, cancellationToken);
            if (snapshot is null)
            {
                return NotFound();
            }

            Response.Headers.CacheControl = "no-store";
            return Json(new
            {
                key = snapshot.Key,
                displayName = snapshot.DisplayName,
                runState = snapshot.RunState.ToString(),
                connectionOk = snapshot.ConnectionOk,
                message = snapshot.Message,
                readAt = snapshot.ReadAt.ToString("HH:mm:ss"),
                ngCount = snapshot.NgCount,
                points = snapshot.Points.Select(p => new
                {
                    name = p.Name,
                    standard = p.Standard,
                    tolerance = p.Tolerance,
                    actual = p.Actual,
                    deviation = p.Deviation,
                    result = p.Result.ToString()
                })
            });
        }
    }
}

