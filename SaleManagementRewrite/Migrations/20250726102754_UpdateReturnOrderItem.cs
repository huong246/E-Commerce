using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SaleManagementRewrite.Migrations
{
    /// <inheritdoc />
    public partial class UpdateReturnOrderItem : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_OrderHistory_OrderShop_OrderShopId",
                table: "OrderHistory");

            migrationBuilder.DropForeignKey(
                name: "FK_OrderItem_Order_OrderId",
                table: "OrderItem");

            migrationBuilder.DropForeignKey(
                name: "FK_OrderItem_Users_UserId",
                table: "OrderItem");

            migrationBuilder.DropForeignKey(
                name: "FK_OrderShop_Users_UserId",
                table: "OrderShop");

            migrationBuilder.DropForeignKey(
                name: "FK_ReturnOrder_OrderShop_OrderShopId",
                table: "ReturnOrder");

            migrationBuilder.DropForeignKey(
                name: "FK_ReturnOrder_Order_OrderId",
                table: "ReturnOrder");

            migrationBuilder.DropIndex(
                name: "IX_ReturnOrder_OrderShopId",
                table: "ReturnOrder");

            migrationBuilder.DropIndex(
                name: "IX_OrderShop_UserId",
                table: "OrderShop");

            migrationBuilder.DropIndex(
                name: "IX_OrderItem_OrderId",
                table: "OrderItem");

            migrationBuilder.DropIndex(
                name: "IX_OrderItem_UserId",
                table: "OrderItem");

            migrationBuilder.DropIndex(
                name: "IX_OrderHistory_OrderShopId",
                table: "OrderHistory");

            migrationBuilder.DropColumn(
                name: "Sale",
                table: "Voucher");

            migrationBuilder.DropColumn(
                name: "TotalSale",
                table: "Voucher");

            migrationBuilder.DropColumn(
                name: "OrderShopId",
                table: "ReturnOrder");

            migrationBuilder.DropColumn(
                name: "Quantity",
                table: "ReturnOrder");

            migrationBuilder.DropColumn(
                name: "Reason",
                table: "ReturnOrder");

            migrationBuilder.DropColumn(
                name: "UserId",
                table: "OrderShop");

            migrationBuilder.DropColumn(
                name: "Status",
                table: "OrderHistory");

            migrationBuilder.DropColumn(
                name: "DeliveredDate",
                table: "Order");

            migrationBuilder.DropColumn(
                name: "OrderTime",
                table: "Order");

            migrationBuilder.RenameColumn(
                name: "SubtotalShop",
                table: "OrderShop",
                newName: "SubTotalShop");

            migrationBuilder.RenameColumn(
                name: "UserId",
                table: "OrderItem",
                newName: "TotalAmount");

            migrationBuilder.RenameColumn(
                name: "OrderId",
                table: "OrderItem",
                newName: "Price");

            migrationBuilder.RenameColumn(
                name: "ToTalAmount",
                table: "Order",
                newName: "TotalAmount");

            migrationBuilder.RenameColumn(
                name: "Subtotal",
                table: "Order",
                newName: "TotalSubtotal");

            migrationBuilder.RenameColumn(
                name: "ShippingFee",
                table: "Order",
                newName: "TotalShippingFee");

            migrationBuilder.AlterColumn<Guid>(
                name: "OrderId",
                table: "ReturnOrder",
                type: "TEXT",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"),
                oldClrType: typeof(Guid),
                oldType: "TEXT",
                oldNullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ReturnShippingTrackingCode",
                table: "ReturnOrder",
                type: "TEXT",
                maxLength: 10000,
                nullable: true);

            migrationBuilder.AlterColumn<int>(
                name: "Status",
                table: "OrderShop",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(int),
                oldType: "INTEGER",
                oldNullable: true);

            migrationBuilder.AlterColumn<DateTime>(
                name: "DeliveredDate",
                table: "OrderShop",
                type: "TEXT",
                nullable: true,
                oldClrType: typeof(DateTime),
                oldType: "TEXT");

            migrationBuilder.AddColumn<string>(
                name: "VoucherShopCode",
                table: "OrderShop",
                type: "TEXT",
                maxLength: 10000000,
                nullable: true);

            migrationBuilder.AlterColumn<int>(
                name: "Status",
                table: "OrderItem",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(int),
                oldType: "INTEGER",
                oldNullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "ChangedByUserId",
                table: "OrderHistory",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "FromStatus",
                table: "OrderHistory",
                type: "TEXT",
                maxLength: 10000,
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "OrderItemId",
                table: "OrderHistory",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ToStatus",
                table: "OrderHistory",
                type: "TEXT",
                maxLength: 10000,
                nullable: true);

            migrationBuilder.AlterColumn<decimal>(
                name: "Price",
                table: "Item",
                type: "TEXT",
                nullable: false,
                defaultValue: 0m,
                oldClrType: typeof(decimal),
                oldType: "TEXT",
                oldNullable: true);

            migrationBuilder.CreateTable(
                name: "ReturnOrderItem",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    ReturnOrderId = table.Column<Guid>(type: "TEXT", nullable: true),
                    OrderItemId = table.Column<Guid>(type: "TEXT", nullable: false),
                    Status = table.Column<int>(type: "INTEGER", nullable: false),
                    Reason = table.Column<string>(type: "TEXT", maxLength: 1000000000, nullable: true),
                    Quantity = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ReturnOrderItem", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ReturnOrderItem_OrderItem_OrderItemId",
                        column: x => x.OrderItemId,
                        principalTable: "OrderItem",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ReturnOrderItem_ReturnOrder_ReturnOrderId",
                        column: x => x.ReturnOrderId,
                        principalTable: "ReturnOrder",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateIndex(
                name: "IX_Voucher_ShopId",
                table: "Voucher",
                column: "ShopId");

            migrationBuilder.CreateIndex(
                name: "IX_OrderShop_VoucherShopId",
                table: "OrderShop",
                column: "VoucherShopId");

            migrationBuilder.CreateIndex(
                name: "IX_ReturnOrderItem_OrderItemId",
                table: "ReturnOrderItem",
                column: "OrderItemId");

            migrationBuilder.CreateIndex(
                name: "IX_ReturnOrderItem_ReturnOrderId",
                table: "ReturnOrderItem",
                column: "ReturnOrderId");

            migrationBuilder.AddForeignKey(
                name: "FK_OrderShop_Voucher_VoucherShopId",
                table: "OrderShop",
                column: "VoucherShopId",
                principalTable: "Voucher",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_ReturnOrder_Order_OrderId",
                table: "ReturnOrder",
                column: "OrderId",
                principalTable: "Order",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Voucher_Shop_ShopId",
                table: "Voucher",
                column: "ShopId",
                principalTable: "Shop",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_OrderShop_Voucher_VoucherShopId",
                table: "OrderShop");

            migrationBuilder.DropForeignKey(
                name: "FK_ReturnOrder_Order_OrderId",
                table: "ReturnOrder");

            migrationBuilder.DropForeignKey(
                name: "FK_Voucher_Shop_ShopId",
                table: "Voucher");

            migrationBuilder.DropTable(
                name: "ReturnOrderItem");

            migrationBuilder.DropIndex(
                name: "IX_Voucher_ShopId",
                table: "Voucher");

            migrationBuilder.DropIndex(
                name: "IX_OrderShop_VoucherShopId",
                table: "OrderShop");

            migrationBuilder.DropColumn(
                name: "ReturnShippingTrackingCode",
                table: "ReturnOrder");

            migrationBuilder.DropColumn(
                name: "VoucherShopCode",
                table: "OrderShop");

            migrationBuilder.DropColumn(
                name: "ChangedByUserId",
                table: "OrderHistory");

            migrationBuilder.DropColumn(
                name: "FromStatus",
                table: "OrderHistory");

            migrationBuilder.DropColumn(
                name: "OrderItemId",
                table: "OrderHistory");

            migrationBuilder.DropColumn(
                name: "ToStatus",
                table: "OrderHistory");

            migrationBuilder.RenameColumn(
                name: "SubTotalShop",
                table: "OrderShop",
                newName: "SubtotalShop");

            migrationBuilder.RenameColumn(
                name: "TotalAmount",
                table: "OrderItem",
                newName: "UserId");

            migrationBuilder.RenameColumn(
                name: "Price",
                table: "OrderItem",
                newName: "OrderId");

            migrationBuilder.RenameColumn(
                name: "TotalAmount",
                table: "Order",
                newName: "ToTalAmount");

            migrationBuilder.RenameColumn(
                name: "TotalSubtotal",
                table: "Order",
                newName: "Subtotal");

            migrationBuilder.RenameColumn(
                name: "TotalShippingFee",
                table: "Order",
                newName: "ShippingFee");

            migrationBuilder.AddColumn<int>(
                name: "Sale",
                table: "Voucher",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<decimal>(
                name: "TotalSale",
                table: "Voucher",
                type: "TEXT",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AlterColumn<Guid>(
                name: "OrderId",
                table: "ReturnOrder",
                type: "TEXT",
                nullable: true,
                oldClrType: typeof(Guid),
                oldType: "TEXT");

            migrationBuilder.AddColumn<Guid>(
                name: "OrderShopId",
                table: "ReturnOrder",
                type: "TEXT",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<int>(
                name: "Quantity",
                table: "ReturnOrder",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "Reason",
                table: "ReturnOrder",
                type: "TEXT",
                maxLength: 1000000000,
                nullable: true);

            migrationBuilder.AlterColumn<int>(
                name: "Status",
                table: "OrderShop",
                type: "INTEGER",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "INTEGER");

            migrationBuilder.AlterColumn<DateTime>(
                name: "DeliveredDate",
                table: "OrderShop",
                type: "TEXT",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified),
                oldClrType: typeof(DateTime),
                oldType: "TEXT",
                oldNullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "UserId",
                table: "OrderShop",
                type: "TEXT",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AlterColumn<int>(
                name: "Status",
                table: "OrderItem",
                type: "INTEGER",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "INTEGER");

            migrationBuilder.AddColumn<int>(
                name: "Status",
                table: "OrderHistory",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "DeliveredDate",
                table: "Order",
                type: "TEXT",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<DateTime>(
                name: "OrderTime",
                table: "Order",
                type: "TEXT",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AlterColumn<decimal>(
                name: "Price",
                table: "Item",
                type: "TEXT",
                nullable: true,
                oldClrType: typeof(decimal),
                oldType: "TEXT");

            migrationBuilder.CreateIndex(
                name: "IX_ReturnOrder_OrderShopId",
                table: "ReturnOrder",
                column: "OrderShopId");

            migrationBuilder.CreateIndex(
                name: "IX_OrderShop_UserId",
                table: "OrderShop",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_OrderItem_OrderId",
                table: "OrderItem",
                column: "OrderId");

            migrationBuilder.CreateIndex(
                name: "IX_OrderItem_UserId",
                table: "OrderItem",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_OrderHistory_OrderShopId",
                table: "OrderHistory",
                column: "OrderShopId");

            migrationBuilder.AddForeignKey(
                name: "FK_OrderHistory_OrderShop_OrderShopId",
                table: "OrderHistory",
                column: "OrderShopId",
                principalTable: "OrderShop",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_OrderItem_Order_OrderId",
                table: "OrderItem",
                column: "OrderId",
                principalTable: "Order",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_OrderItem_Users_UserId",
                table: "OrderItem",
                column: "UserId",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_OrderShop_Users_UserId",
                table: "OrderShop",
                column: "UserId",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_ReturnOrder_OrderShop_OrderShopId",
                table: "ReturnOrder",
                column: "OrderShopId",
                principalTable: "OrderShop",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_ReturnOrder_Order_OrderId",
                table: "ReturnOrder",
                column: "OrderId",
                principalTable: "Order",
                principalColumn: "Id");
        }
    }
}
