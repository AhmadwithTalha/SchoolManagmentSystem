using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SchoolManagementSystem.Migrations
{
    /// <inheritdoc />
    public partial class EncryptPrincipalData : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "UserGuid",
                table: "AspNetUsers",
                newName: "EncryptedPhoneNumber");

            migrationBuilder.RenameColumn(
                name: "LastName",
                table: "AspNetUsers",
                newName: "EncryptedLastName");

            migrationBuilder.RenameColumn(
                name: "FirstName",
                table: "AspNetUsers",
                newName: "EncryptedFirstName");

            migrationBuilder.RenameColumn(
                name: "Country",
                table: "AspNetUsers",
                newName: "EncryptedCountry");

            migrationBuilder.RenameColumn(
                name: "City",
                table: "AspNetUsers",
                newName: "EncryptedCity");

            migrationBuilder.RenameColumn(
                name: "Address",
                table: "AspNetUsers",
                newName: "EncryptedAddress");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "EncryptedPhoneNumber",
                table: "AspNetUsers",
                newName: "UserGuid");

            migrationBuilder.RenameColumn(
                name: "EncryptedLastName",
                table: "AspNetUsers",
                newName: "LastName");

            migrationBuilder.RenameColumn(
                name: "EncryptedFirstName",
                table: "AspNetUsers",
                newName: "FirstName");

            migrationBuilder.RenameColumn(
                name: "EncryptedCountry",
                table: "AspNetUsers",
                newName: "Country");

            migrationBuilder.RenameColumn(
                name: "EncryptedCity",
                table: "AspNetUsers",
                newName: "City");

            migrationBuilder.RenameColumn(
                name: "EncryptedAddress",
                table: "AspNetUsers",
                newName: "Address");
        }
    }
}
