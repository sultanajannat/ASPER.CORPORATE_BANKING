using Microsoft.AspNetCore.Builder;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;
using System;
using System.IO;

namespace ASPER.CORPORATE_BANKING.Infrastructure.Data.SQLDatabase
{
    public class SQLDbContextFactory : IDesignTimeDbContextFactory<SQLDbContext>
    {
        public SQLDbContext CreateDbContext(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            var clientProfile = builder.Configuration["CLIENT"]
                ?? Environment.GetEnvironmentVariable("CLIENT") ?? "";

            IConfigurationRoot configuration = new ConfigurationBuilder()
                .SetBasePath(Path.Combine(Directory.GetCurrentDirectory(), "../ASPER.CORPORATE_BANKING"))
                .AddJsonFile($"appsettings.{(string.IsNullOrEmpty(clientProfile) ? "" : clientProfile + ".")}json", optional: true)
                .Build();

            var optionsBuilder = new DbContextOptionsBuilder<SQLDbContext>();
            string connectionString = configuration.GetConnectionString("DefaultConnection");

            optionsBuilder.UseSqlServer(connectionString,
                x => x.MigrationsHistoryTable("__EFMigrationsHistory", "corporate_banking"));

            return new SQLDbContext(optionsBuilder.Options);
        }
    }
}
