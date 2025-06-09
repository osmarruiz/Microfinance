using Microsoft.AspNetCore.Mvc;
using Microfinance.Models; // Asegúrate de que este namespace exista si usas ErrorViewModel
using Microfinance.Services; // ¡Importante! Asegúrate de que el namespace coincida con el de tu CloudSqlService
using System.Diagnostics;
using Microsoft.AspNetCore.Authorization;


namespace Microfinance.Controllers;

[Authorize(Roles = "Admin,SuperAdmin")] // Asegúrate de que el usuario tenga los roles necesarios
public class BackupController : Controller
{
    private readonly ILogger<BackupController> _logger;
    private readonly CloudSqlService _cloudSqlService;
    private readonly ApplicationStatusService _appStatusService;
    
    public BackupController(ILogger<BackupController> logger, CloudSqlService cloudSqlService, ApplicationStatusService appStatusService)
    {
        _logger = logger;
        _cloudSqlService = cloudSqlService;
        _appStatusService = appStatusService;
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
                _logger.LogInformation(
                    "No se proporcionó ID de copia de seguridad. Intentando obtener la última copia de seguridad exitosa...");
                var latestBackup = await _cloudSqlService.GetLatestBackupRun();
                if (latestBackup == null)
                {
                    ViewBag.ErrorMessage =
                        "No se encontró ninguna copia de seguridad **exitosa** para restaurar. Por favor, crea una copia de seguridad manual o automática primero.";
                    return View("Index");
                }

                idToRestore = latestBackup.Id.Value;
                _logger.LogInformation($"Restaurando desde la última copia de seguridad exitosa con ID: {idToRestore}");
            }

            _logger.LogInformation($"Intentando restaurar la base de datos de Cloud SQL desde el ID de copia de seguridad: {idToRestore}...");

            // !!! Establecer el modo mantenimiento ANTES de iniciar la operación de restauración !!!
            // Proporciona un mensaje específico para la restauración
            _appStatusService.SetMaintenanceMode(true, null, "Estamos restaurando la base de datos. Esto puede tomar unos minutos.");


            // La operación de RestoreBackup de Cloud SQL en sí es asíncrona.
            // Devuelve un objeto Operation que necesitas sondear para conocer su estado.
            var operation = await _cloudSqlService.RestoreDatabase(idToRestore);
            _logger.LogInformation($"Operación de restauración iniciada: {operation.Name}. Estado: {operation.Status}");

            // Almacena el ID de la operación para que el servicio en segundo plano pueda monitorearlo
            _appStatusService.SetMaintenanceMode(true, operation.Name, "Estamos restaurando la base de datos. Esto puede tomar unos minutos.");


            ViewBag.Message =
                $"Operación de restauración de base de datos iniciada desde la copia de seguridad '{idToRestore}'. ID de Operación: {operation.Name}. La aplicación está ahora en modo de mantenimiento.";

            // En lugar de regresar a Index inmediatamente, podrías querer redirigir
            // a una página que confirme el inicio de la operación y explique el mantenimiento.
            // O bien, el middleware interceptará futuras solicitudes.
            return RedirectToAction("Index", "Maintenance"); // O una página de confirmación
        }
        catch (Google.GoogleApiException gex)
        {
            _logger.LogError(gex, "Error al restaurar la base de datos de Cloud SQL: {Message}", gex.Message);
            ViewBag.ErrorMessage =
                $"Error de Google Cloud: {gex.Message}. Asegúrate de que la instancia exista y el ID de la copia de seguridad sea válido.";
            _appStatusService.SetMaintenanceMode(false); // Deshabilitar mantenimiento si la iniciación de la restauración falló
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error inesperado al restaurar la base de datos de Cloud SQL.");
            ViewBag.ErrorMessage = $"Error inesperado: {ex.Message}";
            _appStatusService.SetMaintenanceMode(false); // Deshabilitar mantenimiento si la iniciación de la restauración falló
        }

        return View("Index"); // Si ocurrió un error, regresa a la página de copia de seguridad
    }

    
}