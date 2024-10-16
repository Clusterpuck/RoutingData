using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace RoutingData.Migrations
{
    /// <inheritdoc />
    public partial class python_details_onCalcLog : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "CalculationNumber",
                table: "Calculations",
                type: "int",
                nullable: false,
                defaultValue: 0)
                .Annotation("SqlServer:Identity", "1, 1");

            migrationBuilder.AddColumn<string>(
                name: "PythonPayload",
                table: "Calculations",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CalculationNumber",
                table: "Calculations");

            migrationBuilder.DropColumn(
                name: "PythonPayload",
                table: "Calculations");
        }
    }
}
