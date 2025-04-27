using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Microfinance.Models.Business;
using Microsoft.AspNetCore.Identity;

namespace Microfinance.Data
{
    public class ApplicationDbContext : IdentityDbContext<IdentityUser>
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }
        
        public virtual DbSet<AuditLog> AuditLogs { get; set; }
        public virtual DbSet<CollectionManagement> CollectionManagements { get; set; }
        public virtual DbSet<Customer> Customers { get; set; }
        public virtual DbSet<Installment> Installments { get; set; }
        public virtual DbSet<Loan> Loans { get; set; }
        public virtual DbSet<Payment> Payments { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.HasPostgresEnum("business", "audit_action_enum", new[] { "create", "update", "delete", "restore" });
            modelBuilder.HasPostgresEnum("business", "installment_status_enum", new[] { "pendiente", "pagada", "vencida" });
            modelBuilder.HasPostgresEnum("business", "loan_status_enum", new[] { "activo", "vencido", "pagado", "cancelado" });
            modelBuilder.HasPostgresEnum("business", "payment_frequency_enum", new[] { "diario", "semanal", "quincenal", "mensual" });

            modelBuilder.Entity<AuditLog>(entity =>
            {
                entity.HasKey(e => e.AuditId).HasName("audit_log_pkey");
                entity.ToTable("audit_log", "business");
                entity.Property(e => e.AuditId).HasColumnName("audit_id");
                entity.Property(e => e.AffectedTable).HasMaxLength(50).HasColumnName("affected_table");
                entity.Property(e => e.RecordId).HasColumnName("record_id");
                entity.Property(e => e.Timestamp).HasDefaultValueSql("CURRENT_TIMESTAMP").HasColumnType("timestamp without time zone").HasColumnName("timestamp");
                entity.Property(e => e.UserId).HasColumnName("user_id");

                entity.HasOne<IdentityUser>().WithMany().HasForeignKey(d => d.UserId).HasConstraintName("audit_log_user_id_fkey");
            });

            modelBuilder.Entity<CollectionManagement>(entity =>
            {
                entity.HasKey(e => e.CollectionId).HasName("collection_management_pkey");
                entity.ToTable("collection_management", "business");
                entity.Property(e => e.CollectionId).HasColumnName("collection_id");
                entity.Property(e => e.CollectorId).HasColumnName("collector_id");
                entity.Property(e => e.IsDeleted).HasDefaultValue(false).HasColumnName("is_deleted");
                entity.Property(e => e.LoanId).HasColumnName("loan_id");
                entity.Property(e => e.ManagementDate).HasColumnName("management_date");
                entity.Property(e => e.ManagementResult).HasMaxLength(200).HasColumnName("management_result");
                entity.Property(e => e.Notes).HasColumnName("notes");

                entity.HasOne(d => d.Loan).WithMany(p => p.CollectionManagements).HasForeignKey(d => d.LoanId).OnDelete(DeleteBehavior.ClientSetNull).HasConstraintName("collection_management_loan_id_fkey");
                entity.HasOne<IdentityUser>().WithMany().HasForeignKey(d => d.CollectorId).HasConstraintName("collection_management_collector_id_fkey");
            });

            modelBuilder.Entity<Customer>(entity =>
            {
                entity.HasKey(e => e.CustomerId).HasName("customers_pkey");
                entity.ToTable("customers", "business");
                entity.HasIndex(e => e.IdCard, "customers_id_card_key").IsUnique();
                entity.Property(e => e.CustomerId).HasColumnName("customer_id");
                entity.Property(e => e.Address).HasMaxLength(200).HasColumnName("address");
                entity.Property(e => e.Email).HasMaxLength(100).HasColumnName("email");
                entity.Property(e => e.FullName).HasMaxLength(40).HasColumnName("full_name");
                entity.Property(e => e.IdCard).HasMaxLength(15).HasColumnName("id_card");
                entity.Property(e => e.IsActive).HasDefaultValue(true).HasColumnName("is_active");
                entity.Property(e => e.IsDeleted).HasDefaultValue(false).HasColumnName("is_deleted");
                entity.Property(e => e.PhoneNumber).HasMaxLength(15).HasColumnName("phone_number");
            });

            modelBuilder.Entity<Installment>(entity =>
            {
                entity.HasKey(e => e.InstallmentId).HasName("installments_pkey");
                entity.ToTable("installments", "business");
                entity.Property(e => e.InstallmentId).HasColumnName("installment_id");
                entity.Property(e => e.DueDate).HasColumnType("timestamp without time zone").HasColumnName("due_date");
                entity.Property(e => e.InstallmentAmount).HasPrecision(10, 2).HasColumnName("installment_amount");
                entity.Property(e => e.InstallmentNumber).HasColumnName("installment_number");
                entity.Property(e => e.IsDeleted).HasDefaultValue(false).HasColumnName("is_deleted");
                entity.Property(e => e.LateFee).HasPrecision(10, 2).HasDefaultValueSql("0").HasColumnName("late_fee");
                entity.Property(e => e.LoanId).HasColumnName("loan_id");
                entity.Property(e => e.PaidAmount).HasPrecision(10, 2).HasColumnName("paid_amount");
                entity.Property(e => e.PaymentDate).HasColumnType("timestamp without time zone").HasColumnName("payment_date");

                entity.HasOne(d => d.Loan).WithMany(p => p.Installments).HasForeignKey(d => d.LoanId).OnDelete(DeleteBehavior.ClientSetNull).HasConstraintName("installments_loan_id_fkey");
            });

            modelBuilder.Entity<Loan>(entity =>
            {
                entity.HasKey(e => e.LoanId).HasName("loans_pkey");
                entity.ToTable("loans", "business");
                entity.Property(e => e.LoanId).HasColumnName("loan_id");
                entity.Property(e => e.Amount).HasPrecision(10, 2).HasColumnName("amount");
                entity.Property(e => e.CurrentBalance).HasPrecision(10, 2).HasColumnName("current_balance");
                entity.Property(e => e.CustomerId).HasColumnName("customer_id");
                entity.Property(e => e.DueDate).HasColumnType("timestamp without time zone").HasColumnName("due_date");
                entity.Property(e => e.InterestRate).HasPrecision(5, 2).HasColumnName("interest_rate");
                entity.Property(e => e.IsDeleted).HasDefaultValue(false).HasColumnName("is_deleted");
                entity.Property(e => e.SellerId).HasColumnName("seller_id");
                entity.Property(e => e.StartDate).HasDefaultValueSql("CURRENT_TIMESTAMP").HasColumnType("timestamp without time zone").HasColumnName("start_date");
                entity.Property(e => e.TermMonths).HasColumnName("term_months");

                entity.HasOne(d => d.Customer).WithMany(p => p.Loans).HasForeignKey(d => d.CustomerId).OnDelete(DeleteBehavior.ClientSetNull).HasConstraintName("loans_customer_id_fkey");
                entity.HasOne<IdentityUser>().WithMany().HasForeignKey(d => d.SellerId).HasConstraintName("loans_seller_id_fkey");
            });

            modelBuilder.Entity<Payment>(entity =>
            {
                entity.HasKey(e => e.PaymentId).HasName("payments_pkey");
                entity.ToTable("payments", "business");
                entity.Property(e => e.PaymentId).HasColumnName("payment_id");
                entity.Property(e => e.CollectorId).HasColumnName("collector_id");
                entity.Property(e => e.InstallmentId).HasColumnName("installment_id");
                entity.Property(e => e.IsDeleted).HasDefaultValue(false).HasColumnName("is_deleted");
                entity.Property(e => e.LoanId).HasColumnName("loan_id");
                entity.Property(e => e.PaidAmount).HasColumnName("paid_amount");
                entity.Property(e => e.PaymentDate).HasColumnType("timestamp without time zone").HasColumnName("payment_date");
                entity.Property(e => e.PaymentMethod).HasMaxLength(50).HasColumnName("payment_method");
                entity.Property(e => e.Reference).HasMaxLength(100).HasColumnName("reference");

                entity.HasOne(d => d.Installment).WithMany(p => p.Payments).HasForeignKey(d => d.InstallmentId).OnDelete(DeleteBehavior.ClientSetNull).HasConstraintName("payments_installment_id_fkey");
                entity.HasOne(d => d.Loan).WithMany(p => p.Payments).HasForeignKey(d => d.LoanId).OnDelete(DeleteBehavior.ClientSetNull).HasConstraintName("payments_loan_id_fkey");
                entity.HasOne<IdentityUser>().WithMany().HasForeignKey(d => d.CollectorId).HasConstraintName("payments_collector_id_fkey");
            });

        }
    }
}
