using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace VehicleIntelligence.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Vehicles",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    VehicleExternalId = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    LastSeenAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    Status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Vehicles", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "TelemetryRecords",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    VehicleId = table.Column<Guid>(type: "uuid", nullable: false),
                    TripId = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    Timestamp = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    Speed = table.Column<double>(type: "double precision", nullable: true),
                    Latitude = table.Column<double>(type: "double precision", nullable: true),
                    Longitude = table.Column<double>(type: "double precision", nullable: true),
                    Distance = table.Column<double>(type: "double precision", nullable: true),
                    BatteryLevel = table.Column<double>(type: "double precision", nullable: true),
                    BatteryVoltage = table.Column<double>(type: "double precision", nullable: true),
                    BatteryCurrent = table.Column<double>(type: "double precision", nullable: true),
                    EngineRpm = table.Column<double>(type: "double precision", nullable: true),
                    EngineLoad = table.Column<double>(type: "double precision", nullable: true),
                    FuelRate = table.Column<double>(type: "double precision", nullable: true),
                    EnergyConsumption = table.Column<double>(type: "double precision", nullable: true),
                    Temperature = table.Column<double>(type: "double precision", nullable: true),
                    RawPayloadJson = table.Column<string>(type: "text", nullable: true),
                    RiskScore = table.Column<double>(type: "double precision", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TelemetryRecords", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TelemetryRecords_Vehicles_VehicleId",
                        column: x => x.VehicleId,
                        principalTable: "Vehicles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Alerts",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    VehicleId = table.Column<Guid>(type: "uuid", nullable: false),
                    TelemetryRecordId = table.Column<Guid>(type: "uuid", nullable: true),
                    AlertType = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    Severity = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    Message = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    RiskScore = table.Column<double>(type: "double precision", nullable: false),
                    IsResolved = table.Column<bool>(type: "boolean", nullable: false),
                    ResolvedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Alerts", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Alerts_TelemetryRecords_TelemetryRecordId",
                        column: x => x.TelemetryRecordId,
                        principalTable: "TelemetryRecords",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_Alerts_Vehicles_VehicleId",
                        column: x => x.VehicleId,
                        principalTable: "Vehicles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Alerts_CreatedAt",
                table: "Alerts",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_Alerts_IsResolved",
                table: "Alerts",
                column: "IsResolved");

            migrationBuilder.CreateIndex(
                name: "IX_Alerts_Severity",
                table: "Alerts",
                column: "Severity");

            migrationBuilder.CreateIndex(
                name: "IX_Alerts_TelemetryRecordId",
                table: "Alerts",
                column: "TelemetryRecordId");

            migrationBuilder.CreateIndex(
                name: "IX_Alerts_VehicleId",
                table: "Alerts",
                column: "VehicleId");

            migrationBuilder.CreateIndex(
                name: "IX_TelemetryRecords_Timestamp",
                table: "TelemetryRecords",
                column: "Timestamp");

            migrationBuilder.CreateIndex(
                name: "IX_TelemetryRecords_VehicleId_Timestamp",
                table: "TelemetryRecords",
                columns: new[] { "VehicleId", "Timestamp" });

            migrationBuilder.CreateIndex(
                name: "IX_Vehicles_ExternalId",
                table: "Vehicles",
                column: "VehicleExternalId",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Alerts");

            migrationBuilder.DropTable(
                name: "TelemetryRecords");

            migrationBuilder.DropTable(
                name: "Vehicles");
        }
    }
}
