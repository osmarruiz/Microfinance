using Microfinance.Models.Business;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;

public interface IAuditService
{
    Task<int> LogChangesAsync(DbContext context, string userId, CancellationToken cancellationToken = default);
}

public class AuditService : IAuditService
{
    public async Task<int> LogChangesAsync(DbContext context, string userId, CancellationToken cancellationToken = default)
    {
        var now = DateTime.UtcNow;
        var auditEntries = new List<AuditLog>();
        var changeTracker = context.ChangeTracker;

        // Fase 1: Preparar entradas de auditoría (sin ID)
        foreach (var entry in changeTracker.Entries())
        {
            if (ShouldSkipAudit(entry))
            {
                continue;
            }

            var tableName = entry.Metadata.GetTableName();
            string action;
            var isDeletedProp = entry.Properties.FirstOrDefault(p => p.Metadata.Name == "IsDeleted");

            if (isDeletedProp != null && entry.State == EntityState.Modified)
            {
                var wasDeleted = (bool)isDeletedProp.OriginalValue;
                var isNowDeleted = (bool)isDeletedProp.CurrentValue;

                var changedProperties = entry.Properties
                    .Where(p => p.IsModified && p.Metadata.Name != "IsDeleted")
                    .ToList();

                if (changedProperties.Count == 0)
                {
                    action = isNowDeleted ? "Delete" : "Restore"; // Usando strings directamente
                    auditEntries.Add(new AuditLog
                    {
                        AffectedTable = tableName,
                        Action = action,
                        UserId = userId,
                        LogTime = now,
                        RecordId = 0 // Temporal
                    });
                    continue;
                }
            }

            action = entry.State switch
            {
                EntityState.Added => "Create",
                EntityState.Modified => "Update",
                EntityState.Deleted => "Delete",
                _ => throw new NotSupportedException()
            };

            auditEntries.Add(new AuditLog
            {
                AffectedTable = tableName,
                Action = action,
                UserId = userId,
                LogTime = now,
                RecordId = 0 // Temporal
            });
        }

        // Fase 2: Guardar cambios principales (para generar IDs)
        var result = await context.SaveChangesAsync(cancellationToken);

        // Fase 3: Asignar IDs reales a los logs de auditoría
        foreach (var log in auditEntries)
        {
            var entry = changeTracker.Entries()
                .FirstOrDefault(e => e.Metadata.GetTableName() == log.AffectedTable && 
                                   e.Properties.Any(p => p.Metadata.IsPrimaryKey() && 
                                                       (int)p.CurrentValue != 0));
            
            if (entry != null)
            {
                var primaryKey = entry.Properties.FirstOrDefault(p => p.Metadata.IsPrimaryKey());
                if (primaryKey?.CurrentValue != null)
                {
                    log.RecordId = Convert.ToInt32(primaryKey.CurrentValue);
                }
            }
        }

        // Fase 4: Guardar los logs de auditoría
        if (auditEntries.Any())
        {
            await context.Set<AuditLog>().AddRangeAsync(auditEntries, cancellationToken);
            await context.SaveChangesAsync(cancellationToken);
        }

        return result;
    }

    private static bool ShouldSkipAudit(EntityEntry entry)
    {
        return entry.Entity is AuditLog || 
               entry.Entity is IdentityUser ||
               entry.State == EntityState.Detached ||
               entry.State == EntityState.Unchanged ||
               entry.Metadata?.GetSchema() == null ||
               !entry.Metadata.GetSchema().Equals("business", StringComparison.OrdinalIgnoreCase);
    }
}