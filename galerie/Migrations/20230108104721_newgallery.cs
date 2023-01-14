using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace galerie.Migrations
{
    public partial class newgallery : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "GalleryBackgroundColor",
                table: "Galleries",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "GalleryBackgroundColor",
                table: "Galleries");
        }
    }
}
