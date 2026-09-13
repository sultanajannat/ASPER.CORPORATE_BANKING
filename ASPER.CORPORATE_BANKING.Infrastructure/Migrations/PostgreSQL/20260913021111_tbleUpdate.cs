using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ASPER.CORPORATE_BANKING.Infrastructure.Migrations.PostgreSQL
{
    /// <inheritdoc />
    public partial class tbleUpdate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "actionByRoleId",
                schema: "crbanking",
                table: "TransactionApprovalAction",
                type: "text",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "integer",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "roleId",
                schema: "crbanking",
                table: "ApprovalStepRole",
                type: "text",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "integer",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "checkerRoleId",
                schema: "crbanking",
                table: "ApprovalMatrixSlab",
                type: "text",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "integer",
                oldNullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<int>(
                name: "actionByRoleId",
                schema: "crbanking",
                table: "TransactionApprovalAction",
                type: "integer",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "text",
                oldNullable: true);

            migrationBuilder.AlterColumn<int>(
                name: "roleId",
                schema: "crbanking",
                table: "ApprovalStepRole",
                type: "integer",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "text",
                oldNullable: true);

            migrationBuilder.AlterColumn<int>(
                name: "checkerRoleId",
                schema: "crbanking",
                table: "ApprovalMatrixSlab",
                type: "integer",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "text",
                oldNullable: true);
        }
    }
}
