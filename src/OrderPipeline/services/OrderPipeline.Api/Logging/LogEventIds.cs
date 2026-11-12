namespace OrderPipeline.Api.Logging;

/// <summary>
/// Event ids for this service. The 2000 range belongs to OrderPipeline.Api, so an id never
/// collides with another service.
/// </summary>
public static class LogEventIds
{
    /// <summary>An order was accepted and persisted.</summary>
    public const int OrderCreated = 2001;

    /// <summary>An order was refused before it was persisted.</summary>
    public const int OrderCreationFailed = 2002;

    /// <summary>A caller asked for an order they cannot see, or that does not exist.</summary>
    public const int OrderNotFound = 2003;

    /// <summary>A saga event arrived for an order this service does not know.</summary>
    public const int OrderUnknownForEvent = 2004;

    /// <summary>An order moved to a new status because of a saga event.</summary>
    public const int OrderStatusChanged = 2005;

    /// <summary>A saga event was ignored because the order had already settled.</summary>
    public const int OrderStatusChangeIgnored = 2006;

    /// <summary>An integration event could not be published.</summary>
    public const int OrderEventPublishFailed = 2007;

    /// <summary>Inventory.Api could not be reached.</summary>
    public const int InventoryUnreachable = 2008;

    /// <summary>An order could not be written to the database.</summary>
    public const int OrderPersistenceFailed = 2009;

    /// <summary>An HTTP request reached a controller action.</summary>
    public const int RequestReceived = 2010;

    /// <summary>Events that must always reach stdout, whatever the configured log levels.</summary>
    public static readonly int[] Audited =
    [
        OrderCreated, OrderCreationFailed, OrderNotFound, OrderStatusChanged
    ];
}
