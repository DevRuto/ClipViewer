using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ClipViewer.API.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddCompositeIndexToVideoConversionJob : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_VideoConversionJobs_Status",
                table: "VideoConversionJobs");

            migrationBuilder.CreateIndex(
                name: "IX_VideoConversionJobs_Status_CreatedAt",
                table: "VideoConversionJobs",
                columns: new[] { "Status", "CreatedAt" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_VideoConversionJobs_Status_CreatedAt",
                table: "VideoConversionJobs");

            migrationBuilder.CreateIndex(
                name: "IX_VideoConversionJobs_Status",
                table: "VideoConversionJobs",
                column: "Status");
        }
    }
}
