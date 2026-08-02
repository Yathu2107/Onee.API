using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace OneeProject.Database.Migrations
{
    /// <inheritdoc />
    public partial class UserIsOnlineColumn : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "IsOnline",
                table: "t_user",
                type: "tinyint(1)",
                nullable: false,
                defaultValue: false);

            migrationBuilder.UpdateData(
                table: "Tbl_Role",
                keyColumn: "Id",
                keyValue: "a1b2c3d4-e5f6-7890-abcd-ef1234567890",
                column: "ConcurrencyStamp",
                value: "13762635-7cd9-458e-9738-034a402a0ba0");

            migrationBuilder.UpdateData(
                table: "Tbl_Role",
                keyColumn: "Id",
                keyValue: "d3f1a7b2-8c4e-4f3a-9a1e-1a2b3c4d5e6f",
                column: "ConcurrencyStamp",
                value: "12017689-7743-47ef-8bfd-c297ba6cd24a");

            migrationBuilder.UpdateData(
                table: "Tbl_Role",
                keyColumn: "Id",
                keyValue: "f6e5d4c3-b2a1-0987-fedc-ba0987654321",
                column: "ConcurrencyStamp",
                value: "594de03c-fba7-4975-b77b-fe992b4d208a");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "IsOnline",
                table: "t_user");

            migrationBuilder.UpdateData(
                table: "Tbl_Role",
                keyColumn: "Id",
                keyValue: "a1b2c3d4-e5f6-7890-abcd-ef1234567890",
                column: "ConcurrencyStamp",
                value: "a1rating01-0001-4aa1-8b01-jobrating0001");

            migrationBuilder.UpdateData(
                table: "Tbl_Role",
                keyColumn: "Id",
                keyValue: "d3f1a7b2-8c4e-4f3a-9a1e-1a2b3c4d5e6f",
                column: "ConcurrencyStamp",
                value: "a1rating01-0002-4aa1-8b01-jobrating0002");

            migrationBuilder.UpdateData(
                table: "Tbl_Role",
                keyColumn: "Id",
                keyValue: "f6e5d4c3-b2a1-0987-fedc-ba0987654321",
                column: "ConcurrencyStamp",
                value: "a1rating01-0003-4aa1-8b01-jobrating0003");
        }
    }
}
