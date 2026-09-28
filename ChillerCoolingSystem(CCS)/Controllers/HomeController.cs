using System.Diagnostics;
using ChillerCoolingSystem_CCS_.Models;
using ChillerCoolingSystem_CCS_.Services;
using Microsoft.AspNetCore.Mvc;

namespace ChillerCoolingSystem_CCS_.Controllers
{
    public class HomeController : Controller
    {
        private readonly IMachineService _machineService;
        private readonly IExportService _exportService;
        private readonly ILogger<HomeController> _logger;

        public HomeController(IMachineService machineService, IExportService exportService, ILogger<HomeController> logger)
        {
            _machineService = machineService;
            _exportService = exportService;
            _logger = logger;
        }

        public async Task<IActionResult> Index()
        {
            var zones = await _machineService.GetDashboardZonesAsync();
            return View(zones);
        }

        [HttpGet]
        public async Task<IActionResult> ExportExcel(string[] machineKeys, DateTime fromDate, DateTime toDate)
        {
            var bytes = await _exportService.ExportMachineDataAsync(machineKeys, fromDate, toDate);
            var fileName = $"du-lieu-may_{DateTime.Now:yyyyMMdd_HHmmss}.xlsx";
            return File(bytes, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", fileName);
        }

        public IActionResult Privacy()
        {
            return View();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}
