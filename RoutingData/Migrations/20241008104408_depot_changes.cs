using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace RoutingData.Migrations
{
    /// <inheritdoc />
    public partial class depot_changes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "CustomerID",
                table: "Locations",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<bool>(
                name: "IsDepot",
                table: "Locations",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<int>(
                name: "DepotID",
                table: "DeliveryRoutes",
                type: "int",
                nullable: false,
                defaultValue: 0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CustomerID",
                table: "Locations");

            migrationBuilder.DropColumn(
                name: "IsDepot",
                table: "Locations");

            migrationBuilder.DropColumn(
                name: "DepotID",
                table: "DeliveryRoutes");
        }
    }
}
