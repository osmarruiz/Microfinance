using System.Security.Claims;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Microfinance.Models.Business;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore.ChangeTracking;

namespace Microfinance.Data;

public class ApplicationDbContext : IdentityDbContext
{
    private readonly IHttpContextAccessor _httpContextAccessor;

    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options,
        IHttpContextAccessor httpContextAccessor)
        : base(options)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    public DbSet<AuditLog> AuditLogs { get; set; }
    public DbSet<CollectionManagement> CollectionManagements { get; set; }
    public DbSet<Customer> Customers { get; set; }
    public DbSet<Installment> Installments { get; set; }
    public DbSet<Loan> Loans { get; set; }
    public DbSet<Payment> Payments { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        
        

        modelBuilder.Entity<AuditLog>(entity =>
        {
            entity.HasKey(e => e.AuditId).HasName("audit_log_pkey");
            entity.ToTable("audit_log", "business");
            entity.Property(e => e.AuditId).HasColumnName("audit_id");
            entity.Property(e => e.AffectedTable).HasMaxLength(50).HasColumnName("affected_table");
            entity.Property(e => e.RecordId).HasColumnName("record_id");
            entity.Property(e => e.Action).HasColumnName("action");
            entity.Property(e => e.LogTime).HasDefaultValueSql("CURRENT_TIMESTAMP")
                .HasColumnType("timestamp with time zone").HasColumnName("log_time");
            entity.Property(e => e.UserId).HasColumnName("user_id");

            entity.HasOne(al => al.User)
                .WithMany()
                .HasForeignKey(al => al.UserId)
                .HasConstraintName("audit_log_user_id_fkey")
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<CollectionManagement>(entity =>
        {
            entity.HasKey(e => e.CollectionId).HasName("collection_management_pkey");
            entity.ToTable("collection_management", "business");
            entity.Property(e => e.CollectionId).HasColumnName("collection_id");
            entity.Property(e => e.CollectorId).HasColumnName("collector_id");
            entity.Property(e => e.IsDeleted).HasDefaultValue(false).HasColumnName("is_deleted");
            entity.Property(e => e.LoanId).HasColumnName("loan_id");
            entity.Property(e => e.ManagementDate).HasColumnType("timestamp with time zone")
                .HasColumnName("management_date");
            entity.Property(e => e.ManagementResult).HasMaxLength(200).HasColumnName("management_result");
            entity.Property(e => e.Notes).HasColumnName("notes");

            entity.HasOne(cm => cm.Loan)
                .WithMany(l => l.CollectionManagements)
                .HasForeignKey(cm => cm.LoanId)
                .HasConstraintName("collection_management_loan_id_fkey")
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(cm => cm.Collector)
                .WithMany()
                .HasForeignKey(cm => cm.CollectorId)
                .HasConstraintName("collection_management_collector_id_fkey")
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<Customer>(entity =>
        {
            entity.HasKey(e => e.CustomerId).HasName("customers_pkey");
            entity.ToTable("customers", "business");
            entity.Property(e => e.CustomerId).HasColumnName("customer_id");
            entity.Property(e => e.Address).HasMaxLength(200).HasColumnName("address");
            entity.Property(e => e.Email).HasMaxLength(100).HasColumnName("email");
            entity.Property(e => e.FullName).HasMaxLength(100).HasColumnName("full_name");
            entity.Property(e => e.IdCard).HasMaxLength(15).HasColumnName("id_card");
            entity.Property(e => e.IsActive).HasDefaultValue(true).HasColumnName("is_active");
            entity.Property(e => e.IsDeleted).HasDefaultValue(false).HasColumnName("is_deleted");
            entity.Property(e => e.PhoneNumber).HasMaxLength(15).HasColumnName("phone_number");

            entity.HasMany(c => c.Loans).WithOne(l => l.Customer).HasForeignKey(l => l.CustomerId)
                .HasConstraintName("loans_customer_id_fkey")
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<Installment>(entity =>
        {
            entity.HasKey(e => e.InstallmentId).HasName("installments_pkey");
            entity.ToTable("installments", "business");
            entity.Property(e => e.InstallmentId).HasColumnName("installment_id");
            entity.Property(e => e.DueDate).HasColumnType("timestamp with time zone").HasColumnName("due_date");
            entity.Property(e => e.InstallmentAmount).HasPrecision(10, 2).HasColumnName("installment_amount");
            entity.Property(e => e.InstallmentNumber).HasColumnName("installment_number");
            entity.Property(e => e.IsDeleted).HasDefaultValue(false).HasColumnName("is_deleted");
            entity.Property(e => e.LateFee).HasPrecision(10, 2).HasColumnName("late_fee");
            entity.Property(e => e.LoanId).HasColumnName("loan_id");
            entity.Property(e => e.PaidAmount).HasPrecision(10, 2).HasColumnName("paid_amount");
            entity.Property(e => e.PaymentDate).HasColumnType("timestamp with time zone")
                .HasColumnName("payment_date");
            entity.Property(e => e.InstallmentStatus).HasColumnName("installment_status");

            entity.HasOne(i => i.Loan).WithMany(l => l.Installments).HasForeignKey(i => i.LoanId)
                .HasConstraintName("installments_loan_id_fkey")
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<Loan>(entity =>
        {
            entity.HasKey(e => e.LoanId).HasName("loans_pkey");
            entity.ToTable("loans", "business");
            entity.Property(e => e.LoanId).HasColumnName("loan_id");
            entity.Property(e => e.Amount).HasPrecision(10, 2).HasColumnName("amount");
            entity.Property(e => e.CurrentBalance).HasPrecision(10, 2).HasColumnName("current_balance");
            entity.Property(e => e.CustomerId).HasColumnName("customer_id");
            entity.Property(e => e.DueDate).HasColumnType("timestamp with time zone").HasColumnName("due_date");
            entity.Property(e => e.InterestRate).HasPrecision(10, 2).HasColumnName("interest_rate");
            entity.Property(e => e.IsDeleted).HasDefaultValue(false).HasColumnName("is_deleted");
            entity.Property(e => e.SellerId).HasColumnName("seller_id");
            entity.Property(e => e.StartDate).HasDefaultValueSql("CURRENT_TIMESTAMP")
                .HasColumnType("timestamp with time zone").HasColumnName("start_date");
            entity.Property(e => e.TermMonths).HasColumnName("term_months");
            entity.Property(e => e.PaymentFrequency).HasColumnName("payment_frequency");
            entity.Property(e => e.LoanStatus).HasColumnName("loan_status");

            entity.HasOne(l => l.Customer)
                .WithMany(c => c.Loans)
                .HasForeignKey(l => l.CustomerId)
                .HasConstraintName("loans_customer_id_fkey")
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(l => l.Seller)
                .WithMany()
                .HasForeignKey(l => l.SellerId)
                .HasConstraintName("loans_seller_id_fkey")
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasMany(l => l.Installments).WithOne(i => i.Loan).HasForeignKey(i => i.LoanId)
                .HasConstraintName("installments_loan_id_fkey")
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasMany(l => l.CollectionManagements).WithOne(cm => cm.Loan).HasForeignKey(cm => cm.LoanId)
                .HasConstraintName("collection_management_loan_id_fkey")
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<Payment>(entity =>
        {
            entity.HasKey(e => e.PaymentId).HasName("payments_pkey");
            entity.ToTable("payments", "business");
            entity.Property(e => e.PaymentId).HasColumnName("payment_id");
            entity.Property(e => e.CollectorId).HasColumnName("collector_id");
            entity.Property(e => e.InstallmentId).HasColumnName("installment_id");
            entity.Property(e => e.IsDeleted).HasDefaultValue(false).HasColumnName("is_deleted");
            entity.Property(e => e.PaidAmount).HasPrecision(10, 2).HasColumnName("paid_amount");
            entity.Property(e => e.PaymentDate).HasDefaultValueSql("CURRENT_TIMESTAMP")
                .HasColumnType("timestamp with time zone").HasColumnName("payment_date");
            entity.Property(e => e.Reference).HasMaxLength(100).HasColumnName("reference");

            entity.HasOne(p => p.Installment)
                .WithMany(i => i.Payments)
                .HasForeignKey(p => p.InstallmentId)
                .HasConstraintName("payments_installment_id_fkey")
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(p => p.Collector)
                .WithMany()
                .HasForeignKey(p => p.CollectorId)
                .HasConstraintName("payments_collector_id_fkey")
                .OnDelete(DeleteBehavior.Restrict);
        });

    }

    public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
{
    var userId = GetUserId();
    var now = DateTime.UtcNow;
    var auditEntries = new List<(EntityEntry Entry, AuditLog Log)>();

    foreach (var entry in ChangeTracker.Entries())
    {
        if (entry.Entity is AuditLog || 
            entry.Entity is IdentityUser ||
            entry.State == EntityState.Detached ||
            entry.State == EntityState.Unchanged ||
            entry.Metadata?.GetSchema() == null ||
            !entry.Metadata.GetSchema().Equals("business", StringComparison.OrdinalIgnoreCase))
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
                action = isNowDeleted ? AuditActionEnum.Delete : AuditActionEnum.Restore;
                var auditLog = new AuditLog
                {
                    AffectedTable = tableName,
                    Action = action,
                    UserId = userId,
                    LogTime = now
                };
                
                auditEntries.Add((entry, auditLog));
                continue;
            }
        }

        action = entry.State switch
        {
            EntityState.Added => AuditActionEnum.Create,
            EntityState.Modified => AuditActionEnum.Update,
            EntityState.Deleted => AuditActionEnum.Delete,
            _ => throw new NotSupportedException()
        };

        var log = new AuditLog
        {
            AffectedTable = tableName,
            Action = action,
            UserId = userId,
            LogTime = now
        };

        auditEntries.Add((entry, log));
    }

    var result = await base.SaveChangesAsync(cancellationToken);

    foreach (var (entry, log) in auditEntries)
    {
        var primaryKey = entry.Properties.FirstOrDefault(p => p.Metadata.IsPrimaryKey());
        if (primaryKey?.CurrentValue is int intValue)
        {
            log.RecordId = intValue;
        }
        AuditLogs.Add(log);
    }

    if (auditEntries.Any())
    {
        await base.SaveChangesAsync(cancellationToken);
    }

    return result;
}



    private string? GetUserId()
    {
        var user = _httpContextAccessor.HttpContext?.User;
        if (user == null || !user.Identity.IsAuthenticated)
            return null;

        var userIdClaim = user.FindFirst(ClaimTypes.NameIdentifier)
                          ?? user.FindFirst("sub");

        return userIdClaim?.Value?.ToString();
    }
}
