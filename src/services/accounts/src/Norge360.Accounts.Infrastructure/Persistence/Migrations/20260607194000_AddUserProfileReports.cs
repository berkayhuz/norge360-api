// <copyright file="20260607194000_AddUserProfileReports.cs" company="Norge360">
// Copyright (c) 2026 Norge360. All rights reserved.
// Norge360 is proprietary software. See the LICENSE file in the repository root.
// </copyright>

using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Norge360.Accounts.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddUserProfileReports : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "UserProfileReports",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ReporterProfileId = table.Column<Guid>(type: "uuid", nullable: false),
                    ReportedProfileId = table.Column<Guid>(type: "uuid", nullable: false),
                    ReporterAuthUserId = table.Column<Guid>(type: "uuid", nullable: false),
                    ReportedAuthUserId = table.Column<Guid>(type: "uuid", nullable: false),
                    ReasonCode = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    Description = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserProfileReports", x => x.Id);
                    table.CheckConstraint("CK_UserProfileReports_ReporterProfileId_NotEqual_ReportedProfileId", "\"ReporterProfileId\" <> \"ReportedProfileId\"");
                    table.ForeignKey(
                        name: "FK_UserProfileReports_UserProfiles_ReportedProfileId",
                        column: x => x.ReportedProfileId,
                        principalTable: "UserProfiles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_UserProfileReports_UserProfiles_ReporterProfileId",
                        column: x => x.ReporterProfileId,
                        principalTable: "UserProfiles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_UserProfileReports_ReasonCode",
                table: "UserProfileReports",
                column: "ReasonCode");

            migrationBuilder.CreateIndex(
                name: "IX_UserProfileReports_ReportedProfileId_CreatedAt",
                table: "UserProfileReports",
                columns: new[] { "ReportedProfileId", "CreatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_UserProfileReports_ReporterProfileId_CreatedAt",
                table: "UserProfileReports",
                columns: new[] { "ReporterProfileId", "CreatedAt" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "UserProfileReports");
        }
    }
}
