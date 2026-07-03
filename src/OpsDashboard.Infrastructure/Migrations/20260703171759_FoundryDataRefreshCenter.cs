using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace OpsDashboard.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class FoundryDataRefreshCenter : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "DataRefreshJobs",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    DatasetId = table.Column<int>(type: "int", nullable: false),
                    StartedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CompletedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    Status = table.Column<string>(type: "nvarchar(24)", maxLength: 24, nullable: false),
                    RecordsProcessed = table.Column<int>(type: "int", nullable: false),
                    TriggeredByUserId = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: true),
                    TriggeredByEmail = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    Message = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DataRefreshJobs", x => x.Id);
                    table.ForeignKey(
                        name: "FK_DataRefreshJobs_Datasets_DatasetId",
                        column: x => x.DatasetId,
                        principalTable: "Datasets",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_DataRefreshJobs_DatasetId_StartedAtUtc",
                table: "DataRefreshJobs",
                columns: new[] { "DatasetId", "StartedAtUtc" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "DataRefreshJobs");
        }
    }
}
