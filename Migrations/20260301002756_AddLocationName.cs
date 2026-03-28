using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SafeRoute.Migrations
{
    /// <inheritdoc />
    public partial class AddLocationName : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "LocationName",
                table: "IncidentReports",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "LocationName",
                table: "IncidentReports");
        }
    }
}

