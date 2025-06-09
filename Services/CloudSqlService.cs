using Google.Apis.SQLAdmin.v1beta4;
using Google.Apis.SQLAdmin.v1beta4.Data;

namespace Microfinance.Services;

    public class CloudSqlService
    {
        private readonly SQLAdminService _sqlAdminService;
        private readonly string _projectId;
        private readonly string _sqlInstanceId;

        public CloudSqlService(SQLAdminService sqlAdminService, IConfiguration configuration)
        {
            _sqlAdminService = sqlAdminService;
            _projectId = configuration["GoogleCloud:ProjectId"];
            _sqlInstanceId = configuration["GoogleCloud:SqlInstanceId"];
        }

        public async Task<Operation> StartManualBackup()
        {
            var backupRun = new BackupRun
            {
                Kind = "sql#backupRun",
                Description = "Manual backup initiated via API"
            };

            var request = _sqlAdminService.BackupRuns.Insert(backupRun, _projectId, _sqlInstanceId);
            return await request.ExecuteAsync();
        }
        

        // Para restaurar, necesitamos el ID de una copia de seguridad existente (BackupRunId)
        // Puedes obtener estas IDs listando las copias de seguridad o de los logs.
        public async Task<Operation> RestoreDatabase(long backupRunId)
        {
            var restoreContext = new RestoreBackupContext
            {
                BackupRunId = backupRunId,
                Kind = "sql#restoreBackupContext"
            };
            
            var restoreRequest = new InstancesRestoreBackupRequest
            {
                RestoreBackupContext = restoreContext
            };

            var request = _sqlAdminService.Instances.RestoreBackup(restoreRequest, _projectId, _sqlInstanceId);
            return await request.ExecuteAsync();
        }

        public async Task<BackupRun> GetLatestBackupRun()
        {
            var request = _sqlAdminService.BackupRuns.List(_projectId, _sqlInstanceId);
            request.MaxResults = 10; // Ajusta según sea necesario

            var response = await request.ExecuteAsync();
            return response.Items?
                .Where(b => b.Status == "SUCCESSFUL")
                .OrderByDescending(b => b.EndTime)
                .FirstOrDefault(); // Ordenar manualmente por fecha de finalización
        }
        
        public async Task<Operation> GetOperationStatus(string operationId)
        {
            var request = _sqlAdminService.Operations.Get(_projectId, operationId);
            return await request.ExecuteAsync();
        }
    }
