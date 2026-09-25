using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SportsCenterAPI.Migrations
{
    /// <inheritdoc />
    public partial class Flow2SessionsAndRemoveGateway : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Preserve historical transactions. An unresolved gateway checkout is not proof
            // of failure and must not silently become eligible for a second cash payment.
            migrationBuilder.Sql("UPDATE Payments SET Status = N'NeedsReview' WHERE PaymentMethod = N'VNPay' AND Status = N'Pending';");
            migrationBuilder.DropIndex(
                name: "IX_Payments_PendingVnPay",
                table: "Payments");

            migrationBuilder.DropIndex(
                name: "IX_ClassRegistrations_MemberId_ClassId",
                table: "ClassRegistrations");

            migrationBuilder.DropIndex(
                name: "IX_Attendances_MemberId",
                table: "Attendances");

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

            migrationBuilder.AlterColumn<string>(
                name: "Status",
                table: "ClassRegistrations",
                type: "nvarchar(20)",
                maxLength: 20,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)");

            migrationBuilder.AddColumn<DateTime>(
                name: "CancellationDeadline",
                table: "ClassRegistrations",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CancellationReason",
                table: "ClassRegistrations",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "CancelledAt",
                table: "ClassRegistrations",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "CancelledByUserId",
                table: "ClassRegistrations",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "CreatedByUserId",
                table: "ClassRegistrations",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<byte[]>(
                name: "RowVersion",
                table: "ClassRegistrations",
                type: "rowversion",
                rowVersion: true,
                nullable: false,
                defaultValue: new byte[0]);

            migrationBuilder.AddColumn<int>(
                name: "SessionId",
                table: "ClassRegistrations",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "SessionId",
                table: "Attendances",
                type: "int",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "CancellationPolicies",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: false),
                    MinimumHoursBeforeStart = table.Column<int>(type: "int", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CancellationPolicies", x => x.Id);
                    table.CheckConstraint("CK_CancellationPolicies_Hours", "[MinimumHoursBeforeStart] BETWEEN 2 AND 720");
                });

            migrationBuilder.CreateTable(
                name: "ClassReviews",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    RegistrationId = table.Column<int>(type: "int", nullable: false),
                    Rating = table.Column<int>(type: "int", nullable: false),
                    Comment = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ClassReviews", x => x.Id);
                    table.CheckConstraint("CK_ClassReviews_Rating", "[Rating] BETWEEN 1 AND 5");
                    table.ForeignKey(
                        name: "FK_ClassReviews_ClassRegistrations_RegistrationId",
                        column: x => x.RegistrationId,
                        principalTable: "ClassRegistrations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "ClassSessions",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ClassId = table.Column<int>(type: "int", nullable: false),
                    CoachId = table.Column<int>(type: "int", nullable: false),
                    CancellationPolicyId = table.Column<int>(type: "int", nullable: false),
                    StartsAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    EndsAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Capacity = table.Column<int>(type: "int", nullable: false),
                    Status = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    CancellationReason = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ClassSessions", x => x.Id);
                    table.UniqueConstraint("AK_ClassSessions_Id_ClassId", x => new { x.Id, x.ClassId });
                    table.CheckConstraint("CK_ClassSessions_Capacity", "[Capacity] > 0");
                    table.CheckConstraint("CK_ClassSessions_Time", "[EndsAt] > [StartsAt]");
                    table.ForeignKey(
                        name: "FK_ClassSessions_CancellationPolicies_CancellationPolicyId",
                        column: x => x.CancellationPolicyId,
                        principalTable: "CancellationPolicies",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ClassSessions_Coaches_CoachId",
                        column: x => x.CoachId,
                        principalTable: "Coaches",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ClassSessions_SportClasses_ClassId",
                        column: x => x.ClassId,
                        principalTable: "SportClasses",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.InsertData(
                table: "CancellationPolicies",
                columns: new[] { "Id", "IsActive", "MinimumHoursBeforeStart", "Name" },
                values: new object[] { 1, true, 2, "Hủy trước ít nhất 2 tiếng" });

            migrationBuilder.CreateIndex(
                name: "IX_ClassRegistrations_CancelledByUserId",
                table: "ClassRegistrations",
                column: "CancelledByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_ClassRegistrations_CreatedByUserId",
                table: "ClassRegistrations",
                column: "CreatedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_ClassRegistrations_MemberId_ClassId",
                table: "ClassRegistrations",
                columns: new[] { "MemberId", "ClassId" },
                unique: true,
                filter: "[SessionId] IS NULL");

            migrationBuilder.CreateIndex(
                name: "IX_ClassRegistrations_MemberId_SessionId",
                table: "ClassRegistrations",
                columns: new[] { "MemberId", "SessionId" },
                unique: true,
                filter: "[SessionId] IS NOT NULL AND [Status] <> N'Cancelled'");

            migrationBuilder.CreateIndex(
                name: "IX_ClassRegistrations_SessionId_ClassId",
                table: "ClassRegistrations",
                columns: new[] { "SessionId", "ClassId" });

            migrationBuilder.CreateIndex(
                name: "IX_Attendances_MemberId_SessionId",
                table: "Attendances",
                columns: new[] { "MemberId", "SessionId" },
                unique: true,
                filter: "[SessionId] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_Attendances_SessionId_ClassId",
                table: "Attendances",
                columns: new[] { "SessionId", "ClassId" });

            migrationBuilder.CreateIndex(
                name: "IX_ClassReviews_RegistrationId",
                table: "ClassReviews",
                column: "RegistrationId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ClassSessions_CancellationPolicyId",
                table: "ClassSessions",
                column: "CancellationPolicyId");

            migrationBuilder.CreateIndex(
                name: "IX_ClassSessions_ClassId",
                table: "ClassSessions",
                column: "ClassId");

            migrationBuilder.CreateIndex(
                name: "IX_ClassSessions_CoachId_StartsAt_EndsAt",
                table: "ClassSessions",
                columns: new[] { "CoachId", "StartsAt", "EndsAt" });

            migrationBuilder.AddForeignKey(
                name: "FK_Attendances_ClassSessions_SessionId_ClassId",
                table: "Attendances",
                columns: new[] { "SessionId", "ClassId" },
                principalTable: "ClassSessions",
                principalColumns: new[] { "Id", "ClassId" },
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_ClassRegistrations_ClassSessions_SessionId_ClassId",
                table: "ClassRegistrations",
                columns: new[] { "SessionId", "ClassId" },
                principalTable: "ClassSessions",
                principalColumns: new[] { "Id", "ClassId" },
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_ClassRegistrations_Users_CancelledByUserId",
                table: "ClassRegistrations",
                column: "CancelledByUserId",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_ClassRegistrations_Users_CreatedByUserId",
                table: "ClassRegistrations",
                column: "CreatedByUserId",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Attendances_ClassSessions_SessionId_ClassId",
                table: "Attendances");

            migrationBuilder.DropForeignKey(
                name: "FK_ClassRegistrations_ClassSessions_SessionId_ClassId",
                table: "ClassRegistrations");

            migrationBuilder.DropForeignKey(
                name: "FK_ClassRegistrations_Users_CancelledByUserId",
                table: "ClassRegistrations");

            migrationBuilder.DropForeignKey(
                name: "FK_ClassRegistrations_Users_CreatedByUserId",
                table: "ClassRegistrations");

            migrationBuilder.DropTable(
                name: "ClassReviews");

            migrationBuilder.DropTable(
                name: "ClassSessions");

            migrationBuilder.DropTable(
                name: "CancellationPolicies");

            migrationBuilder.DropIndex(
                name: "IX_ClassRegistrations_CancelledByUserId",
                table: "ClassRegistrations");

            migrationBuilder.DropIndex(
                name: "IX_ClassRegistrations_CreatedByUserId",
                table: "ClassRegistrations");

            migrationBuilder.DropIndex(
                name: "IX_ClassRegistrations_MemberId_ClassId",
                table: "ClassRegistrations");

            migrationBuilder.DropIndex(
                name: "IX_ClassRegistrations_MemberId_SessionId",
                table: "ClassRegistrations");

            migrationBuilder.DropIndex(
                name: "IX_ClassRegistrations_SessionId_ClassId",
                table: "ClassRegistrations");

            migrationBuilder.DropIndex(
                name: "IX_Attendances_MemberId_SessionId",
                table: "Attendances");

            migrationBuilder.DropIndex(
                name: "IX_Attendances_SessionId_ClassId",
                table: "Attendances");

            migrationBuilder.DropColumn(
                name: "CancellationDeadline",
                table: "ClassRegistrations");

            migrationBuilder.DropColumn(
                name: "CancellationReason",
                table: "ClassRegistrations");

            migrationBuilder.DropColumn(
                name: "CancelledAt",
                table: "ClassRegistrations");

            migrationBuilder.DropColumn(
                name: "CancelledByUserId",
                table: "ClassRegistrations");

            migrationBuilder.DropColumn(
                name: "CreatedByUserId",
                table: "ClassRegistrations");

            migrationBuilder.DropColumn(
                name: "RowVersion",
                table: "ClassRegistrations");

            migrationBuilder.DropColumn(
                name: "SessionId",
                table: "ClassRegistrations");

            migrationBuilder.DropColumn(
                name: "SessionId",
                table: "Attendances");

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
                table: "ClassRegistrations",
                type: "nvarchar(max)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(20)",
                oldMaxLength: 20);

            migrationBuilder.CreateIndex(
                name: "IX_Payments_PendingVnPay",
                table: "Payments",
                column: "SubscriptionId",
                unique: true,
                filter: "[SubscriptionId] IS NOT NULL AND [Status] = N'Pending' AND [PaymentMethod] = N'VNPay'");

            migrationBuilder.CreateIndex(
                name: "IX_ClassRegistrations_MemberId_ClassId",
                table: "ClassRegistrations",
                columns: new[] { "MemberId", "ClassId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Attendances_MemberId",
                table: "Attendances",
                column: "MemberId");
        }
    }
}
