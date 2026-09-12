using Microsoft.EntityFrameworkCore;

using ASPER.CORPORATE_BANKING.Domain.Entities;

namespace ASPER.CORPORATE_BANKING.Infrastructure.Data
{
    public class CORPORATE_BANKINGDbContext : DbContext
    {
        public DbSet<TransactionType> TransactionTypes { get; set; }
        public DbSet<FileTemplateConfig> FileTemplateConfigs { get; set; }
        public DbSet<FileFieldMapping> FileFieldMappings { get; set; }
        public DbSet<ApprovalMatrixConfig> ApprovalMatrixConfigs { get; set; }
        public DbSet<ApprovalMatrixSlab> ApprovalMatrixSlabs { get; set; }
        public DbSet<ApprovalStep> ApprovalSteps { get; set; }
        public DbSet<ApprovalStepRole> ApprovalStepRoles { get; set; }
        public DbSet<TransactionBatch> TransactionBatches { get; set; }
        public DbSet<TransactionRecord> TransactionRecords { get; set; }
        public DbSet<TransactionApprovalAction> TransactionApprovalActions { get; set; }

        public CORPORATE_BANKINGDbContext(DbContextOptions<CORPORATE_BANKINGDbContext> options) : base(options)
        {
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);
        }
    }
}
