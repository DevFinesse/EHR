using System.Collections.Concurrent;
using System.Linq.Expressions;

namespace EHR.Cqrs;

public interface ICommand<TResponse>;

public interface IQuery<TResponse>;

public interface ICommandHandler<in TCommand, TResponse>
    where TCommand : ICommand<TResponse>
{
    Task<TResponse> HandleAsync(TCommand command, CancellationToken cancellationToken);
}

public interface IQueryHandler<in TQuery, TResponse>
    where TQuery : IQuery<TResponse>
{
    Task<TResponse> HandleAsync(TQuery query, CancellationToken cancellationToken);
}

public interface ICqrsDispatcher
{
    Task<TResponse> SendAsync<TResponse>(ICommand<TResponse> command, CancellationToken cancellationToken);

    Task<TResponse> QueryAsync<TResponse>(IQuery<TResponse> query, CancellationToken cancellationToken);
}

public sealed class CqrsDispatcher : ICqrsDispatcher
{
    private static readonly ConcurrentDictionary<HandlerInvocationKey, Delegate> InvocationCache = new();

    private readonly IServiceProvider _serviceProvider;

    public CqrsDispatcher(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
    }

    public Task<TResponse> SendAsync<TResponse>(ICommand<TResponse> command, CancellationToken cancellationToken)
    {
        var handlerType = typeof(ICommandHandler<,>).MakeGenericType(command.GetType(), typeof(TResponse));
        var handler = _serviceProvider.GetService(handlerType)
            ?? throw new InvalidOperationException($"No command handler registered for {command.GetType().Name}.");

        return InvokeHandlerAsync<TResponse>(handler, command, cancellationToken);
    }

    public Task<TResponse> QueryAsync<TResponse>(IQuery<TResponse> query, CancellationToken cancellationToken)
    {
        var handlerType = typeof(IQueryHandler<,>).MakeGenericType(query.GetType(), typeof(TResponse));
        var handler = _serviceProvider.GetService(handlerType)
            ?? throw new InvalidOperationException($"No query handler registered for {query.GetType().Name}.");

        return InvokeHandlerAsync<TResponse>(handler, query, cancellationToken);
    }

    private static Task<TResponse> InvokeHandlerAsync<TResponse>(object handler, object message, CancellationToken cancellationToken)
    {
        var key = new HandlerInvocationKey(handler.GetType(), message.GetType(), typeof(TResponse));
        var invoker = (Func<object, object, CancellationToken, Task<TResponse>>)InvocationCache.GetOrAdd(
            key,
            static cacheKey => CreateInvoker<TResponse>(cacheKey.HandlerType, cacheKey.MessageType));

        return invoker(handler, message, cancellationToken);
    }

    private static Func<object, object, CancellationToken, Task<TResponse>> CreateInvoker<TResponse>(Type handlerType, Type messageType)
    {
        var method = handlerType.GetMethod("HandleAsync", [messageType, typeof(CancellationToken)])
            ?? throw new InvalidOperationException($"Handler {handlerType.Name} does not expose HandleAsync.");

        var handler = Expression.Parameter(typeof(object), "handler");
        var message = Expression.Parameter(typeof(object), "message");
        var cancellationToken = Expression.Parameter(typeof(CancellationToken), "cancellationToken");
        var call = Expression.Call(
            Expression.Convert(handler, handlerType),
            method,
            Expression.Convert(message, messageType),
            cancellationToken);

        var body = Expression.Convert(call, typeof(Task<TResponse>));
        return Expression.Lambda<Func<object, object, CancellationToken, Task<TResponse>>>(
            body,
            handler,
            message,
            cancellationToken).Compile();
    }

    private readonly record struct HandlerInvocationKey(Type HandlerType, Type MessageType, Type ResponseType);
}
