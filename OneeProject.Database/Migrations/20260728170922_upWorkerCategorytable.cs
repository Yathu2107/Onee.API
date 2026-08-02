using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace OneeProject.Database.Migrations
{
    /// <inheritdoc />
    public partial class upWorkerCategorytable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "t_worker_categories",
                columns: table => new
                {
                    FK_user_ID = table.Column<string>(type: "varchar(255)", maxLength: 255, nullable: false, collation: "utf8mb4_general_ci"),
                    Category_id = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_t_worker_categories", x => new { x.FK_user_ID, x.Category_id });
                })
                .Annotation("Relational:Collation", "utf8mb4_general_ci");

            migrationBuilder.UpdateData(
                table: "Tbl_Role",
                keyColumn: "Id",
                keyValue: "a1b2c3d4-e5f6-7890-abcd-ef1234567890",
                column: "ConcurrencyStamp",
                value: "43970929-fc69-4231-91fc-feb2467685b2");

            migrationBuilder.UpdateData(
                table: "Tbl_Role",
                keyColumn: "Id",
                keyValue: "d3f1a7b2-8c4e-4f3a-9a1e-1a2b3c4d5e6f",
                column: "ConcurrencyStamp",
                value: "4e7dc3cd-d7d7-430e-b638-36637983919b");

            migrationBuilder.UpdateData(
                table: "Tbl_Role",
                keyColumn: "Id",
                keyValue: "f6e5d4c3-b2a1-0987-fedc-ba0987654321",
                column: "ConcurrencyStamp",
                value: "0a865558-4d01-4a1e-9a6f-cf3d7fce4088");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "t_worker_categories");

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
    }
}
