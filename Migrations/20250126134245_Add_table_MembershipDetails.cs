using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace App.Migrations
{
    /// <inheritdoc />
    public partial class Add_table_MembershipDetails : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "MembershipType",
                table: "Memberships",
                newName: "MembershipDetailsId");

            migrationBuilder.CreateTable(
                name: "MembershipDetails",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    MembershipType = table.Column<int>(type: "int", nullable: false),
                    StorageLimitMB = table.Column<int>(type: "int", nullable: false),
                    HasAds = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    MaxImageCount = table.Column<int>(type: "int", nullable: false),
                    QualityImage = table.Column<int>(type: "int", nullable: false),
                    PrioritySupport = table.Column<int>(type: "int", nullable: false),
                    ImageGeneration = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    ResolutionEnhancement = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    Unblur = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    ObjectRemoval = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    BackgroundBlur = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    ColorEnhancement = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    Denoise = table.Column<bool>(type: "tinyint(1)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MembershipDetails", x => x.Id);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateIndex(
                name: "IX_Memberships_MembershipDetailsId",
                table: "Memberships",
                column: "MembershipDetailsId");

            migrationBuilder.CreateIndex(
                name: "IX_MembershipDetails_MembershipType",
                table: "MembershipDetails",
                column: "MembershipType",
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_Memberships_MembershipDetails_MembershipDetailsId",
                table: "Memberships",
                column: "MembershipDetailsId",
                principalTable: "MembershipDetails",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Memberships_MembershipDetails_MembershipDetailsId",
                table: "Memberships");

            migrationBuilder.DropTable(
                name: "MembershipDetails");

            migrationBuilder.DropIndex(
                name: "IX_Memberships_MembershipDetailsId",
                table: "Memberships");

            migrationBuilder.RenameColumn(
                name: "MembershipDetailsId",
                table: "Memberships",
                newName: "MembershipType");
        }
    }
}
