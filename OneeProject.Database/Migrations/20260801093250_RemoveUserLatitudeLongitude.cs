using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace OneeProject.Database.Migrations
{
    /// <inheritdoc />
    public partial class RemoveUserLatitudeLongitude : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Latitude",
                table: "t_user");

            migrationBuilder.DropColumn(
                name: "Longitude",
                table: "t_user");

            migrationBuilder.UpdateData(
                table: "Tbl_Role",
                keyColumn: "Id",
                keyValue: "a1b2c3d4-e5f6-7890-abcd-ef1234567890",
                column: "ConcurrencyStamp",
                value: "892dbc04-d054-4ebd-886b-89dd4e2941e7");

            migrationBuilder.UpdateData(
                table: "Tbl_Role",
                keyColumn: "Id",
                keyValue: "d3f1a7b2-8c4e-4f3a-9a1e-1a2b3c4d5e6f",
                column: "ConcurrencyStamp",
                value: "6e5c36d3-6b3b-47d2-bfe7-1969af586cae");

            migrationBuilder.UpdateData(
                table: "Tbl_Role",
                keyColumn: "Id",
                keyValue: "f6e5d4c3-b2a1-0987-fedc-ba0987654321",
                column: "ConcurrencyStamp",
                value: "fe398a99-0b07-4d28-8db8-20e1b1ec326c");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<double>(
                name: "Latitude",
                table: "t_user",
                type: "double",
                nullable: true);

            migrationBuilder.AddColumn<double>(
                name: "Longitude",
                table: "t_user",
                type: "double",
                nullable: true);

            migrationBuilder.UpdateData(
                table: "Tbl_Role",
                keyColumn: "Id",
                keyValue: "a1b2c3d4-e5f6-7890-abcd-ef1234567890",
                column: "ConcurrencyStamp",
                value: "4272d7f0-267b-4269-ac45-b6f83d92f477");

            migrationBuilder.UpdateData(
                table: "Tbl_Role",
                keyColumn: "Id",
                keyValue: "d3f1a7b2-8c4e-4f3a-9a1e-1a2b3c4d5e6f",
                column: "ConcurrencyStamp",
                value: "99804217-7621-4dfa-b695-fc1ee3c20a9f");

            migrationBuilder.UpdateData(
                table: "Tbl_Role",
                keyColumn: "Id",
                keyValue: "f6e5d4c3-b2a1-0987-fedc-ba0987654321",
                column: "ConcurrencyStamp",
                value: "2d5750d2-05fe-4b48-8f85-27fd443539ca");
        }
    }
}
