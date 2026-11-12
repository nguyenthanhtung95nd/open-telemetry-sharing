namespace Shared.Messaging;

/// <summary>
/// Publishes an integration event to a broker exchange.
/// </summary>
/// <remarks>
/// Business code depends on this abstraction rather than on the RabbitMQ client, so a
/// publisher can be substituted in tests and the broker stays replaceable.
/// </remarks>
public interface IRabbitMqPublisher
{
    /// <summary>Publishes <paramref name="event"/> to <paramref name="exchange"/>.</summary>
    /// <typeparam name="T">Event contract type.</typeparam>
    /// <param name="event">The event to serialise and send.</param>
    /// <param name="exchange">Fanout exchange to publish to.</param>
    void Publish<T>(T @event, string exchange);
}
