using Microsoft.EntityFrameworkCore;

namespace ASPER.CORPORATE_BANKING.Infrastructure.Data.NPGDatabase
{
    public class PostgreDbContext : DbContext
    {
        public PostgreDbContext(DbContextOptions<PostgreDbContext> options) : base(options)
        {
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);
        }
    }
}
