using InvoiceTrackingSystemBackend.Constants;
using InvoiceTrackingSystemBackend.Entities.Invoice;
using Microsoft.EntityFrameworkCore;

namespace InvoiceTrackingSystemBackend.Data;

public class InvoiceDbContext : DbContext
{
    public InvoiceDbContext(DbContextOptions<InvoiceDbContext> options) : base(options)
    {
    }

    public DbSet<ExchangeRate> ExchangeRates { get; set; }
    public DbSet<InvoiceType> InvoiceTypes { get; set; }
    public DbSet<InvoiceTypeDepartmentStep> InvoiceTypeDepartmentSteps { get; set; }
    public DbSet<SupplierCategory> SupplierCategories { get; set; }
    public DbSet<Supplier> Suppliers { get; set; }
    public DbSet<SupplierInvoiceType> SupplierInvoiceTypes { get; set; }
    public DbSet<Invoice> Invoices { get; set; }
    public DbSet<InvoiceLineItem> InvoiceLineItems { get; set; }
    public DbSet<InvoiceRelation> InvoiceRelations { get; set; }
    public DbSet<WorkflowStepDefinition> WorkflowStepDefinitions { get; set; }
    public DbSet<WorkflowTransitionRule> WorkflowTransitionRules { get; set; }
    public DbSet<InvoiceWorkflowStep> InvoiceWorkflowSteps { get; set; }
    public DbSet<InvoiceWorkflowHistory> InvoiceWorkflowHistories { get; set; }
    public DbSet<InvoiceActivityLog> InvoiceActivityLogs { get; set; }
    public DbSet<InvoiceAttachment> InvoiceAttachments { get; set; }
    public DbSet<InvoiceNotificationLog> InvoiceNotificationLogs { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<ExchangeRate>(entity =>
        {
            entity.ToTable("exchange_rates");
            entity.HasIndex(e => new { e.CurrencyCode, e.RateDate })
                  .IsUnique()
                  .HasDatabaseName("uq_exchange_rate_day");
            entity.Property(e => e.Source).HasDefaultValue("TCMB");
        });

        modelBuilder.Entity<InvoiceType>(entity =>
        {
            entity.ToTable("invoice_types");
            entity.HasIndex(e => e.Code).IsUnique();
            entity.Property(e => e.IsActive).HasDefaultValue(true);
            entity.HasQueryFilter(e => e.DeletedAt == null);
        });

        modelBuilder.Entity<InvoiceTypeDepartmentStep>(entity =>
        {
            entity.ToTable("invoice_type_department_steps");
            entity.HasIndex(e => new { e.InvoiceTypeId, e.StepOrder })
                  .IsUnique()
                  .HasDatabaseName("uq_invoice_type_step_order");
            entity.Property(e => e.MaxDurationDays).HasColumnType("decimal(5,2)");
            entity.Property(e => e.IsActive).HasDefaultValue(true);

            entity.HasOne(e => e.InvoiceType)
                  .WithMany(t => t.DepartmentSteps)
                  .HasForeignKey(e => e.InvoiceTypeId)
                  .OnDelete(DeleteBehavior.Cascade);

            entity.HasQueryFilter(e => e.DeletedAt == null);
        });

        modelBuilder.Entity<SupplierCategory>(entity =>
        {
            entity.ToTable("supplier_categories");
            entity.Property(e => e.IsActive).HasDefaultValue(true);
            entity.HasQueryFilter(e => e.DeletedAt == null);
        });

        modelBuilder.Entity<Supplier>(entity =>
        {
            entity.ToTable("suppliers");
            entity.HasIndex(e => e.VknTckn).IsUnique();
            entity.Property(e => e.IsActive).HasDefaultValue(true);

            entity.HasOne(e => e.SupplierCategory)
                  .WithMany(c => c.Suppliers)
                  .HasForeignKey(e => e.SupplierCategoryId)
                  .OnDelete(DeleteBehavior.SetNull);

            entity.HasQueryFilter(e => e.DeletedAt == null);
        });

        modelBuilder.Entity<SupplierInvoiceType>(entity =>
        {
            entity.ToTable("supplier_invoice_types");
            entity.HasIndex(e => new { e.SupplierId, e.InvoiceTypeId })
                  .IsUnique()
                  .HasDatabaseName("uq_supplier_invoice_type");
            entity.Property(e => e.IsActive).HasDefaultValue(true);

            entity.HasOne(e => e.Supplier)
                  .WithMany(s => s.SupplierInvoiceTypes)
                  .HasForeignKey(e => e.SupplierId)
                  .OnDelete(DeleteBehavior.Cascade);

            // Supplier -> Cascade zaten var; SQL Server multiple cascade path hatasına düşmemek için Restrict
            entity.HasOne(e => e.InvoiceType)
                  .WithMany(t => t.SupplierInvoiceTypes)
                  .HasForeignKey(e => e.InvoiceTypeId)
                  .OnDelete(DeleteBehavior.Restrict);

            entity.HasQueryFilter(e => e.DeletedAt == null);
        });

        modelBuilder.Entity<Invoice>(entity =>
        {
            entity.ToTable("invoices");

            entity.HasIndex(e => new { e.VknTckn, e.InvoiceNumber, e.Amount })
                  .IsUnique()
                  .HasDatabaseName("uq_invoice_dedup");
            entity.HasIndex(e => e.CurrentStatus);
            entity.HasIndex(e => e.SupplierId);
            entity.HasIndex(e => e.InvoiceTypeId);

            entity.Property(e => e.Amount).HasColumnType("decimal(18,2)");
            entity.Property(e => e.AmountTry).HasColumnType("decimal(18,2)");
            entity.Property(e => e.PredictionConfidence).HasColumnType("decimal(5,4)");

            entity.Property(e => e.Currency)
                  .HasConversion<string>()
                  .HasMaxLength(10)
                  .HasDefaultValue(Currency.Try);
            entity.Property(e => e.AssignmentMethod)
                  .HasConversion<string>()
                  .HasMaxLength(30);
            entity.Property(e => e.CurrentStatus)
                  .HasConversion<string>()
                  .HasMaxLength(40);

            entity.Property(e => e.IsDuplicate).HasDefaultValue(false);

            entity.HasOne(e => e.Supplier)
                  .WithMany(s => s.Invoices)
                  .HasForeignKey(e => e.SupplierId)
                  .OnDelete(DeleteBehavior.SetNull);

            entity.HasOne(e => e.InvoiceType)
                  .WithMany(t => t.Invoices)
                  .HasForeignKey(e => e.InvoiceTypeId)
                  .OnDelete(DeleteBehavior.SetNull);

            entity.HasOne(e => e.ExchangeRate)
                  .WithMany(r => r.Invoices)
                  .HasForeignKey(e => e.ExchangeRateId)
                  .OnDelete(DeleteBehavior.SetNull);

            entity.HasQueryFilter(e => e.DeletedAt == null);
        });

        modelBuilder.Entity<InvoiceLineItem>(entity =>
        {
            entity.ToTable("invoice_line_items");
            entity.Property(e => e.Quantity).HasColumnType("decimal(18,4)");
            entity.Property(e => e.UnitPrice).HasColumnType("decimal(18,4)");
            entity.Property(e => e.LineAmount).HasColumnType("decimal(18,2)");

            entity.HasOne(e => e.Invoice)
                  .WithMany(i => i.LineItems)
                  .HasForeignKey(e => e.InvoiceId)
                  .OnDelete(DeleteBehavior.Cascade);

            entity.HasQueryFilter(e => e.DeletedAt == null);
        });

        modelBuilder.Entity<InvoiceRelation>(entity =>
        {
            entity.ToTable("invoice_relations");

            entity.HasIndex(e => e.InvoiceId);
            entity.HasIndex(e => e.RelatedInvoiceId);
            entity.HasIndex(e => new { e.InvoiceId, e.RelatedInvoiceId, e.RelationType })
                  .IsUnique()
                  .HasDatabaseName("uq_invoice_relation");

            entity.Property(e => e.RelationType)
                  .HasConversion<string>()
                  .HasMaxLength(30);

            entity.HasOne(e => e.Invoice)
                  .WithMany(i => i.Relations)
                  .HasForeignKey(e => e.InvoiceId)
                  .OnDelete(DeleteBehavior.Cascade);

            // DBML'de cascade; SQL Server aynı tabloya iki cascade path'e izin vermediği için Restrict
            entity.HasOne(e => e.RelatedInvoice)
                  .WithMany(i => i.RelatedToRelations)
                  .HasForeignKey(e => e.RelatedInvoiceId)
                  .OnDelete(DeleteBehavior.Restrict);

            // Invoice soft-delete filtreli olduğu için required navigation uyarısını önler
            entity.HasQueryFilter(e => e.Invoice.DeletedAt == null && e.RelatedInvoice.DeletedAt == null);
        });

        modelBuilder.Entity<WorkflowStepDefinition>(entity =>
        {
            entity.ToTable("workflow_step_definitions");
            entity.HasIndex(e => e.Code).IsUnique();
            entity.Property(e => e.StepKind)
                  .HasConversion<string>()
                  .HasMaxLength(30)
                  .HasDefaultValue(StepKind.Fixed);
            entity.Property(e => e.MaxDurationDays).HasColumnType("decimal(5,2)");
            entity.Property(e => e.IsActive).HasDefaultValue(true);
            entity.HasQueryFilter(e => e.DeletedAt == null);
        });

        modelBuilder.Entity<WorkflowTransitionRule>(entity =>
        {
            entity.ToTable("workflow_transition_rules");

            entity.HasIndex(e => new { e.TriggerAction, e.SourceStepCode })
                  .HasDatabaseName("ix_transition_rule_lookup");

            entity.Property(e => e.TriggerAction)
                  .HasConversion<string>()
                  .HasMaxLength(40);
            entity.Property(e => e.Priority).HasDefaultValue(100);
            entity.Property(e => e.IsActive).HasDefaultValue(true);

            // FK'lar workflow_step_definitions.code alternate key'ine bağlanır
            entity.HasOne(e => e.SourceStep)
                  .WithMany(d => d.SourceTransitionRules)
                  .HasForeignKey(e => e.SourceStepCode)
                  .HasPrincipalKey(d => d.Code)
                  .OnDelete(DeleteBehavior.SetNull);

            entity.HasOne(e => e.TargetStep)
                  .WithMany(d => d.TargetTransitionRules)
                  .HasForeignKey(e => e.TargetStepCode)
                  .HasPrincipalKey(d => d.Code)
                  .OnDelete(DeleteBehavior.Restrict);

            entity.HasQueryFilter(e => e.DeletedAt == null);
        });

        modelBuilder.Entity<InvoiceWorkflowStep>(entity =>
        {
            entity.ToTable("invoice_workflow_steps", tb =>
                // step_definition_id ve department_step_id'den TAM OLARAK BİRİ dolu olmalı
                tb.HasCheckConstraint(
                    "ck_invoice_workflow_steps_exactly_one_source",
                    "(StepDefinitionId IS NOT NULL AND DepartmentStepId IS NULL) OR (StepDefinitionId IS NULL AND DepartmentStepId IS NOT NULL)"));

            entity.HasIndex(e => new { e.DueAt, e.CompletedAt })
                  .HasDatabaseName("ix_workflow_steps_due");
            entity.HasIndex(e => new { e.InvoiceId, e.CompletedAt })
                  .HasDatabaseName("ix_workflow_steps_current_lookup");

            entity.Property(e => e.IsOverdue).HasDefaultValue(false);

            entity.HasOne(e => e.Invoice)
                  .WithMany(i => i.WorkflowSteps)
                  .HasForeignKey(e => e.InvoiceId)
                  .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(e => e.StepDefinition)
                  .WithMany(d => d.WorkflowSteps)
                  .HasForeignKey(e => e.StepDefinitionId)
                  .OnDelete(DeleteBehavior.SetNull);

            entity.HasOne(e => e.DepartmentStep)
                  .WithMany(s => s.WorkflowSteps)
                  .HasForeignKey(e => e.DepartmentStepId)
                  .OnDelete(DeleteBehavior.SetNull);

            entity.HasQueryFilter(e => e.Invoice.DeletedAt == null);
        });

        modelBuilder.Entity<InvoiceWorkflowHistory>(entity =>
        {
            entity.ToTable("invoice_workflow_history");

            entity.Property(e => e.ActionType)
                  .HasConversion<string>()
                  .HasMaxLength(40);
            entity.Property(e => e.FromStatus)
                  .HasConversion<string>()
                  .HasMaxLength(40);
            entity.Property(e => e.ToStatus)
                  .HasConversion<string>()
                  .HasMaxLength(40);

            entity.HasOne(e => e.Invoice)
                  .WithMany(i => i.WorkflowHistories)
                  .HasForeignKey(e => e.InvoiceId)
                  .OnDelete(DeleteBehavior.Cascade);

            entity.HasQueryFilter(e => e.Invoice.DeletedAt == null);
        });

        modelBuilder.Entity<InvoiceActivityLog>(entity =>
        {
            entity.ToTable("invoice_activity_logs");

            entity.HasIndex(e => e.InvoiceId);
            entity.HasIndex(e => e.CreatedAt);

            entity.Property(e => e.ActivityType)
                  .HasConversion<string>()
                  .HasMaxLength(30);

            entity.HasOne(e => e.Invoice)
                  .WithMany(i => i.ActivityLogs)
                  .HasForeignKey(e => e.InvoiceId)
                  .OnDelete(DeleteBehavior.Cascade);

            // Invoice -> Cascade zaten var; multiple cascade path oluşmaması için ClientSetNull
            entity.HasOne(e => e.WorkflowStep)
                  .WithMany(s => s.ActivityLogs)
                  .HasForeignKey(e => e.WorkflowStepId)
                  .OnDelete(DeleteBehavior.ClientSetNull);

            entity.HasQueryFilter(e => e.Invoice.DeletedAt == null);
        });

        modelBuilder.Entity<InvoiceAttachment>(entity =>
        {
            entity.ToTable("invoice_attachments");

            entity.Property(e => e.FileType).HasDefaultValue("SCANNED_SIGNED_PDF");

            entity.HasOne(e => e.Invoice)
                  .WithMany(i => i.Attachments)
                  .HasForeignKey(e => e.InvoiceId)
                  .OnDelete(DeleteBehavior.Cascade);

            entity.HasQueryFilter(e => e.DeletedAt == null);
        });

        modelBuilder.Entity<InvoiceNotificationLog>(entity =>
        {
            entity.ToTable("invoice_notification_logs");

            entity.Property(e => e.NotificationType)
                  .HasConversion<string>()
                  .HasMaxLength(30);
            entity.Property(e => e.IsSuccess).HasDefaultValue(true);

            entity.HasOne(e => e.Invoice)
                  .WithMany(i => i.NotificationLogs)
                  .HasForeignKey(e => e.InvoiceId)
                  .OnDelete(DeleteBehavior.Cascade);

            // Invoice -> Cascade zaten var; multiple cascade path oluşmaması için ClientSetNull
            entity.HasOne(e => e.WorkflowStep)
                  .WithMany(s => s.NotificationLogs)
                  .HasForeignKey(e => e.WorkflowStepId)
                  .OnDelete(DeleteBehavior.ClientSetNull);

            entity.HasQueryFilter(e => e.Invoice.DeletedAt == null);
        });
    }
}
