using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace InvoiceTrackingSystemBackend.Migrations.InvoiceDb
{
    /// <inheritdoc />
    public partial class AddInvoiceArchiveFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "ArchivedAt",
                table: "invoices",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "ArchivedByUserId",
                table: "invoices",
                type: "int",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ArchivedAt",
                table: "invoices");

            migrationBuilder.DropColumn(
                name: "ArchivedByUserId",
                table: "invoices");
        }
    }
}
