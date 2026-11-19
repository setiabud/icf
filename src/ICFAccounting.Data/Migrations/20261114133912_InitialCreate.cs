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
            migrationBuilder.EnsureSchema(name: "public");

            migrationBuilder
                .AlterDatabase()
                .Annotation(
                    "Npgsql:CollationDefinition:icf_case_insensitive_accent_sensitive",
                    "und-u-ks-level2,und-u-ks-level2,icu,False"
                );

            migrationBuilder.CreateTable(
                name: "PledgeCategories",
                schema: "public",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    CategoryName = table.Column<string>(
                        type: "text",
                        nullable: false,
                        collation: "icf_case_insensitive_accent_sensitive"
                    ),
                    Active = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAt = table.Column<DateTime>(
                        type: "timestamp without time zone",
                        nullable: false
                    ),
                    UpdatedAt = table.Column<DateTime>(
                        type: "timestamp without time zone",
                        nullable: false
                    ),
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PledgeCategories", x => x.Id);
                }
            );

            migrationBuilder.CreateTable(
                name: "Pledgers",
                schema: "public",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    PledgerName = table.Column<string>(
                        type: "text",
                        nullable: false,
                        collation: "icf_case_insensitive_accent_sensitive"
                    ),
                    Active = table.Column<bool>(type: "boolean", nullable: false),
                    Email = table.Column<string>(
                        type: "text",
                        nullable: true,
                        collation: "icf_case_insensitive_accent_sensitive"
                    ),
                    EnableNotification = table.Column<bool>(
                        type: "boolean",
                        nullable: false,
                        defaultValue: false
                    ),
                    CreatedAt = table.Column<DateTime>(
                        type: "timestamp without time zone",
                        nullable: false
                    ),
                    UpdatedAt = table.Column<DateTime>(
                        type: "timestamp without time zone",
                        nullable: false
                    ),
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Pledgers", x => x.Id);
                }
            );

            migrationBuilder.CreateTable(
                name: "Pledges",
                schema: "public",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Year = table.Column<int>(type: "integer", nullable: false),
                    Amount = table.Column<decimal>(type: "numeric", nullable: false),
                    Comment = table.Column<string>(
                        type: "text",
                        nullable: true,
                        collation: "icf_case_insensitive_accent_sensitive"
                    ),
                    PledgerId = table.Column<Guid>(type: "uuid", nullable: false),
                    PledgeCategoryId = table.Column<Guid>(type: "uuid", nullable: false),
                    CreatedAt = table.Column<DateTime>(
                        type: "timestamp without time zone",
                        nullable: false
                    ),
                    UpdatedAt = table.Column<DateTime>(
                        type: "timestamp without time zone",
                        nullable: false
                    ),
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Pledges", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Pledges_PledgeCategories_PledgeCategoryId",
                        column: x => x.PledgeCategoryId,
                        principalSchema: "public",
                        principalTable: "PledgeCategories",
                        principalColumn: "Id"
                    );
                    table.ForeignKey(
                        name: "FK_Pledges_Pledgers_PledgerId",
                        column: x => x.PledgerId,
                        principalSchema: "public",
                        principalTable: "Pledgers",
                        principalColumn: "Id"
                    );
                }
            );

            migrationBuilder.CreateTable(
                name: "PledgePayments",
                schema: "public",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    PledgeId = table.Column<Guid>(type: "uuid", nullable: false),
                    Amount = table.Column<decimal>(type: "numeric", nullable: false),
                    PaymentDate = table.Column<DateTime>(
                        type: "timestamp without time zone",
                        nullable: false
                    ),
                    Comment = table.Column<string>(
                        type: "text",
                        nullable: true,
                        collation: "icf_case_insensitive_accent_sensitive"
                    ),
                    Active = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAt = table.Column<DateTime>(
                        type: "timestamp without time zone",
                        nullable: false
                    ),
                    UpdatedAt = table.Column<DateTime>(
                        type: "timestamp without time zone",
                        nullable: false
                    ),
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PledgePayments", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PledgePayments_Pledges_PledgeId",
                        column: x => x.PledgeId,
                        principalSchema: "public",
                        principalTable: "Pledges",
                        principalColumn: "Id"
                    );
                }
            );

            migrationBuilder.CreateIndex(
                name: "IX_PledgePayments_PledgeId_PaymentDate_Id",
                schema: "public",
                table: "PledgePayments",
                columns: new[] { "PledgeId", "PaymentDate", "Id" },
                descending: new[] { false, true, true }
            );

            migrationBuilder.CreateIndex(
                name: "IX_Pledges_PledgeCategoryId_Year_Id",
                schema: "public",
                table: "Pledges",
                columns: new[] { "PledgeCategoryId", "Year", "Id" },
                descending: new[] { false, true, true }
            );

            migrationBuilder.CreateIndex(
                name: "IX_Pledges_PledgerId_Year_Id",
                schema: "public",
                table: "Pledges",
                columns: new[] { "PledgerId", "Year", "Id" },
                descending: new[] { false, true, true }
            );
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(name: "PledgePayments", schema: "public");

            migrationBuilder.DropTable(name: "Pledges", schema: "public");

            migrationBuilder.DropTable(name: "PledgeCategories", schema: "public");

            migrationBuilder.DropTable(name: "Pledgers", schema: "public");

            migrationBuilder.Sql("DROP COLLATION IF EXISTS icf_case_insensitive_accent_sensitive");
        }
    }
}
