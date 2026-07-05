using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace VehicleIntelligence.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddKaggleTelemetryColumns : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<double>(
                name: "AirConditioningPower",
                table: "TelemetryRecords",
                type: "double precision",
                nullable: true);

            migrationBuilder.AddColumn<double>(
                name: "Elevation",
                table: "TelemetryRecords",
                type: "double precision",
                nullable: true);

            migrationBuilder.AddColumn<double>(
                name: "HeaterPower",
                table: "TelemetryRecords",
                type: "double precision",
                nullable: true);

            migrationBuilder.AddColumn<double>(
                name: "MassAirFlow",
                table: "TelemetryRecords",
                type: "double precision",
                nullable: true);

            migrationBuilder.AddColumn<double>(
                name: "SpeedLimit",
                table: "TelemetryRecords",
                type: "double precision",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "AirConditioningPower",
                table: "TelemetryRecords");

            migrationBuilder.DropColumn(
                name: "Elevation",
                table: "TelemetryRecords");

            migrationBuilder.DropColumn(
                name: "HeaterPower",
                table: "TelemetryRecords");

            migrationBuilder.DropColumn(
                name: "MassAirFlow",
                table: "TelemetryRecords");

            migrationBuilder.DropColumn(
                name: "SpeedLimit",
                table: "TelemetryRecords");
        }
    }
}
