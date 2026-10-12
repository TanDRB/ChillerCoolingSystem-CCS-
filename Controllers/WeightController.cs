using ChillerCoolingSystem_CCS_.Monitoring;
using Microsoft.AspNetCore.Mvc;

namespace ChillerCoolingSystem_CCS_.Controllers
{
    public class WeightController : Controller
    {
        private readonly IWeighingService _weighing;

        public WeightController(IWeighingService weighing)
        {
            _weighing = weighing;
        }

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
                configured = snapshot.Configured,
                tagOk = snapshot.TagOk,
                connectionOk = snapshot.ConnectionOk,
                message = snapshot.Message,
                readAt = snapshot.ReadAt.ToString("HH:mm:ss"),
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
