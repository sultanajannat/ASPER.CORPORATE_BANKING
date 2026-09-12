using ASPER.CORPORATE_BANKING.Contract;
using ASPER.CORPORATE_BANKING.Infrastructure.Data;
using MassTransit;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using ASPER.CORPORATE_BANKING.Infrastructure.Data.NPGDatabase; 
using ASPER.CORPORATE_BANKING.Infrastructure.Data.SQLDatabase;

namespace ASPER.CORPORATE_BANKING.Infrastructure
{
    public static class DependencyInjection
    {
        public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
        {
            var databaseProvider = configuration.GetValue<string>("DatabaseProvider");
            if (databaseProvider == "PostgreSQL")
            {
                services.AddDbContext<PostgreDbContext>((sp, options) =>
                {
                    options.UseNpgsql(configuration.GetConnectionString("NPGSqlConnectionString"),
                        x => x.MigrationsHistoryTable("__EFMigrationsHistory", "corporate_banking"));
                });
                
                // services.AddScoped<DbContext>(provider => provider.GetRequiredService<PostgreDbContext>());
            }
            else
            {
                services.AddDbContext<SQLDbContext>((sp, options) =>
                {
                    options.UseSqlServer(configuration.GetConnectionString("DpsConnection"),
                        x => x.MigrationsHistoryTable("__EFMigrationsHistory", "corporate_banking"));
                });

                // services.AddScoped<DbContext>(provider => provider.GetRequiredService<SQLDbContext>());
            }

            return services;
        }

        public static IServiceCollection AddMassTransitWithRabbitMQ(this IServiceCollection services)
        {
            services.AddMassTransit(x =>
            {
                x.UsingRabbitMq((context, cfg) =>
                {
                    cfg.Host("localhost", "/", h =>
                    {
                        h.Username("guest");
                        h.Password("guest");
                    });
                });
            });
            return services;
        }
    }
}
