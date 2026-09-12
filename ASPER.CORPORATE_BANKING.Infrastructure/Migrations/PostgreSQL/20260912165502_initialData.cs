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
            migrationBuilder.CreateTable(
                name: "AuditOutboxMessage",
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
                        principalTable: "TransactionType",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "FileTemplateConfig",
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
                        principalTable: "TransactionType",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "ApprovalMatrixSlab",
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
                        principalTable: "ApprovalMatrixConfig",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "TransactionBatch",
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
                        principalTable: "ApprovalMatrixConfig",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_TransactionBatch_TransactionType_transactionTypeId",
                        column: x => x.transactionTypeId,
                        principalTable: "TransactionType",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "FileFieldMapping",
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
                        principalTable: "FileTemplateConfig",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "ApprovalStep",
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
                        principalTable: "ApprovalMatrixSlab",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "TransactionRecord",
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
                        principalTable: "ApprovalMatrixSlab",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_TransactionRecord_TransactionBatch_transactionBatchId",
                        column: x => x.transactionBatchId,
                        principalTable: "TransactionBatch",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "ApprovalStepRole",
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
                        principalTable: "ApprovalStep",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "TransactionApprovalAction",
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
                        principalTable: "TransactionRecord",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateIndex(
                name: "IX_ApprovalMatrixConfig_transactionTypeId",
                table: "ApprovalMatrixConfig",
                column: "transactionTypeId");

            migrationBuilder.CreateIndex(
                name: "IX_ApprovalMatrixSlab_approvalMatrixConfigId",
                table: "ApprovalMatrixSlab",
                column: "approvalMatrixConfigId");

            migrationBuilder.CreateIndex(
                name: "IX_ApprovalStep_approvalMatrixSlabId",
                table: "ApprovalStep",
                column: "approvalMatrixSlabId");

            migrationBuilder.CreateIndex(
                name: "IX_ApprovalStepRole_approvalStepId",
                table: "ApprovalStepRole",
                column: "approvalStepId");

            migrationBuilder.CreateIndex(
                name: "IX_FileFieldMapping_fileTemplateConfigId",
                table: "FileFieldMapping",
                column: "fileTemplateConfigId");

            migrationBuilder.CreateIndex(
                name: "IX_FileTemplateConfig_transactionTypeId",
                table: "FileTemplateConfig",
                column: "transactionTypeId");

            migrationBuilder.CreateIndex(
                name: "IX_TransactionApprovalAction_transactionRecordId",
                table: "TransactionApprovalAction",
                column: "transactionRecordId");

            migrationBuilder.CreateIndex(
                name: "IX_TransactionBatch_approvalMatrixConfigId",
                table: "TransactionBatch",
                column: "approvalMatrixConfigId");

            migrationBuilder.CreateIndex(
                name: "IX_TransactionBatch_transactionTypeId",
                table: "TransactionBatch",
                column: "transactionTypeId");

            migrationBuilder.CreateIndex(
                name: "IX_TransactionRecord_instructionRefNo",
                table: "TransactionRecord",
                column: "instructionRefNo",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_TransactionRecord_matrixSlabId",
                table: "TransactionRecord",
                column: "matrixSlabId");

            migrationBuilder.CreateIndex(
                name: "IX_TransactionRecord_transactionBatchId",
                table: "TransactionRecord",
                column: "transactionBatchId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ApprovalStepRole");

            migrationBuilder.DropTable(
                name: "AuditOutboxMessage");

            migrationBuilder.DropTable(
                name: "FileFieldMapping");

            migrationBuilder.DropTable(
                name: "TransactionApprovalAction");

            migrationBuilder.DropTable(
                name: "ApprovalStep");

            migrationBuilder.DropTable(
                name: "FileTemplateConfig");

            migrationBuilder.DropTable(
                name: "TransactionRecord");

            migrationBuilder.DropTable(
                name: "ApprovalMatrixSlab");

            migrationBuilder.DropTable(
                name: "TransactionBatch");

            migrationBuilder.DropTable(
                name: "ApprovalMatrixConfig");

            migrationBuilder.DropTable(
                name: "TransactionType");
        }
    }
}
