using System;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace OneeProject.Database.Migrations
{
    /// <inheritdoc />
    public partial class SavedAddressNotificationComplaintTables : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "t_admin_notifications",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    Title = table.Column<string>(type: "varchar(200)", maxLength: 200, nullable: false, collation: "utf8mb4_general_ci"),
                    Body = table.Column<string>(type: "longtext", nullable: false, collation: "utf8mb4_general_ci"),
                    Audience = table.Column<string>(type: "varchar(20)", maxLength: 20, nullable: false, collation: "utf8mb4_general_ci"),
                    Status = table.Column<string>(type: "varchar(20)", maxLength: 20, nullable: false, collation: "utf8mb4_general_ci"),
                    CreatedBy = table.Column<string>(type: "longtext", nullable: false, collation: "utf8mb4_general_ci"),
                    CreatedOn = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    SentBy = table.Column<string>(type: "longtext", nullable: true, collation: "utf8mb4_general_ci"),
                    SentOn = table.Column<DateTime>(type: "datetime(6)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_t_admin_notifications", x => x.Id);
                })
                .Annotation("Relational:Collation", "utf8mb4_general_ci");

            migrationBuilder.CreateTable(
                name: "t_complaints",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    FK_job_ID = table.Column<int>(type: "int", nullable: false),
                    FK_customer_ID = table.Column<string>(type: "varchar(255)", maxLength: 255, nullable: false, collation: "utf8mb4_general_ci"),
                    FK_worker_ID = table.Column<string>(type: "varchar(255)", maxLength: 255, nullable: false, collation: "utf8mb4_general_ci"),
                    Subject = table.Column<string>(type: "varchar(200)", maxLength: 200, nullable: false, collation: "utf8mb4_general_ci"),
                    Description = table.Column<string>(type: "longtext", nullable: false, collation: "utf8mb4_general_ci"),
                    Status = table.Column<string>(type: "varchar(50)", maxLength: 50, nullable: false, collation: "utf8mb4_general_ci"),
                    Admin_Response = table.Column<string>(type: "longtext", nullable: true, collation: "utf8mb4_general_ci"),
                    CreatedBy = table.Column<string>(type: "longtext", nullable: false, collation: "utf8mb4_general_ci"),
                    CreatedOn = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    LastUpdatedBy = table.Column<string>(type: "longtext", nullable: true, collation: "utf8mb4_general_ci"),
                    LastUpdatedOn = table.Column<DateTime>(type: "datetime(6)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_t_complaints", x => x.Id);
                })
                .Annotation("Relational:Collation", "utf8mb4_general_ci");

            migrationBuilder.CreateTable(
                name: "t_notifications",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    FK_user_ID = table.Column<string>(type: "varchar(255)", maxLength: 255, nullable: false, collation: "utf8mb4_general_ci"),
                    Title = table.Column<string>(type: "varchar(200)", maxLength: 200, nullable: false, collation: "utf8mb4_general_ci"),
                    Body = table.Column<string>(type: "longtext", nullable: false, collation: "utf8mb4_general_ci"),
                    Type = table.Column<string>(type: "varchar(50)", maxLength: 50, nullable: false, collation: "utf8mb4_general_ci"),
                    FK_job_ID = table.Column<int>(type: "int", nullable: true),
                    FK_admin_notification_ID = table.Column<int>(type: "int", nullable: true),
                    Is_Read = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    CreatedOn = table.Column<DateTime>(type: "datetime(6)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_t_notifications", x => x.Id);
                })
                .Annotation("Relational:Collation", "utf8mb4_general_ci");

            migrationBuilder.CreateTable(
                name: "t_saved_addresses",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    FK_user_ID = table.Column<string>(type: "varchar(255)", maxLength: 255, nullable: false, collation: "utf8mb4_general_ci"),
                    Label = table.Column<string>(type: "varchar(50)", maxLength: 50, nullable: false, collation: "utf8mb4_general_ci"),
                    Address_Line = table.Column<string>(type: "varchar(500)", maxLength: 500, nullable: false, collation: "utf8mb4_general_ci"),
                    Latitude = table.Column<double>(type: "double", nullable: false),
                    Longitude = table.Column<double>(type: "double", nullable: false),
                    Is_Default = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    CreatedBy = table.Column<string>(type: "longtext", nullable: false, collation: "utf8mb4_general_ci"),
                    CreatedOn = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    LastUpdatedBy = table.Column<string>(type: "longtext", nullable: true, collation: "utf8mb4_general_ci"),
                    LastUpdatedOn = table.Column<DateTime>(type: "datetime(6)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_t_saved_addresses", x => x.Id);
                })
                .Annotation("Relational:Collation", "utf8mb4_general_ci");

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

            migrationBuilder.CreateIndex(
                name: "IX_t_complaints_FK_job_ID",
                table: "t_complaints",
                column: "FK_job_ID",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_t_notifications_FK_job_ID",
                table: "t_notifications",
                column: "FK_job_ID");

            migrationBuilder.CreateIndex(
                name: "IX_t_notifications_FK_user_ID_Is_Read",
                table: "t_notifications",
                columns: new[] { "FK_user_ID", "Is_Read" });

            migrationBuilder.CreateIndex(
                name: "IX_t_saved_addresses_FK_user_ID",
                table: "t_saved_addresses",
                column: "FK_user_ID");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "t_admin_notifications");

            migrationBuilder.DropTable(
                name: "t_complaints");

            migrationBuilder.DropTable(
                name: "t_notifications");

            migrationBuilder.DropTable(
                name: "t_saved_addresses");

            migrationBuilder.UpdateData(
                table: "Tbl_Role",
                keyColumn: "Id",
                keyValue: "a1b2c3d4-e5f6-7890-abcd-ef1234567890",
                column: "ConcurrencyStamp",
                value: "039abe51-6bd2-4fee-ba3a-ecad993e2d71");

            migrationBuilder.UpdateData(
                table: "Tbl_Role",
                keyColumn: "Id",
                keyValue: "d3f1a7b2-8c4e-4f3a-9a1e-1a2b3c4d5e6f",
                column: "ConcurrencyStamp",
                value: "2be5776d-7994-426a-83a3-6d05788c963a");

            migrationBuilder.UpdateData(
                table: "Tbl_Role",
                keyColumn: "Id",
                keyValue: "f6e5d4c3-b2a1-0987-fedc-ba0987654321",
                column: "ConcurrencyStamp",
                value: "05d2e56d-2b99-480c-853d-4f1710fa509e");
        }
    }
}
