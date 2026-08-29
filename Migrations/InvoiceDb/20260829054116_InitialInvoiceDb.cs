using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace InvoiceTrackingSystemBackend.Migrations.InvoiceDb
{
    /// <inheritdoc />
    public partial class InitialInvoiceDb : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "exchange_rates",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    CurrencyCode = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: false),
                    RateDate = table.Column<DateOnly>(type: "date", nullable: false),
                    RateToTry = table.Column<decimal>(type: "decimal(18,6)", nullable: false),
                    Source = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false, defaultValue: "TCMB"),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_exchange_rates", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "invoice_types",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Code = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    Name = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false, defaultValue: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    DeletedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_invoice_types", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "supplier_categories",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false, defaultValue: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    DeletedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_supplier_categories", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "invoice_type_steps",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    InvoiceTypeId = table.Column<int>(type: "int", nullable: false),
                    StepOrder = table.Column<int>(type: "int", nullable: false),
                    StepName = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                    StepRoleTag = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: true),
                    DepartmentId = table.Column<int>(type: "int", nullable: true),
                    MaxDurationDays = table.Column<decimal>(type: "decimal(5,2)", nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false, defaultValue: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    DeletedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_invoice_type_steps", x => x.Id);
                    table.ForeignKey(
                        name: "FK_invoice_type_steps_invoice_types_InvoiceTypeId",
                        column: x => x.InvoiceTypeId,
                        principalTable: "invoice_types",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "suppliers",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    VknTckn = table.Column<string>(type: "nvarchar(11)", maxLength: 11, nullable: false),
                    Name = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    SupplierCategoryId = table.Column<int>(type: "int", nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false, defaultValue: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    DeletedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_suppliers", x => x.Id);
                    table.ForeignKey(
                        name: "FK_suppliers_supplier_categories_SupplierCategoryId",
                        column: x => x.SupplierCategoryId,
                        principalTable: "supplier_categories",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "invoice_type_step_approvers",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    InvoiceTypeStepId = table.Column<int>(type: "int", nullable: false),
                    UserId = table.Column<int>(type: "int", nullable: false),
                    Priority = table.Column<int>(type: "int", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false, defaultValue: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    DeletedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_invoice_type_step_approvers", x => x.Id);
                    table.ForeignKey(
                        name: "FK_invoice_type_step_approvers_invoice_type_steps_InvoiceTypeStepId",
                        column: x => x.InvoiceTypeStepId,
                        principalTable: "invoice_type_steps",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "workflow_transition_rules",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    TriggerAction = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false),
                    SourceStepId = table.Column<int>(type: "int", nullable: true),
                    TargetStepId = table.Column<int>(type: "int", nullable: false),
                    Priority = table.Column<int>(type: "int", nullable: false, defaultValue: 100),
                    IsActive = table.Column<bool>(type: "bit", nullable: false, defaultValue: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    DeletedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_workflow_transition_rules", x => x.Id);
                    table.ForeignKey(
                        name: "FK_workflow_transition_rules_invoice_type_steps_SourceStepId",
                        column: x => x.SourceStepId,
                        principalTable: "invoice_type_steps",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_workflow_transition_rules_invoice_type_steps_TargetStepId",
                        column: x => x.TargetStepId,
                        principalTable: "invoice_type_steps",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "invoices",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ExternalInvoiceId = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    VknTckn = table.Column<string>(type: "nvarchar(11)", maxLength: 11, nullable: false),
                    InvoiceNumber = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    InvoiceDate = table.Column<DateOnly>(type: "date", nullable: false),
                    Amount = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    Currency = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: false, defaultValue: "Try"),
                    SupplierId = table.Column<int>(type: "int", nullable: true),
                    CurrentAccountCode = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    RawPayloadJson = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ExchangeRateId = table.Column<int>(type: "int", nullable: true),
                    AmountTry = table.Column<decimal>(type: "decimal(18,2)", nullable: true),
                    ErpMatchStatus = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: true),
                    ErpCheckedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ErpSource = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: true),
                    IsDuplicate = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                    ErrorReason = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    InvoiceTypeId = table.Column<int>(type: "int", nullable: true),
                    PredictionConfidence = table.Column<decimal>(type: "decimal(5,4)", nullable: true),
                    PredictionModelVersion = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    AssignmentMethod = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: true),
                    AssignedUserId = table.Column<int>(type: "int", nullable: true),
                    Auditor1UserId = table.Column<int>(type: "int", nullable: true),
                    Auditor2UserId = table.Column<int>(type: "int", nullable: true),
                    CurrentStatus = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    DeletedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_invoices", x => x.Id);
                    table.ForeignKey(
                        name: "FK_invoices_exchange_rates_ExchangeRateId",
                        column: x => x.ExchangeRateId,
                        principalTable: "exchange_rates",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_invoices_invoice_types_InvoiceTypeId",
                        column: x => x.InvoiceTypeId,
                        principalTable: "invoice_types",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_invoices_suppliers_SupplierId",
                        column: x => x.SupplierId,
                        principalTable: "suppliers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "supplier_invoice_types",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    SupplierId = table.Column<int>(type: "int", nullable: false),
                    InvoiceTypeId = table.Column<int>(type: "int", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false, defaultValue: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    DeletedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_supplier_invoice_types", x => x.Id);
                    table.ForeignKey(
                        name: "FK_supplier_invoice_types_invoice_types_InvoiceTypeId",
                        column: x => x.InvoiceTypeId,
                        principalTable: "invoice_types",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_supplier_invoice_types_suppliers_SupplierId",
                        column: x => x.SupplierId,
                        principalTable: "suppliers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "invoice_attachments",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    InvoiceId = table.Column<int>(type: "int", nullable: false),
                    FileName = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    NasRelativePath = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    FileType = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false, defaultValue: "SCANNED_SIGNED_PDF"),
                    FileSizeBytes = table.Column<int>(type: "int", nullable: true),
                    ChecksumSha256 = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: true),
                    UploadedByUserId = table.Column<int>(type: "int", nullable: true),
                    UploadedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_invoice_attachments", x => x.Id);
                    table.ForeignKey(
                        name: "FK_invoice_attachments_invoices_InvoiceId",
                        column: x => x.InvoiceId,
                        principalTable: "invoices",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "invoice_line_items",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    InvoiceId = table.Column<int>(type: "int", nullable: false),
                    Description = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    Quantity = table.Column<decimal>(type: "decimal(18,4)", nullable: true),
                    UnitPrice = table.Column<decimal>(type: "decimal(18,4)", nullable: true),
                    LineAmount = table.Column<decimal>(type: "decimal(18,2)", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    DeletedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_invoice_line_items", x => x.Id);
                    table.ForeignKey(
                        name: "FK_invoice_line_items_invoices_InvoiceId",
                        column: x => x.InvoiceId,
                        principalTable: "invoices",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "invoice_relations",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    InvoiceId = table.Column<int>(type: "int", nullable: false),
                    RelatedInvoiceId = table.Column<int>(type: "int", nullable: false),
                    RelationType = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    Note = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    CreatedByUserId = table.Column<int>(type: "int", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_invoice_relations", x => x.Id);
                    table.ForeignKey(
                        name: "FK_invoice_relations_invoices_InvoiceId",
                        column: x => x.InvoiceId,
                        principalTable: "invoices",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_invoice_relations_invoices_RelatedInvoiceId",
                        column: x => x.RelatedInvoiceId,
                        principalTable: "invoices",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "invoice_workflow_history",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    InvoiceId = table.Column<int>(type: "int", nullable: false),
                    ActionType = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false),
                    FromStatus = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: true),
                    ToStatus = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false),
                    ActorUserId = table.Column<int>(type: "int", nullable: true),
                    Reason = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_invoice_workflow_history", x => x.Id);
                    table.ForeignKey(
                        name: "FK_invoice_workflow_history_invoices_InvoiceId",
                        column: x => x.InvoiceId,
                        principalTable: "invoices",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "invoice_workflow_steps",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    InvoiceId = table.Column<int>(type: "int", nullable: false),
                    InvoiceTypeStepId = table.Column<int>(type: "int", nullable: false),
                    AssignedUserId = table.Column<int>(type: "int", nullable: true),
                    AssignedDepartmentId = table.Column<int>(type: "int", nullable: true),
                    StartedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    DueAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CompletedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ReminderSentAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    IsOverdue = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                    Result = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: true),
                    RejectReason = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_invoice_workflow_steps", x => x.Id);
                    table.ForeignKey(
                        name: "FK_invoice_workflow_steps_invoice_type_steps_InvoiceTypeStepId",
                        column: x => x.InvoiceTypeStepId,
                        principalTable: "invoice_type_steps",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_invoice_workflow_steps_invoices_InvoiceId",
                        column: x => x.InvoiceId,
                        principalTable: "invoices",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "invoice_activity_logs",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    InvoiceId = table.Column<int>(type: "int", nullable: false),
                    WorkflowStepId = table.Column<int>(type: "int", nullable: true),
                    UserId = table.Column<int>(type: "int", nullable: true),
                    ActivityType = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    IpAddress = table.Column<string>(type: "nvarchar(45)", maxLength: 45, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_invoice_activity_logs", x => x.Id);
                    table.ForeignKey(
                        name: "FK_invoice_activity_logs_invoice_workflow_steps_WorkflowStepId",
                        column: x => x.WorkflowStepId,
                        principalTable: "invoice_workflow_steps",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_invoice_activity_logs_invoices_InvoiceId",
                        column: x => x.InvoiceId,
                        principalTable: "invoices",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "invoice_notification_logs",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    InvoiceId = table.Column<int>(type: "int", nullable: false),
                    WorkflowStepId = table.Column<int>(type: "int", nullable: true),
                    RecipientUserId = table.Column<int>(type: "int", nullable: true),
                    RecipientEmail = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                    NotificationType = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    SentAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    IsSuccess = table.Column<bool>(type: "bit", nullable: false, defaultValue: true),
                    ErrorMessage = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_invoice_notification_logs", x => x.Id);
                    table.ForeignKey(
                        name: "FK_invoice_notification_logs_invoice_workflow_steps_WorkflowStepId",
                        column: x => x.WorkflowStepId,
                        principalTable: "invoice_workflow_steps",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_invoice_notification_logs_invoices_InvoiceId",
                        column: x => x.InvoiceId,
                        principalTable: "invoices",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "uq_exchange_rate_day",
                table: "exchange_rates",
                columns: new[] { "CurrencyCode", "RateDate" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_invoice_activity_logs_CreatedAt",
                table: "invoice_activity_logs",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_invoice_activity_logs_InvoiceId",
                table: "invoice_activity_logs",
                column: "InvoiceId");

            migrationBuilder.CreateIndex(
                name: "IX_invoice_activity_logs_WorkflowStepId",
                table: "invoice_activity_logs",
                column: "WorkflowStepId");

            migrationBuilder.CreateIndex(
                name: "IX_invoice_attachments_InvoiceId",
                table: "invoice_attachments",
                column: "InvoiceId");

            migrationBuilder.CreateIndex(
                name: "IX_invoice_line_items_InvoiceId",
                table: "invoice_line_items",
                column: "InvoiceId");

            migrationBuilder.CreateIndex(
                name: "IX_invoice_notification_logs_InvoiceId",
                table: "invoice_notification_logs",
                column: "InvoiceId");

            migrationBuilder.CreateIndex(
                name: "IX_invoice_notification_logs_WorkflowStepId",
                table: "invoice_notification_logs",
                column: "WorkflowStepId");

            migrationBuilder.CreateIndex(
                name: "IX_invoice_relations_InvoiceId",
                table: "invoice_relations",
                column: "InvoiceId");

            migrationBuilder.CreateIndex(
                name: "IX_invoice_relations_RelatedInvoiceId",
                table: "invoice_relations",
                column: "RelatedInvoiceId");

            migrationBuilder.CreateIndex(
                name: "uq_invoice_relation",
                table: "invoice_relations",
                columns: new[] { "InvoiceId", "RelatedInvoiceId", "RelationType" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "uq_step_approver_priority",
                table: "invoice_type_step_approvers",
                columns: new[] { "InvoiceTypeStepId", "Priority" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "uq_step_approver_user",
                table: "invoice_type_step_approvers",
                columns: new[] { "InvoiceTypeStepId", "UserId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "uq_invoice_type_step_order",
                table: "invoice_type_steps",
                columns: new[] { "InvoiceTypeId", "StepOrder" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_invoice_types_Code",
                table: "invoice_types",
                column: "Code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_invoice_workflow_history_InvoiceId",
                table: "invoice_workflow_history",
                column: "InvoiceId");

            migrationBuilder.CreateIndex(
                name: "IX_invoice_workflow_steps_InvoiceTypeStepId",
                table: "invoice_workflow_steps",
                column: "InvoiceTypeStepId");

            migrationBuilder.CreateIndex(
                name: "ix_workflow_steps_current_lookup",
                table: "invoice_workflow_steps",
                columns: new[] { "InvoiceId", "CompletedAt" });

            migrationBuilder.CreateIndex(
                name: "ix_workflow_steps_due",
                table: "invoice_workflow_steps",
                columns: new[] { "DueAt", "CompletedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_invoices_CurrentStatus",
                table: "invoices",
                column: "CurrentStatus");

            migrationBuilder.CreateIndex(
                name: "IX_invoices_ExchangeRateId",
                table: "invoices",
                column: "ExchangeRateId");

            migrationBuilder.CreateIndex(
                name: "IX_invoices_InvoiceTypeId",
                table: "invoices",
                column: "InvoiceTypeId");

            migrationBuilder.CreateIndex(
                name: "IX_invoices_SupplierId",
                table: "invoices",
                column: "SupplierId");

            migrationBuilder.CreateIndex(
                name: "uq_invoice_dedup",
                table: "invoices",
                columns: new[] { "VknTckn", "InvoiceNumber", "Amount" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_supplier_invoice_types_InvoiceTypeId",
                table: "supplier_invoice_types",
                column: "InvoiceTypeId");

            migrationBuilder.CreateIndex(
                name: "uq_supplier_invoice_type",
                table: "supplier_invoice_types",
                columns: new[] { "SupplierId", "InvoiceTypeId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_suppliers_SupplierCategoryId",
                table: "suppliers",
                column: "SupplierCategoryId");

            migrationBuilder.CreateIndex(
                name: "IX_suppliers_VknTckn",
                table: "suppliers",
                column: "VknTckn",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_workflow_transition_rules_SourceStepId",
                table: "workflow_transition_rules",
                column: "SourceStepId");

            migrationBuilder.CreateIndex(
                name: "IX_workflow_transition_rules_TargetStepId",
                table: "workflow_transition_rules",
                column: "TargetStepId");

            migrationBuilder.CreateIndex(
                name: "ix_transition_rule_lookup",
                table: "workflow_transition_rules",
                columns: new[] { "TriggerAction", "SourceStepId" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "invoice_activity_logs");

            migrationBuilder.DropTable(
                name: "invoice_attachments");

            migrationBuilder.DropTable(
                name: "invoice_line_items");

            migrationBuilder.DropTable(
                name: "invoice_notification_logs");

            migrationBuilder.DropTable(
                name: "invoice_relations");

            migrationBuilder.DropTable(
                name: "invoice_type_step_approvers");

            migrationBuilder.DropTable(
                name: "invoice_workflow_history");

            migrationBuilder.DropTable(
                name: "supplier_invoice_types");

            migrationBuilder.DropTable(
                name: "workflow_transition_rules");

            migrationBuilder.DropTable(
                name: "invoice_workflow_steps");

            migrationBuilder.DropTable(
                name: "invoice_type_steps");

            migrationBuilder.DropTable(
                name: "invoices");

            migrationBuilder.DropTable(
                name: "exchange_rates");

            migrationBuilder.DropTable(
                name: "invoice_types");

            migrationBuilder.DropTable(
                name: "suppliers");

            migrationBuilder.DropTable(
                name: "supplier_categories");
        }
    }
}
