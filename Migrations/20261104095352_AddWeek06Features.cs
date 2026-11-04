using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace QTKD.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddWeek06Features : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "Status",
                table: "Contracts",
                type: "character varying(50)",
                maxLength: 50,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "text");

            migrationBuilder.AddColumn<string>(
                name: "ContractContent",
                table: "Contracts",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CurrentApprovalId",
                table: "Contracts",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "FileUrl",
                table: "Contracts",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "LastIdempotencyKey",
                table: "Contracts",
                type: "text",
                nullable: true);

            migrationBuilder.UpdateData(
                table: "Contracts",
                keyColumn: "Id",
                keyValue: 1,
                columns: new[] { "ContractContent", "CurrentApprovalId", "FileUrl", "LastIdempotencyKey" },
                values: new object[] { null, null, null, null });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ContractContent",
                table: "Contracts");

            migrationBuilder.DropColumn(
                name: "CurrentApprovalId",
                table: "Contracts");

            migrationBuilder.DropColumn(
                name: "FileUrl",
                table: "Contracts");

            migrationBuilder.DropColumn(
                name: "LastIdempotencyKey",
                table: "Contracts");

            migrationBuilder.AlterColumn<string>(
                name: "Status",
                table: "Contracts",
                type: "text",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(50)",
                oldMaxLength: 50);
        }
    }
}
