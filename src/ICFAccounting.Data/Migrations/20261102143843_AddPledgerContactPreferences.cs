using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ICFAccounting.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddPledgerContactPreferences : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Email",
                schema: "dbo",
                table: "Pledgers",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "EnableNotification",
                schema: "dbo",
                table: "Pledgers",
                type: "bit",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Email",
                schema: "dbo",
                table: "Pledgers");

            migrationBuilder.DropColumn(
                name: "EnableNotification",
                schema: "dbo",
                table: "Pledgers");
        }
    }
}
