using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
using System.Linq;
using System.Threading.Tasks;
using Microfinance.Data;
using Microfinance.Enums;
using Microfinance.Models;
using Microfinance.Services;

namespace YourApplication.Controllers
{
    [Authorize]
    public class ReportsController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly ReportService _reportService;

        public ReportsController(ApplicationDbContext context, ReportService reportService)
        {
            _context = context;
            _reportService = reportService;
        }

        // GET: Reports/Index
        public IActionResult Index()
        {
            return View();
        }

        // GET: Reports/LoanWithInstallmentsReport
        [HttpGet]
        public async Task<IActionResult> LoanWithInstallmentsReport(string format = "pdf", int? loanId = null)
        {
            try
            {
                var reportFormat = format.ToLower() == "excel" ? ReportFormat.Excel : ReportFormat.PDF;
                var result = await _reportService.GenerateLoanWithInstallmentsReport(loanId, reportFormat);
                
                return File(result.Content, result.ContentType, result.FileName);
            }
            catch (Exception ex)
            {
                // Log the error (you should implement proper logging)
                TempData["ErrorMessage"] = $"Error al generar el reporte: {ex.Message}";
                return RedirectToAction(nameof(Index));
            }
        }

        //GET: Reports/PaymentsReport
        [HttpGet]
        public async Task<IActionResult> PaymentsReport(DateTime startDate, DateTime endDate, string format = "pdf")
        {
            try
            {
                // Validaciones de fecha
                if (startDate > endDate)
                {
                    TempData["ErrorMessage"] = "La fecha de inicio no puede ser mayor a la fecha final";
                    return RedirectToAction(nameof(Index));
                }

                if (endDate > DateTime.Now)
                {
                    TempData["ErrorMessage"] = "La fecha final no puede ser mayor al día actual";
                    return RedirectToAction(nameof(Index));
                }

                var reportFormat = format.ToLower() == "excel" ? ReportFormat.Excel : ReportFormat.PDF;
                var result = await _reportService.GeneratePaymentsReport(startDate, endDate, reportFormat);
        
                return File(result.Content, result.ContentType, result.FileName);
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = $"Error al generar el reporte de pagos: {ex.Message}";
                return RedirectToAction(nameof(Index));
            }
        }
        
        // GET: Reports/CollectionsReport
        [HttpGet]
        public async Task<IActionResult> CollectionsReport(DateTime? startDate, DateTime? endDate, string format = "pdf")
        {
            try
            {
                // Validaciones de fecha
                if (startDate.HasValue && endDate.HasValue)
                {
                    if (startDate > endDate)
                    {
                        TempData["ErrorMessage"] = "La fecha de inicio no puede ser mayor a la fecha final";
                        return RedirectToAction(nameof(Index));
                    }

                    if (endDate > DateTime.Now)
                    {
                        TempData["ErrorMessage"] = "La fecha final no puede ser mayor al día actual";
                        return RedirectToAction(nameof(Index));
                    }
                }

                var reportFormat = format.ToLower() == "excel" ? ReportFormat.Excel : ReportFormat.PDF;
                var result = await _reportService.GenerateCollectionsReport(startDate, endDate, reportFormat);
        
                return File(result.Content, result.ContentType, result.FileName);
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = $"Error al generar el reporte de cobros: {ex.Message}";
                return RedirectToAction(nameof(Index));
            }
        }
        
        
        // GET: Reports/OverdueInstallmentsReport
        [HttpGet]
        public async Task<IActionResult> OverdueInstallmentsReport(string format = "pdf")
        {
            try
            {
                var reportFormat = format.ToLower() == "excel" ? ReportFormat.Excel : ReportFormat.PDF;
                var result = await _reportService.GenerateOverdueInstallmentsReport(reportFormat);
                
                return File(result.Content, result.ContentType, result.FileName);
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = $"Error al generar el reporte de cuotas vencidas: {ex.Message}";
                return RedirectToAction(nameof(Index));
            }
        }
        
        // GET: Reports/CollectorsPerformanceReport
        [HttpGet]
        public async Task<IActionResult> CollectorsPerformanceReport(string format = "pdf")
        {
            try
            {
                var reportFormat = format.ToLower() == "excel" ? ReportFormat.Excel : ReportFormat.PDF;
                var result = await _reportService.GenerateCollectorsPerformanceReport(reportFormat);
                
                return File(result.Content, result.ContentType, result.FileName);
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = $"Error al generar el reporte de desempeño: {ex.Message}";
                return RedirectToAction(nameof(Index));
            }
        }
        
        // GET: Reports/LoansSummaryReport
        [HttpGet]
        public async Task<IActionResult> LoansSummaryReport(string format = "pdf")
        {
            try
            {
                var reportFormat = format.ToLower() == "excel" ? ReportFormat.Excel : ReportFormat.PDF;
                var result = await _reportService.GenerateLoansSummaryReport(reportFormat);
                
                return File(result.Content, result.ContentType, result.FileName);
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = $"Error al generar el resumen de préstamos: {ex.Message}";
                return RedirectToAction(nameof(Index));
            }
        }
    }
}