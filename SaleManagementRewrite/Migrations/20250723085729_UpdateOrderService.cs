using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SaleManagementRewrite.Migrations
{
    /// <inheritdoc />
    public partial class UpdateOrderService : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Notes",
                table: "OrderShop",
                type: "TEXT",
                maxLength: 1000000,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Notes",
                table: "OrderShop");
        }
    }
}
