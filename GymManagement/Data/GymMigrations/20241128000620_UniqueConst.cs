using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GymManagement.Data.GymMigrations
{
    /// <inheritdoc />
    public partial class UniqueConst : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "IX_FitnessCategories_Category",
                table: "FitnessCategories",
                column: "Category",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Exercises_Name",
                table: "Exercises",
                column: "Name",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_FitnessCategories_Category",
                table: "FitnessCategories");

            migrationBuilder.DropIndex(
                name: "IX_Exercises_Name",
                table: "Exercises");
        }
    }
}
