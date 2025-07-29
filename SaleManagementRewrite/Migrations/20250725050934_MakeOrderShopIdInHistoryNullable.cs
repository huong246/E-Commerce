using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SaleManagementRewrite.Migrations
{
    /// <inheritdoc />
    public partial class MakeOrderShopIdInHistoryNullable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_OrderHistory_Order_OrderId",
                table: "OrderHistory");

            migrationBuilder.AlterColumn<Guid>(
                name: "OrderId",
                table: "OrderHistory",
                type: "TEXT",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"),
                oldClrType: typeof(Guid),
                oldType: "TEXT",
                oldNullable: true);

            migrationBuilder.AddForeignKey(
                name: "FK_OrderHistory_Order_OrderId",
                table: "OrderHistory",
                column: "OrderId",
                principalTable: "Order",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_OrderHistory_Order_OrderId",
                table: "OrderHistory");

            migrationBuilder.AlterColumn<Guid>(
                name: "OrderId",
                table: "OrderHistory",
                type: "TEXT",
                nullable: true,
                oldClrType: typeof(Guid),
                oldType: "TEXT");

            migrationBuilder.AddForeignKey(
                name: "FK_OrderHistory_Order_OrderId",
                table: "OrderHistory",
                column: "OrderId",
                principalTable: "Order",
                principalColumn: "Id");
        }
    }
}
