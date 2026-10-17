using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Content.Server.Database.Migrations.Sqlite
{
    /// <inheritdoc />
    public partial class Moff_MultiCharacter : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "moff_preference",
                columns: table => new
                {
                    moff_preference_id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    preference_id = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_moff_preference", x => x.moff_preference_id);
                    table.ForeignKey(
                        name: "FK_moff_preference_preference_preference_id",
                        column: x => x.preference_id,
                        principalTable: "preference",
                        principalColumn: "preference_id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "moff_profile",
                columns: table => new
                {
                    moff_profile_id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    profile_id = table.Column<int>(type: "INTEGER", nullable: false),
                    enabled = table.Column<bool>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_moff_profile", x => x.moff_profile_id);
                    table.ForeignKey(
                        name: "FK_moff_profile_profile_profile_id",
                        column: x => x.profile_id,
                        principalTable: "profile",
                        principalColumn: "profile_id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "moff_job_priority",
                columns: table => new
                {
                    moff_job_priority_id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    moff_preference_id = table.Column<int>(type: "INTEGER", nullable: false),
                    job_name = table.Column<string>(type: "TEXT", nullable: false),
                    priority = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_moff_job_priority", x => x.moff_job_priority_id);
                    table.ForeignKey(
                        name: "FK_moff_job_priority_moff_preference_moff_preference_id",
                        column: x => x.moff_preference_id,
                        principalTable: "moff_preference",
                        principalColumn: "moff_preference_id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_moff_job_priority_moff_preference_id_job_name",
                table: "moff_job_priority",
                columns: new[] { "moff_preference_id", "job_name" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_moff_preference_preference_id",
                table: "moff_preference",
                column: "preference_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_moff_profile_profile_id",
                table: "moff_profile",
                column: "profile_id",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "moff_job_priority");

            migrationBuilder.DropTable(
                name: "moff_profile");

            migrationBuilder.DropTable(
                name: "moff_preference");
        }
    }
}
