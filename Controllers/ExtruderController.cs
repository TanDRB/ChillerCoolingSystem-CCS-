using ChillerCoolingSystem_CCS_.Monitoring;
using Microsoft.AspNetCore.Mvc;

namespace ChillerCoolingSystem_CCS_.Controllers
{
    public class ExtruderController : Controller
    {
        private readonly IExtruderMonitoringService _monitoring;

        public ExtruderController(IExtruderMonitoringService monitoring)
        {
            _monitoring = monitoring;
        }

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
                runState = snapshot.RunState.ToString(),
                connectionOk = snapshot.ConnectionOk,
                message = snapshot.Message,
                readAt = snapshot.ReadAt.ToString("HH:mm:ss"),
                points = snapshot.Points.Select(p => new
                {
                    actual = p.Actual,
                    deviation = p.Deviation,
                    result = p.Result.ToString()
                })
            });
        }
    }
}
