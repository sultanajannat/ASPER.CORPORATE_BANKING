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
                
                services.AddScoped<CORPORATE_BANKINGDbContext>(provider => provider.GetRequiredService<PostgreDbContext>());
            }
            else
            {
                services.AddDbContext<SQLDbContext>((sp, options) =>
                {
                    options.UseSqlServer(configuration.GetConnectionString("DpsConnection"),
                        x => x.MigrationsHistoryTable("__EFMigrationsHistory", "corporate_banking"));
                });

                services.AddScoped<CORPORATE_BANKINGDbContext>(provider => provider.GetRequiredService<SQLDbContext>());
            }

            return services;
        }

        public static IServiceCollection AddMassTransitWithRabbitMQ(this IServiceCollection services, IConfiguration configuration)
        {
            services.AddMassTransit(x =>
            {
                x.UsingRabbitMq((context, cfg) =>
                {
                    var rabbitMqHost = configuration["RabbitMqSettings:Host"] ?? "localhost";
                    var rabbitMqUser = configuration["RabbitMqSettings:Username"] ?? "guest";
                    var rabbitMqPass = configuration["RabbitMqSettings:Password"] ?? "guest";

                    cfg.Host(rabbitMqHost, "/", h =>
                    {
                        h.Username(rabbitMqUser);
                        h.Password(rabbitMqPass);
                    });
                });
            });
            return services;
        }
    }
}
