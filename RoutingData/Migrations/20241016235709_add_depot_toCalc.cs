using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace RoutingData.Migrations
{
    /// <inheritdoc />
    public partial class add_depot_toCalc : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "DepotID",
                table: "Calculations",
                type: "int",
                nullable: false,
                defaultValue: 0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "DepotID",
                table: "Calculations");
        }
    }
}
