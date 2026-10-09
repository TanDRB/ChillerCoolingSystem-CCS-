using System.Diagnostics;
using ChillerCoolingSystem_CCS_.Models;
using ChillerCoolingSystem_CCS_.Repositories;
using ChillerCoolingSystem_CCS_.Services;
using Microsoft.AspNetCore.Mvc;

namespace ChillerCoolingSystem_CCS_.Controllers
{
    public class HomeController : Controller
    {
        private readonly IMachineService _machineService;
        private readonly IPlantRepository _plantRepository;
        private readonly IExportService _exportService;
        private readonly ILogger<HomeController> _logger;

        public HomeController(
            IMachineService machineService,
            IPlantRepository plantRepository,
            IExportService exportService,
            ILogger<HomeController> logger)
        {
            _machineService = machineService;
            _plantRepository = plantRepository;
            _exportService = exportService;
            _logger = logger;
        }

        public async Task<IActionResult> Index(string? plant)
        {
            var plants = await _plantRepository.GetActiveAsync();
            var current = plants.FirstOrDefault(p => p.Key == plant) ?? plants.FirstOrDefault();

            if (current == null)
            {
                return NotFound();
            }

            ViewData["Title"] = current.Title ?? "Chiller & Cooler System Overview";
            ViewData["Subtitle"] = current.Name;
            ViewData["Plants"] = plants;
            ViewData["CurrentPlantKey"] = current.Key;

            var zones = await _machineService.GetDashboardZonesAsync(current.Id);
            return View(current.ViewName, zones);
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
