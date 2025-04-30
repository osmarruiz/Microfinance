using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.AspNetCore.Identity;

namespace Microfinance.Models.Business;

[Table("audit_log", Schema = "business")]
public class AuditLog
{
    [Key] [Column("audit_id")] public long AuditId { get; set; }

    [Column("affected_table")]
    [MaxLength(50)]
    public string AffectedTable { get; set; } = null!;

    [Column("record_id")] public int RecordId { get; set; }

    [Column("action")] [MaxLength(20)] public string Action { get; set; } = null!;
    [MaxLength(200)] [Column("user_id")] public string UserId { get; set; } = null!;

    [Column("log_time")] public DateTimeOffset LogTime { get; set; } = DateTimeOffset.UtcNow;

    public IdentityUser User { get; set; } = null!;
}

public static class AuditActionEnum
{
    public const string Create = "Create";
    public const string Update = "Update";
    public const string Delete = "Delete";
    public const string Restore = "Restore";
}