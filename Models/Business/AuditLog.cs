using System;
using System.Collections.Generic;
using Microsoft.AspNetCore.Identity;

namespace Microfinance.Models.Business;


public class AuditLog
{
    public long AuditId { get; set; }

    public string AffectedTable { get; set; } = null!;

    public int RecordId { get; set; }

    public string UserId { get; set; } = null!;

    public DateTime Timestamp { get; set; }
    
    public IdentityUser User { get; set; } = null!;
}
