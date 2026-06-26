using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AppaRently.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddApartmentIdToNotifications : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "ApartmentId",
                table: "Notifications",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Notifications_ApartmentId",
                table: "Notifications",
                column: "ApartmentId");

            migrationBuilder.AddForeignKey(
                name: "FK_Notifications_Apartments_ApartmentId",
                table: "Notifications",
                column: "ApartmentId",
                principalTable: "Apartments",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Notifications_Apartments_ApartmentId",
                table: "Notifications");

            migrationBuilder.DropIndex(
                name: "IX_Notifications_ApartmentId",
                table: "Notifications");

            migrationBuilder.DropColumn(
                name: "ApartmentId",
                table: "Notifications");
        }
    }
}
