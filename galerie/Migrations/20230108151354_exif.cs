using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace galerie.Migrations
{
    public partial class exif : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "ExifDateTaken",
                table: "Files",
                type: "datetime2",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ExifDateTaken",
                table: "Files");
        }
    }
}
