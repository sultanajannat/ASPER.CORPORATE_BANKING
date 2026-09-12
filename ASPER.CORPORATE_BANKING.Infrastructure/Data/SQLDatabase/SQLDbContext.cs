using Microsoft.EntityFrameworkCore;

namespace ASPER.CORPORATE_BANKING.Infrastructure.Data.SQLDatabase
{
    public class SQLDbContext : DbContext
    {
        public SQLDbContext(DbContextOptions<SQLDbContext> options) : base(options)
        {
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);
        }
    }
}
