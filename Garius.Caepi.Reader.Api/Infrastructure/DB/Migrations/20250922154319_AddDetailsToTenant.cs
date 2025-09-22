using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Garius.Caepi.Reader.Api.Infrastructure.DB.Migrations
{
    /// <inheritdoc />
    public partial class AddDetailsToTenant : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Name",
                table: "AspNetTenants");

            migrationBuilder.AddColumn<string>(
                name: "Address",
                table: "AspNetTenants",
                type: "character varying(255)",
                maxLength: 255,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CNPJ",
                table: "AspNetTenants",
                type: "character varying(18)",
                maxLength: 18,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "Email",
                table: "AspNetTenants",
                type: "character varying(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ImageUrl",
                table: "AspNetTenants",
                type: "character varying(255)",
                maxLength: 255,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "LegalName",
                table: "AspNetTenants",
                type: "character varying(255)",
                maxLength: 255,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "MunicipalRegistration",
                table: "AspNetTenants",
                type: "character varying(50)",
                maxLength: 50,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Phone",
                table: "AspNetTenants",
                type: "character varying(50)",
                maxLength: 50,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "StateRegistration",
                table: "AspNetTenants",
                type: "character varying(50)",
                maxLength: 50,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "TradeName",
                table: "AspNetTenants",
                type: "character varying(255)",
                maxLength: 255,
                nullable: false,
                defaultValue: "");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Address",
                table: "AspNetTenants");

            migrationBuilder.DropColumn(
                name: "CNPJ",
                table: "AspNetTenants");

            migrationBuilder.DropColumn(
                name: "Email",
                table: "AspNetTenants");

            migrationBuilder.DropColumn(
                name: "ImageUrl",
                table: "AspNetTenants");

            migrationBuilder.DropColumn(
                name: "LegalName",
                table: "AspNetTenants");

            migrationBuilder.DropColumn(
                name: "MunicipalRegistration",
                table: "AspNetTenants");

            migrationBuilder.DropColumn(
                name: "Phone",
                table: "AspNetTenants");

            migrationBuilder.DropColumn(
                name: "StateRegistration",
                table: "AspNetTenants");

            migrationBuilder.DropColumn(
                name: "TradeName",
                table: "AspNetTenants");

            migrationBuilder.AddColumn<string>(
                name: "Name",
                table: "AspNetTenants",
                type: "text",
                nullable: false,
                defaultValue: "");
        }
    }
}
