using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace NotiPayApi.Migrations
{
    /// <inheritdoc />
    public partial class Appointments : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_PaymentNotices_Users_UserId1",
                table: "PaymentNotices");

            migrationBuilder.DropIndex(
                name: "IX_PaymentNotices_UserId1",
                table: "PaymentNotices");

            migrationBuilder.DropColumn(
                name: "UserId1",
                table: "PaymentNotices");

            migrationBuilder.AddColumn<int>(
                name: "StartSchoolYear",
                table: "Users",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.AlterColumn<Guid>(
                name: "UserId",
                table: "PaymentNotices",
                type: "TEXT",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"),
                oldClrType: typeof(string),
                oldType: "TEXT",
                oldNullable: true);

            migrationBuilder.CreateTable(
                name: "Appointments",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    Time = table.Column<DateTime>(type: "TEXT", nullable: false),
                    ProfessorId = table.Column<Guid>(type: "TEXT", nullable: false),
                    UserId = table.Column<Guid>(type: "TEXT", nullable: false),
                    Description = table.Column<string>(type: "TEXT", nullable: true),
                    StudentName = table.Column<string>(type: "TEXT", nullable: true),
                    StudentEmail = table.Column<string>(type: "TEXT", nullable: true),
                    Status = table.Column<string>(type: "TEXT", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Appointments", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Appointments_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "SchoolYears",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    Year = table.Column<int>(type: "INTEGER", nullable: false),
                    StartDate = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SchoolYears", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "XenditPayments",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    NoticeId = table.Column<Guid>(type: "TEXT", nullable: false),
                    Amount = table.Column<decimal>(type: "TEXT", nullable: false),
                    Currency = table.Column<string>(type: "TEXT", nullable: false),
                    ChannelCode = table.Column<string>(type: "TEXT", nullable: true),
                    Title = table.Column<string>(type: "TEXT", nullable: false),
                    LinkId = table.Column<string>(type: "TEXT", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_XenditPayments", x => x.Id);
                    table.ForeignKey(
                        name: "FK_XenditPayments_PaymentNotices_NoticeId",
                        column: x => x.NoticeId,
                        principalTable: "PaymentNotices",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_PaymentNotices_UserId",
                table: "PaymentNotices",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_Appointments_UserId",
                table: "Appointments",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_XenditPayments_NoticeId",
                table: "XenditPayments",
                column: "NoticeId");

            migrationBuilder.AddForeignKey(
                name: "FK_PaymentNotices_Users_UserId",
                table: "PaymentNotices",
                column: "UserId",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_PaymentNotices_Users_UserId",
                table: "PaymentNotices");

            migrationBuilder.DropTable(
                name: "Appointments");

            migrationBuilder.DropTable(
                name: "SchoolYears");

            migrationBuilder.DropTable(
                name: "XenditPayments");

            migrationBuilder.DropIndex(
                name: "IX_PaymentNotices_UserId",
                table: "PaymentNotices");

            migrationBuilder.DropColumn(
                name: "StartSchoolYear",
                table: "Users");

            migrationBuilder.AlterColumn<string>(
                name: "UserId",
                table: "PaymentNotices",
                type: "TEXT",
                nullable: true,
                oldClrType: typeof(Guid),
                oldType: "TEXT");

            migrationBuilder.AddColumn<Guid>(
                name: "UserId1",
                table: "PaymentNotices",
                type: "TEXT",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_PaymentNotices_UserId1",
                table: "PaymentNotices",
                column: "UserId1");

            migrationBuilder.AddForeignKey(
                name: "FK_PaymentNotices_Users_UserId1",
                table: "PaymentNotices",
                column: "UserId1",
                principalTable: "Users",
                principalColumn: "Id");
        }
    }
}
