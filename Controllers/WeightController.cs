using ChillerCoolingSystem_CCS_.Monitoring;
using Microsoft.AspNetCore.Mvc;

namespace ChillerCoolingSystem_CCS_.Controllers
{
    /// <summary>Trạm cân của máy Extruder (trọng lượng N1/N2/N3, chiều dài).</summary>
    public class WeightController : Controller
    {
        private readonly IWeighingService _weighing;

        public WeightController(IWeighingService weighing)
        {
            _weighing = weighing;
        }

        [HttpGet("Weight/{id}")]
        public IActionResult Index(string id)
        {
            var station = _weighing.FindStation(id);
            return station is null
                ? NotFound()
                : Redirect($"/?plant={Uri.EscapeDataString(station.PlantKey ?? "")}&id={Uri.EscapeDataString(station.Key)}");
        }

        /// <summary>Dữ liệu cho trang polling (mỗi giây một lần).</summary>
        [HttpGet("Weight/{id}/Data")]
        public IActionResult Data(string id)
        {
            var snapshot = _weighing.GetSnapshot(id);
            if (snapshot is null)
            {
                return NotFound();
            }

            Response.Headers.CacheControl = "no-store";
            return Json(new
            {
                key = snapshot.Key,
                displayName = snapshot.DisplayName,
                configured = snapshot.Configured,
                tagOk = snapshot.TagOk,
                connectionOk = snapshot.ConnectionOk,
                message = snapshot.Message,
                readAt = snapshot.ReadAt.ToString("HH:mm:ss"),
                liveWeight = snapshot.LiveWeight,
                lengthMm = snapshot.LengthMm,
                weights = snapshot.Weights.Select(w => w is null ? null : new
                {
                    weightKg = (double?)w.WeightKg,
                    at = (string?)w.RecordedAt.ToString("HH:mm:ss dd/MM")
                })
            });
        }
    }
}
