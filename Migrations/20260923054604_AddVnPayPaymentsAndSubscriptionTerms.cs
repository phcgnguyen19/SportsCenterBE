using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SportsCenterAPI.Migrations
{
    /// <inheritdoc />
    public partial class AddVnPayPaymentsAndSubscriptionTerms : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Payments_SubscriptionId",
                table: "Payments");

            migrationBuilder.DropIndex(
                name: "IX_MemberSubscriptions_MemberId",
                table: "MemberSubscriptions");

            migrationBuilder.AlterColumn<string>(
                name: "TransactionReference",
                table: "Payments",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)",
                oldNullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CheckoutUrl",
                table: "Payments",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "ExpiresAt",
                table: "Payments",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "GatewayResponseCode",
                table: "Payments",
                type: "nvarchar(2)",
                maxLength: 2,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "GatewayTransactionNo",
                table: "Payments",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "Status",
                table: "MemberSubscriptions",
                type: "nvarchar(20)",
                maxLength: 20,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)");

            migrationBuilder.AlterColumn<DateTime>(
                name: "StartDate",
                table: "MemberSubscriptions",
                type: "datetime2",
                nullable: true,
                oldClrType: typeof(DateTime),
                oldType: "datetime2");

            migrationBuilder.AlterColumn<DateTime>(
                name: "EndDate",
                table: "MemberSubscriptions",
                type: "datetime2",
                nullable: true,
                oldClrType: typeof(DateTime),
                oldType: "datetime2");

            migrationBuilder.AddColumn<decimal>(
                name: "AgreedPrice",
                table: "MemberSubscriptions",
                type: "decimal(18,2)",
                precision: 18,
                scale: 2,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<DateTime>(
                name: "CreatedAt",
                table: "MemberSubscriptions",
                type: "datetime2",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<int>(
                name: "DurationInDays",
                table: "MemberSubscriptions",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<byte[]>(
                name: "RowVersion",
                table: "MemberSubscriptions",
                type: "rowversion",
                rowVersion: true,
                nullable: false,
                defaultValue: new byte[0]);

            migrationBuilder.AddColumn<string>(
                name: "FitnessGoal",
                table: "Members",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Gender",
                table: "Members",
                type: "nvarchar(10)",
                maxLength: 10,
                nullable: true);

            // Older schemas did not snapshot purchase terms. Prefer the recorded payment
            // and actual dates; otherwise use the package's current terms for legacy rows.
            migrationBuilder.Sql("""
                UPDATE s SET
                    AgreedPrice = COALESCE((SELECT TOP (1) p.Amount FROM Payments p
                        WHERE p.SubscriptionId = s.Id AND p.Status = N'Completed' ORDER BY p.PaymentDate DESC), pkg.Price),
                    DurationInDays = CASE WHEN s.StartDate IS NOT NULL AND s.EndDate > s.StartDate
                        AND DATEDIFF(day, s.StartDate, s.EndDate) BETWEEN 1 AND 36500
                        THEN DATEDIFF(day, s.StartDate, s.EndDate) ELSE pkg.DurationInDays END,
                    CreatedAt = CASE WHEN s.StartDate >= '20000101' THEN s.StartDate ELSE SYSUTCDATETIME() END
                FROM MemberSubscriptions s INNER JOIN MembershipPackages pkg ON pkg.Id = s.PackageId;
                UPDATE MemberSubscriptions SET StartDate = NULL, EndDate = NULL WHERE Status = N'Pending';
                """);

            migrationBuilder.CreateIndex(
                name: "IX_Payments_PendingVnPay",
                table: "Payments",
                column: "SubscriptionId",
                unique: true,
                filter: "[SubscriptionId] IS NOT NULL AND [Status] = N'Pending' AND [PaymentMethod] = N'VNPay'");

            migrationBuilder.CreateIndex(
                name: "IX_Payments_SubscriptionId",
                table: "Payments",
                column: "SubscriptionId",
                unique: true,
                filter: "[SubscriptionId] IS NOT NULL AND [Status] = N'Completed'");

            migrationBuilder.CreateIndex(
                name: "IX_Payments_TransactionReference",
                table: "Payments",
                column: "TransactionReference",
                unique: true,
                filter: "[TransactionReference] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_MemberSubscriptions_MemberId_PackageId_Status",
                table: "MemberSubscriptions",
                columns: new[] { "MemberId", "PackageId", "Status" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Payments_PendingVnPay",
                table: "Payments");

            migrationBuilder.DropIndex(
                name: "IX_Payments_SubscriptionId",
                table: "Payments");

            migrationBuilder.DropIndex(
                name: "IX_Payments_TransactionReference",
                table: "Payments");

            migrationBuilder.DropIndex(
                name: "IX_MemberSubscriptions_MemberId_PackageId_Status",
                table: "MemberSubscriptions");

            migrationBuilder.DropColumn(
                name: "CheckoutUrl",
                table: "Payments");

            migrationBuilder.DropColumn(
                name: "ExpiresAt",
                table: "Payments");

            migrationBuilder.DropColumn(
                name: "GatewayResponseCode",
                table: "Payments");

            migrationBuilder.DropColumn(
                name: "GatewayTransactionNo",
                table: "Payments");

            migrationBuilder.DropColumn(
                name: "AgreedPrice",
                table: "MemberSubscriptions");

            migrationBuilder.DropColumn(
                name: "CreatedAt",
                table: "MemberSubscriptions");

            migrationBuilder.DropColumn(
                name: "DurationInDays",
                table: "MemberSubscriptions");

            migrationBuilder.DropColumn(
                name: "RowVersion",
                table: "MemberSubscriptions");

            migrationBuilder.DropColumn(
                name: "FitnessGoal",
                table: "Members");

            migrationBuilder.DropColumn(
                name: "Gender",
                table: "Members");

            migrationBuilder.AlterColumn<string>(
                name: "TransactionReference",
                table: "Payments",
                type: "nvarchar(max)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(100)",
                oldMaxLength: 100,
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "Status",
                table: "MemberSubscriptions",
                type: "nvarchar(max)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(20)",
                oldMaxLength: 20);

            migrationBuilder.AlterColumn<DateTime>(
                name: "StartDate",
                table: "MemberSubscriptions",
                type: "datetime2",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified),
                oldClrType: typeof(DateTime),
                oldType: "datetime2",
                oldNullable: true);

            migrationBuilder.AlterColumn<DateTime>(
                name: "EndDate",
                table: "MemberSubscriptions",
                type: "datetime2",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified),
                oldClrType: typeof(DateTime),
                oldType: "datetime2",
                oldNullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Payments_SubscriptionId",
                table: "Payments",
                column: "SubscriptionId");

            migrationBuilder.CreateIndex(
                name: "IX_MemberSubscriptions_MemberId",
                table: "MemberSubscriptions",
                column: "MemberId");
        }
    }
}
