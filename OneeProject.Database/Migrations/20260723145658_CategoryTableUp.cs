using System;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace OneeProject.Database.Migrations
{
    /// <inheritdoc />
    public partial class CategoryTableUp : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "m_categories",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    Category_Name = table.Column<string>(type: "longtext", nullable: false, collation: "utf8mb4_general_ci"),
                    Isdelete = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    CreatedBy = table.Column<string>(type: "longtext", nullable: false, collation: "utf8mb4_general_ci"),
                    CreatedOn = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    LastUpdatedBy = table.Column<string>(type: "longtext", nullable: true, collation: "utf8mb4_general_ci"),
                    LastUpdatedOn = table.Column<DateTime>(type: "datetime(6)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_m_categories", x => x.Id);
                })
                .Annotation("Relational:Collation", "utf8mb4_general_ci");

            migrationBuilder.UpdateData(
                table: "Tbl_Role",
                keyColumn: "Id",
                keyValue: "a1b2c3d4-e5f6-7890-abcd-ef1234567890",
                column: "ConcurrencyStamp",
                value: "36316d78-a991-4f4e-9df8-de0169500a32");

            migrationBuilder.UpdateData(
                table: "Tbl_Role",
                keyColumn: "Id",
                keyValue: "d3f1a7b2-8c4e-4f3a-9a1e-1a2b3c4d5e6f",
                column: "ConcurrencyStamp",
                value: "f5e7cd5d-119d-45f6-bd7b-c994773c2eba");

            migrationBuilder.UpdateData(
                table: "Tbl_Role",
                keyColumn: "Id",
                keyValue: "f6e5d4c3-b2a1-0987-fedc-ba0987654321",
                column: "ConcurrencyStamp",
                value: "4496596c-b2d7-4b1f-ac1e-ef955bfee003");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "m_categories");

            migrationBuilder.UpdateData(
                table: "Tbl_Role",
                keyColumn: "Id",
                keyValue: "a1b2c3d4-e5f6-7890-abcd-ef1234567890",
                column: "ConcurrencyStamp",
                value: "595c3786-ff1a-4557-9d97-39eb8ad19a2c");

            migrationBuilder.UpdateData(
                table: "Tbl_Role",
                keyColumn: "Id",
                keyValue: "d3f1a7b2-8c4e-4f3a-9a1e-1a2b3c4d5e6f",
                column: "ConcurrencyStamp",
                value: "f4771b0b-5bb5-46f6-b308-9ef982e00554");

            migrationBuilder.UpdateData(
                table: "Tbl_Role",
                keyColumn: "Id",
                keyValue: "f6e5d4c3-b2a1-0987-fedc-ba0987654321",
                column: "ConcurrencyStamp",
                value: "60e0fe21-59d9-4ab4-82d3-95dccdc688de");
        }
    }
}
