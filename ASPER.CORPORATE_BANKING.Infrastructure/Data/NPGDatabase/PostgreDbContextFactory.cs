using Microsoft.AspNetCore.Builder;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;
using System;
using System.IO;

namespace ASPER.CORPORATE_BANKING.Infrastructure.Data.NPGDatabase
{
    public class PostgreDbContextFactory : IDesignTimeDbContextFactory<PostgreDbContext>
    {
        public PostgreDbContext CreateDbContext(string[] args)
        {

            //var basePath = Path.Combine(Directory.GetCurrentDirectory(), "../ASPER.DPS");


            //if (!File.Exists(Path.Combine(basePath, "appsettings.json")))
            //{
            //    basePath = Directory.GetCurrentDirectory();
            //}

            //var configuration = new ConfigurationBuilder()
            //    .SetBasePath(basePath)
            //    .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
            //    .Build();


            //var connectionString = configuration.GetConnectionString("NPGSqlConnectionString");


            //var optionsBuilder = new DbContextOptionsBuilder<PostgreDbContext>();
            //optionsBuilder.UseNpgsql(connectionString);


            //return new PostgreDbContext(optionsBuilder.Options, null);

            var builder = WebApplication.CreateBuilder(args);

            var clientProfile = builder.Configuration["CLIENT"]
                ?? Environment.GetEnvironmentVariable("CLIENT");


            IConfigurationRoot configuration = new ConfigurationBuilder()
            .SetBasePath(Path.Combine(Directory.GetCurrentDirectory(), "../ASPER.DPS"))
            .AddJsonFile($"appsettings.{clientProfile}.json")
            .Build();

            var optionsBuilder = new DbContextOptionsBuilder<PostgreDbContext>();
            string connectionString = configuration.GetConnectionString("NPGSqlConnectionString");

            optionsBuilder.UseNpgsql(connectionString,
            x => x.MigrationsHistoryTable("__EFMigrationsHistory", "crbanking"));

            return new PostgreDbContext(optionsBuilder.Options, null);
        }
    }
}
