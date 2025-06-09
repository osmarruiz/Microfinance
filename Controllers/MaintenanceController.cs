using Microfinance.Services;
using Microsoft.AspNetCore.Mvc;

namespace Microfinance.Controllers;

public class MaintenanceController : Controller
{
    private readonly ApplicationStatusService _appStatusService;

    public MaintenanceController(ApplicationStatusService appStatusService)
    {
        _appStatusService = appStatusService;
    }

    public IActionResult Index()
    {
        if (!_appStatusService.IsUnderMaintenance)
        {
            return RedirectToAction("Index", "Dashboard");
        }

        ViewBag.MaintenanceMessage = _appStatusService.MaintenanceMessage;
        return View();
    }
}