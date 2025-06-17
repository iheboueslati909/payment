using System.IdentityModel.Tokens.Jwt;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using System.Security.Claims;

namespace PaymentGateway.Infrastructure.Auth;

public class JwtValidator : IJwtValidator
{
    private readonly IConfiguration _configuration;
    private const string AppIdClaim = "appId";
    private const string RoleClaim = ClaimTypes.Role;

    public JwtValidator(IConfiguration configuration)
    {
        _configuration = configuration;
    }

    public Task<JwtValidationResult> ValidateToken(string token, string appSecret)
    {
        try
        {
            var tokenHandler = new JwtSecurityTokenHandler();
            var key = Encoding.UTF8.GetBytes(appSecret);

            var validationParameters = new TokenValidationParameters
            {
                ValidateIssuerSigningKey = true,
                IssuerSigningKey = new SymmetricSecurityKey(key),
                ValidateIssuer = true,
                ValidIssuer = _configuration["Jwt:Issuer"], // Validate against configured issuer
                ValidateAudience = true,
                ValidAudience = _configuration["Jwt:Audience"], // Validate against configured audience
                ClockSkew = TimeSpan.FromMinutes(5), // Added reasonable clock skew
                ValidAlgorithms = new[] { SecurityAlgorithms.HmacSha512 } // Explicit algorithm validation
            };

            var principal = tokenHandler.ValidateToken(token, validationParameters, out _);

            var appId = principal.FindFirst(AppIdClaim)?.Value;
            var role = principal.FindFirst(RoleClaim)?.Value;

            if (string.IsNullOrEmpty(appId) || string.IsNullOrEmpty(role))
            {
                return Task.FromResult(JwtValidationResult.Failure("Missing required claims"));
            }

            return Task.FromResult(JwtValidationResult.Success(appId, role));
        }
        catch (SecurityTokenException ex)
        {
            return Task.FromResult(JwtValidationResult.Failure($"Token validation failed: {ex.Message}"));
        }
    }
}
