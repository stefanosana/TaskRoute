using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TaskRoute.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddingTime : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "Description",
                table: "Commissions",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(500)",
                oldMaxLength: 500);

            migrationBuilder.AddColumn<int>(
                name: "EstimatedDurationMinutes",
                table: "Commissions",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<TimeSpan>(
                name: "SpecificTime",
                table: "Commissions",
                type: "time",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "EstimatedDurationMinutes",
                table: "Commissions");

            migrationBuilder.DropColumn(
                name: "SpecificTime",
                table: "Commissions");

            migrationBuilder.AlterColumn<string>(
                name: "Description",
                table: "Commissions",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "nvarchar(500)",
                oldMaxLength: 500,
                oldNullable: true);
        }
    }
}
