using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Splittat.API.Migrations
{
    /// <inheritdoc />
    public partial class AddAsyncReceiptProcessing : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "ErrorMessage",
                table: "Receipts",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<double>(
                name: "OcrConfidence",
                table: "Receipts",
                type: "double precision",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "ProcessedAt",
                table: "Receipts",
                type: "timestamp with time zone",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ErrorMessage",
                table: "Receipts");

            migrationBuilder.DropColumn(
                name: "OcrConfidence",
                table: "Receipts");

            migrationBuilder.DropColumn(
                name: "ProcessedAt",
                table: "Receipts");
        }
    }
}
