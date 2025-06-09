namespace PaymentGateway.Infrastructure.Auth;

public interface IJwtValidator
{
    Task<JwtValidationResult> ValidateToken(string token, string appSecret);
}

public record JwtValidationResult
{
    public bool IsValid { get; init; }
    public string? AppId { get; init; }
    public string? Role { get; init; }
    public string? Error { get; init; }

    public static JwtValidationResult Success(string appId, string role) =>
        new() { IsValid = true, AppId = appId, Role = role };

    public static JwtValidationResult Failure(string error) =>
        new() { IsValid = false, Error = error };
}
