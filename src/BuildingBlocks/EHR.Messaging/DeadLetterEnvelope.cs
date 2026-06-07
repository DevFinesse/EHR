namespace EHR.Messaging;

public sealed record DeadLetterEnvelope(
    Guid DeadLetterId,
    string OriginalTopic,
    int OriginalPartition,
    long OriginalOffset,
    string ConsumerGroup,
    int Attempts,
    DateTimeOffset FailedAt,
    string ExceptionType,
    string ErrorMessage,
    EventEnvelope OriginalEnvelope);
