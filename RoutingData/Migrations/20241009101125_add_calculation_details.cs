using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace RoutingData.Migrations
{
    /// <inheritdoc />
    public partial class add_calculation_details : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "ErrorMessage",
                table: "Calculations",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<int>(
                name: "MaxVehicles",
                table: "Calculations",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "NumOfOrders",
                table: "Calculations",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<bool>(
                name: "UsedMapBox",
                table: "Calculations",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "UsedQuantum",
                table: "Calculations",
                type: "bit",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ErrorMessage",
                table: "Calculations");

            migrationBuilder.DropColumn(
                name: "MaxVehicles",
                table: "Calculations");

            migrationBuilder.DropColumn(
                name: "NumOfOrders",
                table: "Calculations");

            migrationBuilder.DropColumn(
                name: "UsedMapBox",
                table: "Calculations");

            migrationBuilder.DropColumn(
                name: "UsedQuantum",
                table: "Calculations");
        }
    }
}
