using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace OpsDashboard.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class FoundryBusinessIntelligenceEngine : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "DatasetInsights",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    DatasetId = table.Column<int>(type: "int", nullable: false),
                    BusinessDomain = table.Column<string>(type: "nvarchar(80)", maxLength: 80, nullable: false),
                    DetectedMeasuresJson = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    DetectedDimensionsJson = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    RecommendedKpisJson = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    RecommendedChartsJson = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    RecommendedDashboardLayoutJson = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    GeneratedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DatasetInsights", x => x.Id);
                    table.ForeignKey(
                        name: "FK_DatasetInsights_Datasets_DatasetId",
                        column: x => x.DatasetId,
                        principalTable: "Datasets",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_DatasetInsights_DatasetId",
                table: "DatasetInsights",
                column: "DatasetId",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "DatasetInsights");
        }
    }
}
