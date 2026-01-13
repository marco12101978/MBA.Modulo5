using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Pagamentos.Infrastructure.Migrations.Sqlite
{
    /// <inheritdoc />
    public partial class Seguranca : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CvvCartao",
                table: "Pagamentos");

            migrationBuilder.DropColumn(
                name: "NumeroCartao",
                table: "Pagamentos");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "CvvCartao",
                table: "Pagamentos",
                type: "varchar(4)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "NumeroCartao",
                table: "Pagamentos",
                type: "varchar(16)",
                nullable: false,
                defaultValue: "");
        }
    }
}
