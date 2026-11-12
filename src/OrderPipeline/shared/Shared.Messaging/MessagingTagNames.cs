namespace Shared.Messaging;

/// <summary>
/// OpenTelemetry semantic-convention attribute names for messaging spans.
/// </summary>
public static class MessagingTagNames
{
    /// <summary>Messaging system in use, for example <c>rabbitmq</c>.</summary>
    public const string MessagingSystem = "messaging.system";

    /// <summary>Kind of destination, for example <c>exchange</c> or <c>queue</c>.</summary>
    public const string MessagingDestinationKind = "messaging.destination_kind";

    /// <summary>Operation performed, for example <c>publish</c> or <c>process</c>.</summary>
    public const string MessagingOperation = "messaging.operation";

    /// <summary>Name of the destination the operation acted on.</summary>
    public const string MessagingDestinationName = "messaging.destination.name";

    /// <summary>CLR type name of the event contract carried by the message.</summary>
    public const string MessagingEventType = "messaging.event_type";

    /// <summary>Whether the handler completed or threw.</summary>
    public const string MessagingOutcome = "messaging.outcome";
}
