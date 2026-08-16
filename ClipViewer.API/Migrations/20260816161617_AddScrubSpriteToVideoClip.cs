using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ClipViewer.API.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddScrubSpriteToVideoClip : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "ScrubSprite",
                table: "VideoClips",
                type: "text",
                nullable: false,
                defaultValue: "");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ScrubSprite",
                table: "VideoClips");
        }
    }
}
