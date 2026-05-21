using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MediaCatalog.Migrations
{
    /// <inheritdoc />
    public partial class AddTvShowSeasonOrderOverride : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "SeasonOrderOverride",
                table: "TvShows",
                type: "INTEGER",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "SeasonOrderOverride",
                table: "TvShows");
        }
    }
}
