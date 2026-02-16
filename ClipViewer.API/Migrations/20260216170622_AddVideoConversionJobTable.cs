using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace ClipViewer.API.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddVideoConversionJobTable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "VideoConversionJobs",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    VideoClipId = table.Column<Guid>(type: "uuid", nullable: false),
                    VideoId = table.Column<string>(type: "text", nullable: true),
                    InputPath = table.Column<string>(type: "text", nullable: false),
                    OutputDirectory = table.Column<string>(type: "text", nullable: false),
                    JobId = table.Column<Guid>(type: "uuid", nullable: false),
                    AuthorId = table.Column<int>(type: "integer", nullable: false),
                    Name = table.Column<string>(type: "text", nullable: true),
                    StartTime = table.Column<int>(type: "integer", nullable: false),
                    EndTime = table.Column<int>(type: "integer", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    StartedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CompletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    Status = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_VideoConversionJobs", x => x.Id);
                    table.ForeignKey(
                        name: "FK_VideoConversionJobs_VideoClips_VideoClipId",
                        column: x => x.VideoClipId,
                        principalTable: "VideoClips",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_VideoConversionJobs_JobId",
                table: "VideoConversionJobs",
                column: "JobId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_VideoConversionJobs_Status",
                table: "VideoConversionJobs",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_VideoConversionJobs_VideoClipId",
                table: "VideoConversionJobs",
                column: "VideoClipId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "VideoConversionJobs");
        }
    }
}
