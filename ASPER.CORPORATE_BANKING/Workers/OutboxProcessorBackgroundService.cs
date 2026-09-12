using System;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using MassTransit;
using ASPER.CORPORATE_BANKING.Application.DTOs;
using ASPER.CORPORATE_BANKING.Infrastructure.Data;

namespace ASPER.CORPORATE_BANKING.Workers
{
    public class OutboxProcessorBackgroundService : BackgroundService
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly ILogger<OutboxProcessorBackgroundService> _logger;

        public OutboxProcessorBackgroundService(
            IServiceProvider serviceProvider, 
            ILogger<OutboxProcessorBackgroundService> logger)
        {
            _serviceProvider = serviceProvider;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("Outbox Processor Background Service started.");

            using var timer = new PeriodicTimer(TimeSpan.FromSeconds(10));

            while (await timer.WaitForNextTickAsync(stoppingToken))
            {
                if (stoppingToken.IsCancellationRequested)
                    break;

                await ProcessOutboxMessagesAsync(stoppingToken);
            }
        }

        private async Task ProcessOutboxMessagesAsync(CancellationToken stoppingToken)
        {
            using var scope = _serviceProvider.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<CORPORATE_BANKINGDbContext>();
            var publishEndpoint = scope.ServiceProvider.GetRequiredService<IPublishEndpoint>();

            var pendingMessages = await context.AuditOutboxMessages
                .Where(m => m.Status == "Pending" || (m.Status == "Failed" && m.RetryCount < 3))
                .OrderBy(m => m.CreatedAt)
                .Take(50)
                .ToListAsync(stoppingToken);

            if (!pendingMessages.Any()) return;

            foreach (var message in pendingMessages)
            {
                try
                {
                    if (message.EventType == nameof(ApprovedTransactionIntegrationEvent))
                    {
                        var integrationEvent = JsonSerializer.Deserialize<ApprovedTransactionIntegrationEvent>(message.Payload);
                        if (integrationEvent != null)
                        {
                            await publishEndpoint.Publish(integrationEvent, stoppingToken);
                        }
                    }
                    
                    message.Status = "Processed";
                    message.ProcessedAt = DateTime.UtcNow;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, $"Failed to process outbox message {message.Id}.");
                    message.Status = "Failed";
                    message.RetryCount++;
                    message.Error = ex.Message;
                }
            }

            await context.SaveChangesAsync(stoppingToken);
        }
    }
}
