namespace Identity.Api.Logging;

/// <summary>
/// Event ids for this service. The 5000 range belongs to Identity.Api.
/// </summary>
public static class LogEventIds
{
    /// <summary>A customer registered themselves.</summary>
    public const int UserRegistered = 5001;

    /// <summary>An administrator created an account.</summary>
    public const int AccountCreated = 5002;

    /// <summary>An administrator reset an account password.</summary>
    public const int PasswordReset = 5003;

    /// <summary>An administrator changed the roles on an account.</summary>
    public const int RolesChanged = 5004;

    /// <summary>An administrator deleted an account.</summary>
    public const int AccountDeleted = 5005;

    /// <summary>The Keycloak Admin API could not be reached.</summary>
    public const int KeycloakUnreachable = 5006;

    /// <summary>
    /// Events that must always reach stdout. Every administrative change to an account is
    /// audited, because these are the actions a security review asks about first.
    /// </summary>
    public static readonly int[] Audited =
    [
        UserRegistered, AccountCreated, PasswordReset, RolesChanged, AccountDeleted
    ];
}

/// <summary>
/// Every log message Identity.Api emits.
/// </summary>
/// <remarks>
/// Accounts are referred to by their Keycloak subject id only. A username or an email
/// address is personally identifiable and must never reach the log backend.
/// </remarks>
public static partial class IdentityLog
{
    /// <summary>Logs that a customer registered themselves.</summary>
    /// <param name="logger">Logger to write to.</param>
    /// <param name="userId">Keycloak subject id of the new account.</param>
    [LoggerMessage(
        EventId = LogEventIds.UserRegistered,
        Level = LogLevel.Information,
        Message = "Registered account {UserId}")]
    public static partial void UserRegistered(this ILogger logger, string userId);

    /// <summary>Logs that an administrator created an account.</summary>
    /// <param name="logger">Logger to write to.</param>
    /// <param name="userId">Keycloak subject id of the new account.</param>
    /// <param name="roles">Roles the account was given.</param>
    [LoggerMessage(
        EventId = LogEventIds.AccountCreated,
        Level = LogLevel.Information,
        Message = "Administrator created account {UserId} with roles {Roles}")]
    public static partial void AccountCreated(
        this ILogger logger, string userId, IReadOnlyList<string> roles);

    /// <summary>Logs that an administrator reset an account password.</summary>
    /// <param name="logger">Logger to write to.</param>
    /// <param name="userId">Keycloak subject id of the account.</param>
    [LoggerMessage(
        EventId = LogEventIds.PasswordReset,
        Level = LogLevel.Information,
        Message = "Administrator reset the password for account {UserId}")]
    public static partial void PasswordReset(this ILogger logger, string userId);

    /// <summary>Logs that an administrator changed the roles on an account.</summary>
    /// <param name="logger">Logger to write to.</param>
    /// <param name="userId">Keycloak subject id of the account.</param>
    /// <param name="roles">Roles the account now has.</param>
    [LoggerMessage(
        EventId = LogEventIds.RolesChanged,
        Level = LogLevel.Information,
        Message = "Administrator set roles of account {UserId} to {Roles}")]
    public static partial void RolesChanged(
        this ILogger logger, string userId, IReadOnlyList<string> roles);

    /// <summary>Logs that an administrator deleted an account.</summary>
    /// <param name="logger">Logger to write to.</param>
    /// <param name="userId">Keycloak subject id of the deleted account.</param>
    [LoggerMessage(
        EventId = LogEventIds.AccountDeleted,
        Level = LogLevel.Information,
        Message = "Administrator deleted account {UserId}")]
    public static partial void AccountDeleted(this ILogger logger, string userId);

    /// <summary>Logs that the Keycloak Admin API could not be reached.</summary>
    /// <param name="logger">Logger to write to.</param>
    /// <param name="exception">The transport failure.</param>
    /// <param name="operation">What was being attempted when it failed.</param>
    [LoggerMessage(
        EventId = LogEventIds.KeycloakUnreachable,
        Level = LogLevel.Error,
        Message = "Keycloak Admin API is unreachable while {Operation}")]
    public static partial void KeycloakUnreachable(
        this ILogger logger, Exception exception, string operation);
}
