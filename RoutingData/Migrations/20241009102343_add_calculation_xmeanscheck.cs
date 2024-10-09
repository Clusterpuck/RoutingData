using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace RoutingData.Migrations
{
    /// <inheritdoc />
    public partial class add_calculation_xmeanscheck : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "UsedXMeans",
                table: "Calculations",
                type: "bit",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "UsedXMeans",
                table: "Calculations");
        }
    }
}
