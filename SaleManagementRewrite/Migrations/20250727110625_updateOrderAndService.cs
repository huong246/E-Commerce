using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SaleManagementRewrite.Migrations
{
    /// <inheritdoc />
    public partial class updateOrderAndService : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ReturnShippingTrackingCode",
                table: "ReturnOrder");

            migrationBuilder.AlterColumn<decimal>(
                name: "Value",
                table: "Voucher",
                type: "TEXT",
                nullable: false,
                oldClrType: typeof(double),
                oldType: "REAL");

            migrationBuilder.AddColumn<string>(
                name: "ReturnShippingTrackingCode",
                table: "ReturnOrderItem",
                type: "TEXT",
                maxLength: 10000,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "TrackingCode",
                table: "OrderShop",
                type: "TEXT",
                maxLength: 1000000,
                nullable: true);

            migrationBuilder.CreateTable(
                name: "Transaction",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    Amount = table.Column<decimal>(type: "TEXT", nullable: false),
                    Type = table.Column<int>(type: "INTEGER", nullable: false),
                    Status = table.Column<int>(type: "INTEGER", nullable: false),
                    FromUserId = table.Column<Guid>(type: "TEXT", nullable: true),
                    ToUserId = table.Column<Guid>(type: "TEXT", nullable: true),
                    OrderId = table.Column<Guid>(type: "TEXT", nullable: false),
                    CreateAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    Notes = table.Column<string>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Transaction", x => x.Id);
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Transaction");

            migrationBuilder.DropColumn(
                name: "ReturnShippingTrackingCode",
                table: "ReturnOrderItem");

            migrationBuilder.DropColumn(
                name: "TrackingCode",
                table: "OrderShop");

            migrationBuilder.AlterColumn<double>(
                name: "Value",
                table: "Voucher",
                type: "REAL",
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "TEXT");

            migrationBuilder.AddColumn<string>(
                name: "ReturnShippingTrackingCode",
                table: "ReturnOrder",
                type: "TEXT",
                maxLength: 10000,
                nullable: true);
        }
    }
}
