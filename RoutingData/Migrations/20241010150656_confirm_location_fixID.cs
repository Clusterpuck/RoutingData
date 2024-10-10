using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace RoutingData.Migrations
{
    public partial class confirm_location_fixID : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Drop the primary key constraint that depends on the 'Id' column
            migrationBuilder.DropPrimaryKey(
                name: "PK_Locations",
                table: "Locations");

            // Drop the 'Id' column
            migrationBuilder.DropColumn(
                name: "Id",
                table: "Locations");

            // Re-add the 'Id' column with IDENTITY specification
            migrationBuilder.AddColumn<int>(
                name: "Id",
                table: "Locations",
                type: "int",
                nullable: false,
                defaultValue: 0)
                .Annotation("SqlServer:Identity", "1, 1");

            // Recreate the primary key on the new 'Id' column
            migrationBuilder.AddPrimaryKey(
                name: "PK_Locations",
                table: "Locations",
                column: "Id");

            // Create unique index for Longitude, Latitude, and CustomerName
            migrationBuilder.CreateIndex(
                name: "IX_Locations_Longitude_Latitude_CustomerName",
                table: "Locations",
                columns: new[] { "Longitude", "Latitude", "CustomerName" },
                unique: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Drop primary key on Id
            migrationBuilder.DropPrimaryKey(
                name: "PK_Locations",
                table: "Locations");

            // Drop the unique index on Longitude, Latitude, and CustomerName
            migrationBuilder.DropIndex(
                name: "IX_Locations_Longitude_Latitude_CustomerName",
                table: "Locations");

            // Drop the 'Id' column
            migrationBuilder.DropColumn(
                name: "Id",
                table: "Locations");

            // Recreate the 'Id' column without IDENTITY (for down migration purposes)
            migrationBuilder.AddColumn<int>(
                name: "Id",
                table: "Locations",
                type: "int",
                nullable: false,
                defaultValue: 0);

            // Re-add the previous composite primary key (Longitude, Latitude, CustomerName)
            migrationBuilder.AddPrimaryKey(
                name: "PK_Locations",
                table: "Locations",
                columns: new[] { "Longitude", "Latitude", "CustomerName" });
        }
    }
}
