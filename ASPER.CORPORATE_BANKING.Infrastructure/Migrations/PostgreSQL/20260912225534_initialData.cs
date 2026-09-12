using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace ASPER.CORPORATE_BANKING.Infrastructure.Migrations.PostgreSQL
{
    /// <inheritdoc />
    public partial class initialData : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "crbanking");

            migrationBuilder.CreateTable(
                name: "AuditOutboxMessage",
                schema: "crbanking",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    EventType = table.Column<string>(type: "text", nullable: false),
                    Payload = table.Column<string>(type: "text", nullable: false),
                    Status = table.Column<string>(type: "text", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    ProcessedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    RetryCount = table.Column<int>(type: "integer", nullable: false),
                    Error = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AuditOutboxMessage", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "TransactionType",
                schema: "crbanking",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    code = table.Column<string>(type: "text", nullable: true),
                    name = table.Column<string>(type: "text", nullable: true),
                    description = table.Column<string>(type: "text", nullable: true),
                    isActive = table.Column<bool>(type: "boolean", nullable: true),
                    IS_DELETE = table.Column<int>(type: "integer", nullable: true),
                    CREATED_AT = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    UPDATED_AT = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    CREATED_BY = table.Column<string>(type: "character varying(250)", maxLength: 250, nullable: true),
                    UPDATED_BY = table.Column<string>(type: "character varying(250)", maxLength: 250, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TransactionType", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ApprovalMatrixConfig",
                schema: "crbanking",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    transactionTypeId = table.Column<int>(type: "integer", nullable: true),
                    version = table.Column<int>(type: "integer", nullable: true),
                    effectiveFrom = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    effectiveTo = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    isActive = table.Column<bool>(type: "boolean", nullable: false),
                    isApproved = table.Column<int>(type: "integer", nullable: true),
                    approvedByUserId = table.Column<int>(type: "integer", nullable: true),
                    approvedDate = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    IS_DELETE = table.Column<int>(type: "integer", nullable: true),
                    CREATED_AT = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    UPDATED_AT = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    CREATED_BY = table.Column<string>(type: "character varying(250)", maxLength: 250, nullable: true),
                    UPDATED_BY = table.Column<string>(type: "character varying(250)", maxLength: 250, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ApprovalMatrixConfig", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ApprovalMatrixConfig_TransactionType_transactionTypeId",
                        column: x => x.transactionTypeId,
                        principalSchema: "crbanking",
                        principalTable: "TransactionType",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "FileTemplateConfig",
                schema: "crbanking",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    transactionTypeId = table.Column<int>(type: "integer", nullable: true),
                    version = table.Column<int>(type: "integer", nullable: true),
                    effectiveFrom = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    effectiveTo = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    isActive = table.Column<bool>(type: "boolean", nullable: false),
                    IS_DELETE = table.Column<int>(type: "integer", nullable: true),
                    CREATED_AT = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    UPDATED_AT = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    CREATED_BY = table.Column<string>(type: "character varying(250)", maxLength: 250, nullable: true),
                    UPDATED_BY = table.Column<string>(type: "character varying(250)", maxLength: 250, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FileTemplateConfig", x => x.Id);
                    table.ForeignKey(
                        name: "FK_FileTemplateConfig_TransactionType_transactionTypeId",
                        column: x => x.transactionTypeId,
                        principalSchema: "crbanking",
                        principalTable: "TransactionType",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "ApprovalMatrixSlab",
                schema: "crbanking",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    approvalMatrixConfigId = table.Column<int>(type: "integer", nullable: true),
                    slabOrder = table.Column<int>(type: "integer", nullable: true),
                    minAmount = table.Column<decimal>(type: "numeric", nullable: true),
                    maxAmount = table.Column<decimal>(type: "numeric", nullable: true),
                    isAutoApprove = table.Column<bool>(type: "boolean", nullable: true),
                    isCheckerRequired = table.Column<bool>(type: "boolean", nullable: true),
                    checkerRoleId = table.Column<int>(type: "integer", nullable: true),
                    checkerRoleName = table.Column<string>(type: "text", nullable: true),
                    isActive = table.Column<bool>(type: "boolean", nullable: true),
                    IS_DELETE = table.Column<int>(type: "integer", nullable: true),
                    CREATED_AT = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    UPDATED_AT = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    CREATED_BY = table.Column<string>(type: "character varying(250)", maxLength: 250, nullable: true),
                    UPDATED_BY = table.Column<string>(type: "character varying(250)", maxLength: 250, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ApprovalMatrixSlab", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ApprovalMatrixSlab_ApprovalMatrixConfig_approvalMatrixConfi~",
                        column: x => x.approvalMatrixConfigId,
                        principalSchema: "crbanking",
                        principalTable: "ApprovalMatrixConfig",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "TransactionBatch",
                schema: "crbanking",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    batchNo = table.Column<Guid>(type: "uuid", nullable: false),
                    transactionTypeId = table.Column<int>(type: "integer", nullable: true),
                    approvalMatrixConfigId = table.Column<int>(type: "integer", nullable: true),
                    fileName = table.Column<string>(type: "text", nullable: true),
                    fileStoragePath = table.Column<string>(type: "text", nullable: true),
                    uploadedByUserId = table.Column<int>(type: "integer", nullable: true),
                    uploadedByUserName = table.Column<string>(type: "text", nullable: true),
                    uploadedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    totalRecords = table.Column<int>(type: "integer", nullable: true),
                    validRecords = table.Column<int>(type: "integer", nullable: true),
                    invalidRecords = table.Column<int>(type: "integer", nullable: true),
                    totalAmount = table.Column<decimal>(type: "numeric", nullable: true),
                    validationErrors = table.Column<string>(type: "text", nullable: true),
                    status = table.Column<string>(type: "text", nullable: true),
                    IS_DELETE = table.Column<int>(type: "integer", nullable: true),
                    CREATED_AT = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    UPDATED_AT = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    CREATED_BY = table.Column<string>(type: "character varying(250)", maxLength: 250, nullable: true),
                    UPDATED_BY = table.Column<string>(type: "character varying(250)", maxLength: 250, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TransactionBatch", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TransactionBatch_ApprovalMatrixConfig_approvalMatrixConfigId",
                        column: x => x.approvalMatrixConfigId,
                        principalSchema: "crbanking",
                        principalTable: "ApprovalMatrixConfig",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_TransactionBatch_TransactionType_transactionTypeId",
                        column: x => x.transactionTypeId,
                        principalSchema: "crbanking",
                        principalTable: "TransactionType",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "FileFieldMapping",
                schema: "crbanking",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    fileTemplateConfigId = table.Column<int>(type: "integer", nullable: true),
                    columnOrder = table.Column<int>(type: "integer", nullable: true),
                    excelColumnName = table.Column<string>(type: "text", nullable: true),
                    fieldKey = table.Column<string>(type: "text", nullable: true),
                    dataType = table.Column<string>(type: "text", nullable: true),
                    isMandatory = table.Column<bool>(type: "boolean", nullable: true),
                    validationRegex = table.Column<string>(type: "text", nullable: true),
                    maxLength = table.Column<int>(type: "integer", nullable: true),
                    IS_DELETE = table.Column<int>(type: "integer", nullable: true),
                    CREATED_AT = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    UPDATED_AT = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    CREATED_BY = table.Column<string>(type: "character varying(250)", maxLength: 250, nullable: true),
                    UPDATED_BY = table.Column<string>(type: "character varying(250)", maxLength: 250, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FileFieldMapping", x => x.Id);
                    table.ForeignKey(
                        name: "FK_FileFieldMapping_FileTemplateConfig_fileTemplateConfigId",
                        column: x => x.fileTemplateConfigId,
                        principalSchema: "crbanking",
                        principalTable: "FileTemplateConfig",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "ApprovalStep",
                schema: "crbanking",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    approvalMatrixSlabId = table.Column<int>(type: "integer", nullable: true),
                    stepOrder = table.Column<int>(type: "integer", nullable: true),
                    approvalLogic = table.Column<string>(type: "text", nullable: true),
                    IS_DELETE = table.Column<int>(type: "integer", nullable: true),
                    CREATED_AT = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    UPDATED_AT = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    CREATED_BY = table.Column<string>(type: "character varying(250)", maxLength: 250, nullable: true),
                    UPDATED_BY = table.Column<string>(type: "character varying(250)", maxLength: 250, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ApprovalStep", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ApprovalStep_ApprovalMatrixSlab_approvalMatrixSlabId",
                        column: x => x.approvalMatrixSlabId,
                        principalSchema: "crbanking",
                        principalTable: "ApprovalMatrixSlab",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "TransactionRecord",
                schema: "crbanking",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    transactionBatchId = table.Column<int>(type: "integer", nullable: true),
                    rowNo = table.Column<int>(type: "integer", nullable: true),
                    instructionRefNo = table.Column<string>(type: "text", nullable: true),
                    bankAccountNo = table.Column<string>(type: "text", nullable: true),
                    accountHolderName = table.Column<string>(type: "text", nullable: true),
                    routingNumber = table.Column<string>(type: "text", nullable: true),
                    beneficiaryBankName = table.Column<string>(type: "text", nullable: true),
                    beneficiaryBranchName = table.Column<string>(type: "text", nullable: true),
                    beneficiaryAccountType = table.Column<string>(type: "text", nullable: true),
                    senderAccountNo = table.Column<string>(type: "text", nullable: true),
                    senderAccountName = table.Column<string>(type: "text", nullable: true),
                    amount = table.Column<decimal>(type: "numeric", nullable: true),
                    currency = table.Column<string>(type: "text", nullable: true),
                    valueDate = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    purposeCode = table.Column<string>(type: "text", nullable: true),
                    priority = table.Column<string>(type: "text", nullable: true),
                    narration = table.Column<string>(type: "text", nullable: true),
                    matrixSlabId = table.Column<int>(type: "integer", nullable: true),
                    checkerRequiredSnapshot = table.Column<bool>(type: "boolean", nullable: true),
                    status = table.Column<string>(type: "text", nullable: true),
                    currentStepOrder = table.Column<int>(type: "integer", nullable: true),
                    externalSystemRefNo = table.Column<string>(type: "text", nullable: true),
                    failureReason = table.Column<string>(type: "text", nullable: true),
                    rowVersion = table.Column<byte[]>(type: "bytea", rowVersion: true, nullable: true),
                    IS_DELETE = table.Column<int>(type: "integer", nullable: true),
                    CREATED_AT = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    UPDATED_AT = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    CREATED_BY = table.Column<string>(type: "character varying(250)", maxLength: 250, nullable: true),
                    UPDATED_BY = table.Column<string>(type: "character varying(250)", maxLength: 250, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TransactionRecord", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TransactionRecord_ApprovalMatrixSlab_matrixSlabId",
                        column: x => x.matrixSlabId,
                        principalSchema: "crbanking",
                        principalTable: "ApprovalMatrixSlab",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_TransactionRecord_TransactionBatch_transactionBatchId",
                        column: x => x.transactionBatchId,
                        principalSchema: "crbanking",
                        principalTable: "TransactionBatch",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "ApprovalStepRole",
                schema: "crbanking",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    approvalStepId = table.Column<int>(type: "integer", nullable: true),
                    roleId = table.Column<int>(type: "integer", nullable: true),
                    roleName = table.Column<string>(type: "text", nullable: true),
                    IS_DELETE = table.Column<int>(type: "integer", nullable: true),
                    CREATED_AT = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    UPDATED_AT = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    CREATED_BY = table.Column<string>(type: "character varying(250)", maxLength: 250, nullable: true),
                    UPDATED_BY = table.Column<string>(type: "character varying(250)", maxLength: 250, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ApprovalStepRole", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ApprovalStepRole_ApprovalStep_approvalStepId",
                        column: x => x.approvalStepId,
                        principalSchema: "crbanking",
                        principalTable: "ApprovalStep",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "TransactionApprovalAction",
                schema: "crbanking",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    transactionRecordId = table.Column<int>(type: "integer", nullable: true),
                    actionType = table.Column<string>(type: "text", nullable: true),
                    stepOrder = table.Column<int>(type: "integer", nullable: true),
                    actionByUserId = table.Column<int>(type: "integer", nullable: true),
                    actionByUserName = table.Column<string>(type: "text", nullable: true),
                    actionByRoleId = table.Column<int>(type: "integer", nullable: true),
                    actionByRoleName = table.Column<string>(type: "text", nullable: true),
                    actionAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    remarks = table.Column<string>(type: "text", nullable: true),
                    ipAddress = table.Column<string>(type: "text", nullable: true),
                    IS_DELETE = table.Column<int>(type: "integer", nullable: true),
                    CREATED_AT = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    UPDATED_AT = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    CREATED_BY = table.Column<string>(type: "character varying(250)", maxLength: 250, nullable: true),
                    UPDATED_BY = table.Column<string>(type: "character varying(250)", maxLength: 250, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TransactionApprovalAction", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TransactionApprovalAction_TransactionRecord_transactionReco~",
                        column: x => x.transactionRecordId,
                        principalSchema: "crbanking",
                        principalTable: "TransactionRecord",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateIndex(
                name: "IX_ApprovalMatrixConfig_transactionTypeId",
                schema: "crbanking",
                table: "ApprovalMatrixConfig",
                column: "transactionTypeId");

            migrationBuilder.CreateIndex(
                name: "IX_ApprovalMatrixSlab_approvalMatrixConfigId",
                schema: "crbanking",
                table: "ApprovalMatrixSlab",
                column: "approvalMatrixConfigId");

            migrationBuilder.CreateIndex(
                name: "IX_ApprovalStep_approvalMatrixSlabId",
                schema: "crbanking",
                table: "ApprovalStep",
                column: "approvalMatrixSlabId");

            migrationBuilder.CreateIndex(
                name: "IX_ApprovalStepRole_approvalStepId",
                schema: "crbanking",
                table: "ApprovalStepRole",
                column: "approvalStepId");

            migrationBuilder.CreateIndex(
                name: "IX_FileFieldMapping_fileTemplateConfigId",
                schema: "crbanking",
                table: "FileFieldMapping",
                column: "fileTemplateConfigId");

            migrationBuilder.CreateIndex(
                name: "IX_FileTemplateConfig_transactionTypeId",
                schema: "crbanking",
                table: "FileTemplateConfig",
                column: "transactionTypeId");

            migrationBuilder.CreateIndex(
                name: "IX_TransactionApprovalAction_transactionRecordId",
                schema: "crbanking",
                table: "TransactionApprovalAction",
                column: "transactionRecordId");

            migrationBuilder.CreateIndex(
                name: "IX_TransactionBatch_approvalMatrixConfigId",
                schema: "crbanking",
                table: "TransactionBatch",
                column: "approvalMatrixConfigId");

            migrationBuilder.CreateIndex(
                name: "IX_TransactionBatch_transactionTypeId",
                schema: "crbanking",
                table: "TransactionBatch",
                column: "transactionTypeId");

            migrationBuilder.CreateIndex(
                name: "IX_TransactionRecord_instructionRefNo",
                schema: "crbanking",
                table: "TransactionRecord",
                column: "instructionRefNo",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_TransactionRecord_matrixSlabId",
                schema: "crbanking",
                table: "TransactionRecord",
                column: "matrixSlabId");

            migrationBuilder.CreateIndex(
                name: "IX_TransactionRecord_transactionBatchId",
                schema: "crbanking",
                table: "TransactionRecord",
                column: "transactionBatchId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ApprovalStepRole",
                schema: "crbanking");

            migrationBuilder.DropTable(
                name: "AuditOutboxMessage",
                schema: "crbanking");

            migrationBuilder.DropTable(
                name: "FileFieldMapping",
                schema: "crbanking");

            migrationBuilder.DropTable(
                name: "TransactionApprovalAction",
                schema: "crbanking");

            migrationBuilder.DropTable(
                name: "ApprovalStep",
                schema: "crbanking");

            migrationBuilder.DropTable(
                name: "FileTemplateConfig",
                schema: "crbanking");

            migrationBuilder.DropTable(
                name: "TransactionRecord",
                schema: "crbanking");

            migrationBuilder.DropTable(
                name: "ApprovalMatrixSlab",
                schema: "crbanking");

            migrationBuilder.DropTable(
                name: "TransactionBatch",
                schema: "crbanking");

            migrationBuilder.DropTable(
                name: "ApprovalMatrixConfig",
                schema: "crbanking");

            migrationBuilder.DropTable(
                name: "TransactionType",
                schema: "crbanking");
        }
    }
}
