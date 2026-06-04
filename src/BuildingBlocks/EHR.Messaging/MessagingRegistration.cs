using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace EHR.Messaging;

public static class MessagingRegistration
{
    public static IServiceCollection AddEhrMessaging(this IServiceCollection services, IConfiguration configuration)
    {
        var bootstrapServers = configuration["Kafka:BootstrapServers"];
        if (string.IsNullOrWhiteSpace(bootstrapServers))
        {
            services.AddSingleton<IEventBus, InMemoryEventBus>();
            return services;
        }

        services.AddSingleton<IEventBus>(provider => new KafkaEventBus(
            new KafkaEventBusOptions(
                bootstrapServers,
                ReadInt(configuration, "Kafka:PublishTimeoutMilliseconds", 1500),
                ReadInt(configuration, "Kafka:SocketTimeoutMilliseconds", 1000),
                ReadInt(configuration, "Kafka:RequestTimeoutMilliseconds", 1000),
                ReadInt(configuration, "Kafka:MessageSendMaxRetries", 1),
                ReadInt(configuration, "Kafka:RetryBackoffMilliseconds", 100)),
            provider.GetRequiredService<ILogger<KafkaEventBus>>()));
        services.AddHostedService<KafkaConsumerWorker>();
        return services;
    }

    private static int ReadInt(IConfiguration configuration, string key, int fallback) =>
        int.TryParse(configuration[key], out var value) ? value : fallback;
}
