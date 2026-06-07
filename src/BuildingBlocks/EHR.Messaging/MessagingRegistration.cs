using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace EHR.Messaging;

public static class MessagingRegistration
{
    public static IServiceCollection AddEhrMessaging(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddSingleton<IOutboxPublisherSignal, OutboxPublisherSignal>();

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
        services.AddHostedService(provider => new KafkaTopicInitializer(
            bootstrapServers,
            ReadTopics(configuration),
            provider.GetRequiredService<ILogger<KafkaTopicInitializer>>()));
        services.AddHostedService<KafkaConsumerWorker>();
        return services;
    }

    private static int ReadInt(IConfiguration configuration, string key, int fallback) =>
        int.TryParse(configuration[key], out var value) ? value : fallback;

    private static IEnumerable<string> ReadTopics(IConfiguration configuration) =>
        configuration
            .GetSection("Kafka:ConsumerTopics")
            .GetChildren()
            .Select(topic => topic.Value)
            .Where(topic => !string.IsNullOrWhiteSpace(topic))
            .Cast<string>();
}
