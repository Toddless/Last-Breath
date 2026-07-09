namespace LastBreathTest.LootSimulation
{
    using Core.Events;
    using Core.MessageBus;

    /// <summary>In-memory event bus: dispatches synchronously and keeps the last event of each
    /// type so the simulator can read per-kill telemetry the pipeline publishes.</summary>
    internal sealed class SimEventBus : IGameEventBus
    {
        private readonly Dictionary<Type, List<Delegate>> _handlers = [];
        private readonly Dictionary<Type, object> _lastEvents = [];

        public void Publish<T>(T evnt)
            where T : notnull, IGameEvent
        {
            _lastEvents[typeof(T)] = evnt;
            if (!_handlers.TryGetValue(typeof(T), out var handlers)) return;
            foreach (var handler in handlers.Cast<Action<T>>()) handler(evnt);
        }

        public void Subscribe<T>(Action<T> handler)
            where T : notnull, IGameEvent
        {
            if (!_handlers.TryGetValue(typeof(T), out var handlers)) _handlers[typeof(T)] = handlers = [];
            handlers.Add(handler);
        }

        public void Unsubscribe<T>(Action<T> handler)
            where T : notnull, IGameEvent
        {
            if (_handlers.TryGetValue(typeof(T), out var handlers)) handlers.Remove(handler);
        }

        /// <summary>Returns the last published event of the type and clears it, so stale
        /// telemetry never leaks into the next kill.</summary>
        public T? TakeLast<T>()
            where T : class, IGameEvent =>
            _lastEvents.Remove(typeof(T), out object? evnt) ? (T)evnt : null;
    }

    /// <summary>Request-routing message bus: the simulator registers the real handlers it needs.</summary>
    internal sealed class SimMessageBus : IGameMessageBus
    {
        private readonly Dictionary<Type, object> _handlers = [];

        public void Register<TRequest, TResponse>(IRequestHandler<TRequest, TResponse> handler)
            where TRequest : IRequest<TResponse> => _handlers[typeof(TRequest)] = handler;

        public Task<TResponce> SendRequest<TRequest, TResponce>(TRequest request)
            where TRequest : IRequest<TResponce> =>
            _handlers.TryGetValue(typeof(TRequest), out object? handler)
                ? ((IRequestHandler<TRequest, TResponce>)handler).HandleRequest(request)
                : throw new InvalidOperationException($"No handler registered for {typeof(TRequest).Name}");

        public Task PublishMessageAsync<TMessage>(TMessage message)
            where TMessage : IMessage => Task.CompletedTask;
    }
}
