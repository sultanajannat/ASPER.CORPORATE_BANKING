using Microsoft.EntityFrameworkCore;

namespace ASPER.CORPORATE_BANKING.Infrastructure.Data
{
    public class CORPORATE_BANKINGDbContext : DbContext
    {
        public CORPORATE_BANKINGDbContext(DbContextOptions<CORPORATE_BANKINGDbContext> options) : base(options)
        {
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);
        }
    }
}
