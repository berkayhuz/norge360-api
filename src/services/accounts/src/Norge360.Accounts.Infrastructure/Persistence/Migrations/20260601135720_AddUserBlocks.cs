// <copyright file="20260601135720_AddUserBlocks.cs" company="Norge360">`r`n// Copyright (c) 2026 Norge360. All rights reserved.`r`n// Norge360 is proprietary software. See the LICENSE file in the repository root.`r`n// </copyright>`r`n`r`nusing System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Norge360.Accounts.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddUserBlocks : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "UserBlocks",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    BlockerProfileId = table.Column<Guid>(type: "uuid", nullable: false),
                    BlockedProfileId = table.Column<Guid>(type: "uuid", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserBlocks", x => x.Id);
                    table.CheckConstraint("CK_UserBlocks_BlockerProfileId_NotEqual_BlockedProfileId", "\"BlockerProfileId\" <> \"BlockedProfileId\"");
                    table.ForeignKey(
                        name: "FK_UserBlocks_UserProfiles_BlockedProfileId",
                        column: x => x.BlockedProfileId,
                        principalTable: "UserProfiles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_UserBlocks_UserProfiles_BlockerProfileId",
                        column: x => x.BlockerProfileId,
                        principalTable: "UserProfiles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_UserBlocks_BlockedProfileId",
                table: "UserBlocks",
                column: "BlockedProfileId");

            migrationBuilder.CreateIndex(
                name: "IX_UserBlocks_BlockerProfileId",
                table: "UserBlocks",
                column: "BlockerProfileId");

            migrationBuilder.CreateIndex(
                name: "IX_UserBlocks_BlockerProfileId_BlockedProfileId",
                table: "UserBlocks",
                columns: new[] { "BlockerProfileId", "BlockedProfileId" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "UserBlocks");
        }
    }
}

