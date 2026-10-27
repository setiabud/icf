using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ICFAccounting.Data.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "dbo");

            migrationBuilder.CreateTable(
                name: "PledgeCategories",
                schema: "dbo",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CategoryName = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Active = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2(7)", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2(7)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PledgeCategories", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Pledgers",
                schema: "dbo",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    PledgerName = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Active = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2(7)", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2(7)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Pledgers", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Pledges",
                schema: "dbo",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Year = table.Column<int>(type: "int", nullable: false),
                    Amount = table.Column<decimal>(type: "decimal(38,2)", precision: 38, scale: 2, nullable: false),
                    Comment = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    PledgerId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    PledgeCategoryId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2(7)", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2(7)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Pledges", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Pledges_PledgeCategories_PledgeCategoryId",
                        column: x => x.PledgeCategoryId,
                        principalSchema: "dbo",
                        principalTable: "PledgeCategories",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_Pledges_Pledgers_PledgerId",
                        column: x => x.PledgerId,
                        principalSchema: "dbo",
                        principalTable: "Pledgers",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "PledgePayments",
                schema: "dbo",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    PledgeId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Amount = table.Column<decimal>(type: "decimal(38,2)", precision: 38, scale: 2, nullable: false),
                    PaymentDate = table.Column<DateTime>(type: "datetime2(7)", nullable: false),
                    Comment = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Active = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2(7)", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2(7)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PledgePayments", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PledgePayments_Pledges_PledgeId",
                        column: x => x.PledgeId,
                        principalSchema: "dbo",
                        principalTable: "Pledges",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateIndex(
                name: "IX_PledgePayments_PledgeId_PaymentDate_Id",
                schema: "dbo",
                table: "PledgePayments",
                columns: new[] { "PledgeId", "PaymentDate", "Id" },
                descending: new[] { false, true, true });

            migrationBuilder.CreateIndex(
                name: "IX_Pledges_PledgeCategoryId_Year_Id",
                schema: "dbo",
                table: "Pledges",
                columns: new[] { "PledgeCategoryId", "Year", "Id" },
                descending: new[] { false, true, true });

            migrationBuilder.CreateIndex(
                name: "IX_Pledges_PledgerId_Year_Id",
                schema: "dbo",
                table: "Pledges",
                columns: new[] { "PledgerId", "Year", "Id" },
                descending: new[] { false, true, true });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "PledgePayments",
                schema: "dbo");

            migrationBuilder.DropTable(
                name: "Pledges",
                schema: "dbo");

            migrationBuilder.DropTable(
                name: "PledgeCategories",
                schema: "dbo");

            migrationBuilder.DropTable(
                name: "Pledgers",
                schema: "dbo");
        }
    }
}
