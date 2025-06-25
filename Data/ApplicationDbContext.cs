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
    private readonly IAuditService _auditService;

    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options,
        IHttpContextAccessor httpContextAccessor, IAuditService auditService)
        : base(options)
    {
        _httpContextAccessor = httpContextAccessor;
        _auditService = auditService;
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

        modelBuilder.Entity<CollectionManagement>(entity =>
        {
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
            entity.HasMany(c => c.Loans).WithOne(l => l.Customer).HasForeignKey(l => l.CustomerId)
                .HasConstraintName("loans_customer_id_fkey")
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<Installment>(entity =>
        {
            entity.Property(e => e.DueDate).HasColumnType("timestamp with time zone");
            entity.Property(e => e.PaymentDate).HasColumnType("timestamp with time zone")
                .HasColumnName("payment_date");

            entity.HasOne(i => i.Loan).WithMany(l => l.Installments).HasForeignKey(i => i.LoanId)
                .HasConstraintName("installments_loan_id_fkey")
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<Loan>(entity =>
        {
            entity.Property(e => e.DueDate).HasColumnType("timestamp with time zone");
            entity.Property(e => e.StartDate).HasColumnType("timestamp with time zone");

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
            entity.Property(e => e.PaymentDate).HasColumnType("timestamp with time zone").HasColumnName("payment_date");

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

    private bool _isAuditing = false; 

    public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        if (_isAuditing)
        {
            return await base.SaveChangesAsync(cancellationToken);
        }

        try
        {
            _isAuditing = true;
            var userId = GetUserId();
            if (userId == null)
            {
                return await base.SaveChangesAsync(cancellationToken);
            }
            return await _auditService.LogChangesAsync(this, userId, cancellationToken);
        }
        finally
        {
            _isAuditing = false; // Restablecer el flag
        }
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