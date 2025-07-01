using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Payment.Migrations
{
    /// <inheritdoc />
    public partial class StripeCheckoutSession : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Provider",
                table: "payments");

            migrationBuilder.DropColumn(
                name: "ProviderPaymentId",
                table: "payments");

            migrationBuilder.AddColumn<string>(
                name: "CheckoutUrl",
                table: "payments",
                type: "character varying(500)",
                maxLength: 500,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "IdempotencyKey",
                table: "payments",
                type: "character varying(100)",
                maxLength: 100,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "IntendId",
                table: "payments",
                type: "character varying(100)",
                maxLength: 100,
                nullable: false,
                defaultValue: "");

            migrationBuilder.CreateIndex(
                name: "IX_payments_AppId_UserId_IdempotencyKey",
                table: "payments",
                columns: new[] { "AppId", "UserId", "IdempotencyKey" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_payments_AppId_UserId_IdempotencyKey",
                table: "payments");

            migrationBuilder.DropColumn(
                name: "CheckoutUrl",
                table: "payments");

            migrationBuilder.DropColumn(
                name: "IdempotencyKey",
                table: "payments");

            migrationBuilder.DropColumn(
                name: "IntendId",
                table: "payments");

            migrationBuilder.AddColumn<string>(
                name: "Provider",
                table: "payments",
                type: "character varying(20)",
                maxLength: 20,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "ProviderPaymentId",
                table: "payments",
                type: "character varying(100)",
                maxLength: 100,
                nullable: true);
        }
    }
}
