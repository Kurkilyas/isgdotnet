using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace InvoiceTrackingSystemBackend.Migrations.UserDb
{
    /// <inheritdoc />
    public partial class AddRoleIsManager : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "IsManager",
                table: "roles",
                type: "bit",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "IsManager",
                table: "roles");
        }
    }
}
