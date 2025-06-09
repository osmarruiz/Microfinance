namespace Microfinance.Services;

public class ApplicationStatusService
{
    private bool _isUnderMaintenance = false;

    private string _maintenanceMessage =
        "Estamos realizando una operación de mantenimiento en la base de datos. Estaremos de vuelta en unos minutos.";

    private DateTime? _maintenanceStartTime;
    private string _operationId; // Para almacenar el ID de la operación de Cloud SQL

    public bool IsUnderMaintenance
    {
        get { return _isUnderMaintenance; }
        private set { _isUnderMaintenance = value; }
    }

    public string MaintenanceMessage
    {
        get { return _maintenanceMessage; }
        set { _maintenanceMessage = value; }
    }

    public DateTime? MaintenanceStartTime
    {
        get { return _maintenanceStartTime; }
        private set { _maintenanceStartTime = value; }
    }

    public string OperationId
    {
        get { return _operationId; }
        private set { _operationId = value; }
    }


    public void SetMaintenanceMode(bool enable, string operationId = null, string message = null)
    {
        _isUnderMaintenance = enable;
        _operationId = operationId;
        if (enable)
        {
            _maintenanceStartTime = DateTime.UtcNow;
            if (!string.IsNullOrEmpty(message))
            {
                _maintenanceMessage = message;
            }
        }
        else
        {
            _maintenanceStartTime = null;
            _operationId = null;
            _maintenanceMessage =
                "Estamos realizando una operación de mantenimiento en la base de datos. Estaremos de vuelta en unos minutos."; // Restablecer al valor predeterminado
        }
    }
}