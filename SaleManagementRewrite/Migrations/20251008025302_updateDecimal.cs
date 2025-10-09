using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SaleManagementRewirte.Migrations
{
    /// <inheritdoc />
    public partial class updateDecimal : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<decimal>(
                name: "Value",
                table: "Voucher",
                type: "TEXT",
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "NUMERIC");

            migrationBuilder.AlterColumn<decimal>(
                name: "MinSpend",
                table: "Voucher",
                type: "TEXT",
                nullable: true,
                oldClrType: typeof(decimal),
                oldType: "NUMERIC",
                oldNullable: true);

            migrationBuilder.AlterColumn<decimal>(
                name: "Maxvalue",
                table: "Voucher",
                type: "TEXT",
                nullable: true,
                oldClrType: typeof(decimal),
                oldType: "NUMERIC",
                oldNullable: true);

            migrationBuilder.AlterColumn<decimal>(
                name: "Amount",
                table: "Transaction",
                type: "TEXT",
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "NUMERIC");

            migrationBuilder.AlterColumn<decimal>(
                name: "Amount",
                table: "ReturnOrderItem",
                type: "TEXT",
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "NUMERIC");

            migrationBuilder.AlterColumn<decimal>(
                name: "Amount",
                table: "ReturnOrder",
                type: "TEXT",
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "NUMERIC");

            migrationBuilder.AlterColumn<decimal>(
                name: "TotalShopAmount",
                table: "OrderShop",
                type: "TEXT",
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "NUMERIC");

            migrationBuilder.AlterColumn<decimal>(
                name: "SubTotalShop",
                table: "OrderShop",
                type: "TEXT",
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "NUMERIC");

            migrationBuilder.AlterColumn<decimal>(
                name: "ShippingFee",
                table: "OrderShop",
                type: "TEXT",
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "NUMERIC");

            migrationBuilder.AlterColumn<decimal>(
                name: "DiscountShopAmount",
                table: "OrderShop",
                type: "TEXT",
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "NUMERIC");

            migrationBuilder.AlterColumn<decimal>(
                name: "TotalAmount",
                table: "OrderItem",
                type: "TEXT",
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "NUMERIC");

            migrationBuilder.AlterColumn<decimal>(
                name: "Price",
                table: "OrderItem",
                type: "TEXT",
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "NUMERIC");

            migrationBuilder.AlterColumn<decimal>(
                name: "TotalSubtotal",
                table: "Order",
                type: "TEXT",
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "NUMERIC");

            migrationBuilder.AlterColumn<decimal>(
                name: "TotalShippingFee",
                table: "Order",
                type: "TEXT",
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "NUMERIC");

            migrationBuilder.AlterColumn<decimal>(
                name: "TotalAmount",
                table: "Order",
                type: "TEXT",
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "NUMERIC");

            migrationBuilder.AlterColumn<decimal>(
                name: "DiscountShippingAmount",
                table: "Order",
                type: "TEXT",
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "NUMERIC");

            migrationBuilder.AlterColumn<decimal>(
                name: "DiscountProductAmount",
                table: "Order",
                type: "TEXT",
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "NUMERIC");

            migrationBuilder.AlterColumn<decimal>(
                name: "Price",
                table: "Item",
                type: "TEXT",
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "NUMERIC");

            migrationBuilder.AlterColumn<decimal>(
                name: "Amount",
                table: "CancelRequest",
                type: "TEXT",
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "NUMERIC");

            migrationBuilder.AlterColumn<decimal>(
                name: "Balance",
                table: "AspNetUsers",
                type: "TEXT",
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "NUMERIC");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<decimal>(
                name: "Value",
                table: "Voucher",
                type: "NUMERIC",
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "TEXT");

            migrationBuilder.AlterColumn<decimal>(
                name: "MinSpend",
                table: "Voucher",
                type: "NUMERIC",
                nullable: true,
                oldClrType: typeof(decimal),
                oldType: "TEXT",
                oldNullable: true);

            migrationBuilder.AlterColumn<decimal>(
                name: "Maxvalue",
                table: "Voucher",
                type: "NUMERIC",
                nullable: true,
                oldClrType: typeof(decimal),
                oldType: "TEXT",
                oldNullable: true);

            migrationBuilder.AlterColumn<decimal>(
                name: "Amount",
                table: "Transaction",
                type: "NUMERIC",
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "TEXT");

            migrationBuilder.AlterColumn<decimal>(
                name: "Amount",
                table: "ReturnOrderItem",
                type: "NUMERIC",
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "TEXT");

            migrationBuilder.AlterColumn<decimal>(
                name: "Amount",
                table: "ReturnOrder",
                type: "NUMERIC",
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "TEXT");

            migrationBuilder.AlterColumn<decimal>(
                name: "TotalShopAmount",
                table: "OrderShop",
                type: "NUMERIC",
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "TEXT");

            migrationBuilder.AlterColumn<decimal>(
                name: "SubTotalShop",
                table: "OrderShop",
                type: "NUMERIC",
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "TEXT");

            migrationBuilder.AlterColumn<decimal>(
                name: "ShippingFee",
                table: "OrderShop",
                type: "NUMERIC",
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "TEXT");

            migrationBuilder.AlterColumn<decimal>(
                name: "DiscountShopAmount",
                table: "OrderShop",
                type: "NUMERIC",
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "TEXT");

            migrationBuilder.AlterColumn<decimal>(
                name: "TotalAmount",
                table: "OrderItem",
                type: "NUMERIC",
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "TEXT");

            migrationBuilder.AlterColumn<decimal>(
                name: "Price",
                table: "OrderItem",
                type: "NUMERIC",
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "TEXT");

            migrationBuilder.AlterColumn<decimal>(
                name: "TotalSubtotal",
                table: "Order",
                type: "NUMERIC",
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "TEXT");

            migrationBuilder.AlterColumn<decimal>(
                name: "TotalShippingFee",
                table: "Order",
                type: "NUMERIC",
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "TEXT");

            migrationBuilder.AlterColumn<decimal>(
                name: "TotalAmount",
                table: "Order",
                type: "NUMERIC",
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "TEXT");

            migrationBuilder.AlterColumn<decimal>(
                name: "DiscountShippingAmount",
                table: "Order",
                type: "NUMERIC",
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "TEXT");

            migrationBuilder.AlterColumn<decimal>(
                name: "DiscountProductAmount",
                table: "Order",
                type: "NUMERIC",
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "TEXT");

            migrationBuilder.AlterColumn<decimal>(
                name: "Price",
                table: "Item",
                type: "NUMERIC",
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "TEXT");

            migrationBuilder.AlterColumn<decimal>(
                name: "Amount",
                table: "CancelRequest",
                type: "NUMERIC",
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "TEXT");

            migrationBuilder.AlterColumn<decimal>(
                name: "Balance",
                table: "AspNetUsers",
                type: "NUMERIC",
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "TEXT");
        }
    }
}
