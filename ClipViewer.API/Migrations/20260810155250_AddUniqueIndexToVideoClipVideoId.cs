using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ClipViewer.API.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddUniqueIndexToVideoClipVideoId : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_VideoClips_VideoId",
                table: "VideoClips");

            migrationBuilder.CreateIndex(
                name: "IX_VideoClips_VideoId",
                table: "VideoClips",
                column: "VideoId",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_VideoClips_VideoId",
                table: "VideoClips");

            migrationBuilder.CreateIndex(
                name: "IX_VideoClips_VideoId",
                table: "VideoClips",
                column: "VideoId");
        }
    }
}
