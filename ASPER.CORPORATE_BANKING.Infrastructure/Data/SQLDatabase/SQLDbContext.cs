using Microsoft.EntityFrameworkCore;

using ASPER.CORPORATE_BANKING.Domain.Entities;

namespace ASPER.CORPORATE_BANKING.Infrastructure.Data.SQLDatabase
{
    public class SQLDbContext : CORPORATE_BANKINGDbContext
    {
        public SQLDbContext(DbContextOptions<SQLDbContext> options) : base(options)
        {
        }
    }
}
