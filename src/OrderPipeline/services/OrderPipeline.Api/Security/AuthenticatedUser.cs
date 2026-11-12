namespace OrderPipeline.Api.Security;

public sealed record AuthenticatedUser(string Sub, string Email, string Name);
