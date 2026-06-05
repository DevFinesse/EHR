using System.Diagnostics;
using System.Diagnostics.Metrics;

namespace EHR.Messaging;

public static class MessagingTelemetry
{
    public const string ActivitySourceName = "EHR.Messaging";
    public const string MeterName = "EHR.Messaging";

    public static readonly ActivitySource ActivitySource = new(ActivitySourceName);
    public static readonly Meter Meter = new(MeterName);

    public static readonly Counter<long> KafkaPublishAttempts = Meter.CreateCounter<long>("ehr.kafka.publish.attempts");
    public static readonly Counter<long> KafkaPublishFailures = Meter.CreateCounter<long>("ehr.kafka.publish.failures");
    public static readonly Histogram<double> KafkaPublishDuration = Meter.CreateHistogram<double>("ehr.kafka.publish.duration.ms");

    public static readonly Counter<long> KafkaConsumedMessages = Meter.CreateCounter<long>("ehr.kafka.consume.messages");
    public static readonly Counter<long> KafkaConsumeFailures = Meter.CreateCounter<long>("ehr.kafka.consume.failures");
    public static readonly Histogram<double> KafkaConsumeDuration = Meter.CreateHistogram<double>("ehr.kafka.consume.duration.ms");

    public static readonly Counter<long> KafkaTopicsCreated = Meter.CreateCounter<long>("ehr.kafka.topics.created");
    public static readonly Counter<long> KafkaTopicInitializationFailures = Meter.CreateCounter<long>("ehr.kafka.topic_init.failures");

    public static readonly Counter<long> OutboxPublishAttempts = Meter.CreateCounter<long>("ehr.outbox.publish.attempts");
    public static readonly Counter<long> OutboxPublishSuccesses = Meter.CreateCounter<long>("ehr.outbox.publish.successes");
    public static readonly Counter<long> OutboxPublishFailures = Meter.CreateCounter<long>("ehr.outbox.publish.failures");

    public static KeyValuePair<string, object?> Tag(string key, object? value) => new(key, value);

    public static KeyValuePair<string, object?>[] Tags(params KeyValuePair<string, object?>[] tags) => tags;
}
