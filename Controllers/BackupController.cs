using Microsoft.AspNetCore.Mvc;
using Microfinance.Models; // Asegúrate de que este namespace exista si usas ErrorViewModel
using Microfinance.Services; // ¡Importante! Asegúrate de que el namespace coincida con el de tu CloudSqlService
using System.Diagnostics;


namespace Microfinance.Controllers;

public class BackupController : Controller
{
    private readonly ILogger<BackupController> _logger;
    private readonly CloudSqlService _cloudSqlService;
    
    public BackupController(ILogger<BackupController> logger, CloudSqlService cloudSqlService)
    {
        _logger = logger;
        _cloudSqlService = cloudSqlService;
    }
    
    public IActionResult Index()
    {
        return View();
    }
    
    [HttpPost]
    public async Task<IActionResult> CreateBackup()
    {
        try
        {
            _logger.LogInformation("Attempting to start manual Cloud SQL backup...");
            var operation = await _cloudSqlService.StartManualBackup();
            _logger.LogInformation($"Manual backup operation initiated: {operation.Name}. Status: {operation.Status}");

            ViewBag.Message =
                $"Operación de copia de seguridad manual de Cloud SQL iniciada. ID de Operación: {operation.Name}";
        }
        catch (Google.GoogleApiException gex)
        {
            _logger.LogError(gex, "Error al crear la copia de seguridad manual de Cloud SQL: {Message}", gex.Message);
            ViewBag.ErrorMessage =
                $"Error de Google Cloud: {gex.Message}. Asegúrate de que la instancia exista y las copias de seguridad estén habilitadas para tu instancia y los permisos sean correctos.";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error inesperado al crear la copia de seguridad manual de Cloud SQL.");
            ViewBag.ErrorMessage = $"Error inesperado: {ex.Message}";
        }

        return View("Index");
    }


    [HttpPost]
    public async Task<IActionResult> RestoreDatabase(long? backupRunId)
    {
        try
        {
            long idToRestore;
            if (backupRunId.HasValue && backupRunId.Value > 0)
            {
                idToRestore = backupRunId.Value;
            }
            else
            {
                // Si no se proporciona un ID, intenta restaurar la última copia de seguridad exitosa
                _logger.LogInformation(
                    "No Backup Run ID provided. Attempting to get the latest successful backup run...");
                var latestBackup = await _cloudSqlService.GetLatestBackupRun();
                if (latestBackup == null)
                {
                    ViewBag.ErrorMessage =
                        "No se encontró ninguna copia de seguridad **exitosa** para restaurar. Por favor, crea una copia de seguridad manual o automática primero.";
                    return View("Index");
                }

                idToRestore = latestBackup.Id.Value;
                _logger.LogInformation($"Restoring from latest successful backup with ID: {idToRestore}");
            }

            _logger.LogInformation($"Attempting to restore Cloud SQL database from Backup Run ID: {idToRestore}...");
            var operation = await _cloudSqlService.RestoreDatabase(idToRestore);
            _logger.LogInformation($"Restore operation initiated: {operation.Name}. Status: {operation.Status}");

            ViewBag.Message =
                $"Operación de restauración de base de datos iniciada desde la copia de seguridad '{idToRestore}'. ID de Operación: {operation.Name}";
        }
        catch (Google.GoogleApiException gex)
        {
            _logger.LogError(gex, "Error al restaurar la base de datos de Cloud SQL: {Message}", gex.Message);
            ViewBag.ErrorMessage =
                $"Error de Google Cloud: {gex.Message}. Asegúrate de que la instancia exista y el ID de la copia de seguridad sea válido.";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error inesperado al restaurar la base de datos de Cloud SQL.");
            ViewBag.ErrorMessage = $"Error inesperado: {ex.Message}";
        }

        return View("Index");
    }

    
}