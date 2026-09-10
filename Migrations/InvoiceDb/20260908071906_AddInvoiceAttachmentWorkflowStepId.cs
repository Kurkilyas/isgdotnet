using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace InvoiceTrackingSystemBackend.Migrations.InvoiceDb
{
    /// <inheritdoc />
    public partial class AddInvoiceAttachmentWorkflowStepId : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "WorkflowStepId",
                table: "invoice_attachments",
                type: "int",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_invoice_attachments_WorkflowStepId",
                table: "invoice_attachments",
                column: "WorkflowStepId");

            migrationBuilder.AddForeignKey(
                name: "FK_invoice_attachments_invoice_workflow_steps_WorkflowStepId",
                table: "invoice_attachments",
                column: "WorkflowStepId",
                principalTable: "invoice_workflow_steps",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_invoice_attachments_invoice_workflow_steps_WorkflowStepId",
                table: "invoice_attachments");

            migrationBuilder.DropIndex(
                name: "IX_invoice_attachments_WorkflowStepId",
                table: "invoice_attachments");

            migrationBuilder.DropColumn(
                name: "WorkflowStepId",
                table: "invoice_attachments");
        }
    }
}
