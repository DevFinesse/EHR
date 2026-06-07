using System.Threading.Channels;

namespace EHR.Messaging;

public interface IOutboxPublisherSignal
{
    void Signal();

    Task WaitAsync(TimeSpan timeout, CancellationToken cancellationToken);
}

public sealed class OutboxPublisherSignal : IOutboxPublisherSignal
{
    private readonly Channel<bool> _channel = Channel.CreateBounded<bool>(new BoundedChannelOptions(1)
    {
        AllowSynchronousContinuations = false,
        FullMode = BoundedChannelFullMode.DropWrite,
        SingleReader = true,
        SingleWriter = false
    });

    public void Signal()
    {
        _channel.Writer.TryWrite(true);
    }

    public async Task WaitAsync(TimeSpan timeout, CancellationToken cancellationToken)
    {
        var waitForSignal = _channel.Reader.WaitToReadAsync(cancellationToken).AsTask();
        var waitForTimeout = Task.Delay(timeout, cancellationToken);
        var completed = await Task.WhenAny(waitForSignal, waitForTimeout);

        if (completed == waitForSignal && await waitForSignal)
        {
            while (_channel.Reader.TryRead(out _))
            {
            }
        }
    }
}
