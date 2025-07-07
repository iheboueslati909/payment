using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Payment.Migrations
{
    /// <inheritdoc />
    public partial class AddedPaymentProviderProperty : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "Provider",
                table: "payments",
                type: "integer",
                nullable: false,
                defaultValue: 0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Provider",
                table: "payments");
        }
    }
}
