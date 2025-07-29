using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SaleManagementRewrite.Migrations
{
    /// <inheritdoc />
    public partial class UpdateOderServiceAndOrderShop : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_CartItem_OrderShop_OrderShopId",
                table: "CartItem");

            migrationBuilder.DropIndex(
                name: "IX_CartItem_OrderShopId",
                table: "CartItem");

            migrationBuilder.DropColumn(
                name: "OderId",
                table: "OrderHistory");

            migrationBuilder.DropColumn(
                name: "OrderShopId",
                table: "CartItem");

            migrationBuilder.AddColumn<DateTime>(
                name: "DeliveredDate",
                table: "OrderShop",
                type: "TEXT",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<decimal>(
                name: "DiscountShopAmount",
                table: "OrderShop",
                type: "TEXT",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "ShippingFee",
                table: "OrderShop",
                type: "TEXT",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "TotalShopAmount",
                table: "OrderShop",
                type: "TEXT",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<Guid>(
                name: "UserId",
                table: "OrderShop",
                type: "TEXT",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<Guid>(
                name: "VoucherShopId",
                table: "OrderShop",
                type: "TEXT",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<Guid>(
                name: "OrderShopId",
                table: "OrderHistory",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "DeliveredDate",
                table: "Order",
                type: "TEXT",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.CreateTable(
                name: "OrderItem",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    OrderId = table.Column<Guid>(type: "TEXT", nullable: false),
                    OrderShopId = table.Column<Guid>(type: "TEXT", nullable: false),
                    ItemId = table.Column<Guid>(type: "TEXT", nullable: false),
                    Quantity = table.Column<int>(type: "INTEGER", nullable: false),
                    ShopId = table.Column<Guid>(type: "TEXT", nullable: false),
                    UserId = table.Column<Guid>(type: "TEXT", nullable: false),
                    Status = table.Column<int>(type: "INTEGER", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OrderItem", x => x.Id);
                    table.ForeignKey(
                        name: "FK_OrderItem_Item_ItemId",
                        column: x => x.ItemId,
                        principalTable: "Item",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_OrderItem_OrderShop_OrderShopId",
                        column: x => x.OrderShopId,
                        principalTable: "OrderShop",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_OrderItem_Order_OrderId",
                        column: x => x.OrderId,
                        principalTable: "Order",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_OrderItem_Shop_ShopId",
                        column: x => x.ShopId,
                        principalTable: "Shop",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_OrderItem_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ReturnOrder",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    OrderId = table.Column<Guid>(type: "TEXT", nullable: true),
                    OrderShopId = table.Column<Guid>(type: "TEXT", nullable: false),
                    RequestAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    ReviewAt = table.Column<DateTime>(type: "TEXT", nullable: true),
                    Status = table.Column<int>(type: "INTEGER", nullable: false),
                    Reason = table.Column<string>(type: "TEXT", maxLength: 1000000000, nullable: true),
                    UserId = table.Column<Guid>(type: "TEXT", nullable: false),
                    Quantity = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ReturnOrder", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ReturnOrder_OrderShop_OrderShopId",
                        column: x => x.OrderShopId,
                        principalTable: "OrderShop",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ReturnOrder_Order_OrderId",
                        column: x => x.OrderId,
                        principalTable: "Order",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_ReturnOrder_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_OrderShop_UserId",
                table: "OrderShop",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_OrderHistory_OrderShopId",
                table: "OrderHistory",
                column: "OrderShopId");

            migrationBuilder.CreateIndex(
                name: "IX_OrderItem_ItemId",
                table: "OrderItem",
                column: "ItemId");

            migrationBuilder.CreateIndex(
                name: "IX_OrderItem_OrderId",
                table: "OrderItem",
                column: "OrderId");

            migrationBuilder.CreateIndex(
                name: "IX_OrderItem_OrderShopId",
                table: "OrderItem",
                column: "OrderShopId");

            migrationBuilder.CreateIndex(
                name: "IX_OrderItem_ShopId",
                table: "OrderItem",
                column: "ShopId");

            migrationBuilder.CreateIndex(
                name: "IX_OrderItem_UserId",
                table: "OrderItem",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_ReturnOrder_OrderId",
                table: "ReturnOrder",
                column: "OrderId");

            migrationBuilder.CreateIndex(
                name: "IX_ReturnOrder_OrderShopId",
                table: "ReturnOrder",
                column: "OrderShopId");

            migrationBuilder.CreateIndex(
                name: "IX_ReturnOrder_UserId",
                table: "ReturnOrder",
                column: "UserId");

            migrationBuilder.AddForeignKey(
                name: "FK_OrderHistory_OrderShop_OrderShopId",
                table: "OrderHistory",
                column: "OrderShopId",
                principalTable: "OrderShop",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_OrderShop_Users_UserId",
                table: "OrderShop",
                column: "UserId",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_OrderHistory_OrderShop_OrderShopId",
                table: "OrderHistory");

            migrationBuilder.DropForeignKey(
                name: "FK_OrderShop_Users_UserId",
                table: "OrderShop");

            migrationBuilder.DropTable(
                name: "OrderItem");

            migrationBuilder.DropTable(
                name: "ReturnOrder");

            migrationBuilder.DropIndex(
                name: "IX_OrderShop_UserId",
                table: "OrderShop");

            migrationBuilder.DropIndex(
                name: "IX_OrderHistory_OrderShopId",
                table: "OrderHistory");

            migrationBuilder.DropColumn(
                name: "DeliveredDate",
                table: "OrderShop");

            migrationBuilder.DropColumn(
                name: "DiscountShopAmount",
                table: "OrderShop");

            migrationBuilder.DropColumn(
                name: "ShippingFee",
                table: "OrderShop");

            migrationBuilder.DropColumn(
                name: "TotalShopAmount",
                table: "OrderShop");

            migrationBuilder.DropColumn(
                name: "UserId",
                table: "OrderShop");

            migrationBuilder.DropColumn(
                name: "VoucherShopId",
                table: "OrderShop");

            migrationBuilder.DropColumn(
                name: "OrderShopId",
                table: "OrderHistory");

            migrationBuilder.DropColumn(
                name: "DeliveredDate",
                table: "Order");

            migrationBuilder.AddColumn<Guid>(
                name: "OderId",
                table: "OrderHistory",
                type: "TEXT",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<Guid>(
                name: "OrderShopId",
                table: "CartItem",
                type: "TEXT",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_CartItem_OrderShopId",
                table: "CartItem",
                column: "OrderShopId");

            migrationBuilder.AddForeignKey(
                name: "FK_CartItem_OrderShop_OrderShopId",
                table: "CartItem",
                column: "OrderShopId",
                principalTable: "OrderShop",
                principalColumn: "Id");
        }
    }
}
