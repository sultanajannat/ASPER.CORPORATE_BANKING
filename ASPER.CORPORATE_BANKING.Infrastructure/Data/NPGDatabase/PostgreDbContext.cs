using ASPER.CORPORATE_BANKING.Domain.Entities;
using ASPER.CORPORATE_BANKING.Domain.Entities;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using System;

namespace ASPER.CORPORATE_BANKING.Infrastructure.Data.NPGDatabase
{
    public class PostgreDbContext : CORPORATE_BANKINGDbContext
    {
        public PostgreDbContext(DbContextOptions<PostgreDbContext> options, IHttpContextAccessor httpContextAccessor)
            : base(options, httpContextAccessor)
        {
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);


            foreach (var entity in modelBuilder.Model.GetEntityTypes())
            {
                var dateTimeConverter = new ValueConverter<DateTime, DateTime>(
                    v => DateTime.SpecifyKind(v, DateTimeKind.Unspecified),
                    v => DateTime.SpecifyKind(v, DateTimeKind.Unspecified)
                );

                var nullableDateTimeConverter = new ValueConverter<DateTime?, DateTime?>(
                    v => v.HasValue ? DateTime.SpecifyKind(v.Value, DateTimeKind.Unspecified) : v,
                    v => v.HasValue ? DateTime.SpecifyKind(v.Value, DateTimeKind.Unspecified) : v
                );

                foreach (var property in entity.GetProperties())
                {
                    if (property.ClrType == typeof(DateTime))
                    {
                        property.SetValueConverter(dateTimeConverter);
                        property.SetColumnType("timestamp without time zone");
                    }
                    else if (property.ClrType == typeof(DateTime?))
                    {
                        property.SetValueConverter(nullableDateTimeConverter);
                        property.SetColumnType("timestamp without time zone");
                    }
                }
            }
        }
    }
}
