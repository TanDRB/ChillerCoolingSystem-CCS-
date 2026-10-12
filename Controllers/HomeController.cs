using System.Diagnostics;
using ChillerCoolingSystem_CCS_.Models;
using ChillerCoolingSystem_CCS_.Repositories;
using ChillerCoolingSystem_CCS_.Services;
using Microsoft.AspNetCore.Mvc;

namespace ChillerCoolingSystem_CCS_.Controllers
{
    public class HomeController : Controller
    {
        private const string DefaultTitle = "Chiller & Cooler System Overview";
        private const string ExcelContentType = "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet";

        private readonly IMachineService _machineService;
        private readonly IPlantRepository _plantRepository;
        private readonly IExportService _exportService;

        public HomeController(
            IMachineService machineService,
            IPlantRepository plantRepository,
            IExportService exportService)
        {
            _machineService = machineService;
            _plantRepository = plantRepository;
            _exportService = exportService;
        }

        public async Task<IActionResult> Index(string? plant)
        {
            var plants = await _plantRepository.GetActiveAsync();
            var current = plants.FirstOrDefault(p => p.Key == plant) ?? plants.FirstOrDefault();
            if (current == null)
            {
                return NotFound();
            }
            ViewData["Title"] = current.Title ?? DefaultTitle;
            ViewData["Plants"] = plants;
            ViewData["CurrentPlantKey"] = current.Key;
            var zones = await _machineService.GetDashboardZonesAsync(current.Id);
            return View(current.ViewName, zones);
        }

        [HttpGet]
        public async Task<IActionResult> ExportExcel(string[] machineKeys, DateTime fromDate, DateTime toDate)
        {
            var bytes = await _exportService.ExportMachineDataAsync(machineKeys, fromDate, toDate);
            return File(bytes, ExcelContentType, $"du-lieu-may_{DateTime.Now:yyyyMMdd_HHmmss}.xlsx");
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}
