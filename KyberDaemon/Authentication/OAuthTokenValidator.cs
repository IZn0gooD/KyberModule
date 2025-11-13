using System.IO;
using System.Linq;
using System.Security;
using System.Security.Claims;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using KyberDaemon.Configuration;

namespace KyberDaemon.Authentication;

public sealed class OAuthTokenValidator
{
    private readonly JwtSecurityTokenHandler _handler = new();
    private readonly TokenValidationParameters _parameters;
    private readonly string[] _requiredScopes;

    public OAuthTokenValidator(OAuthConfiguration config)
    {
        if (!config.Enabled)
        {
            throw new InvalidOperationException("OAuth n'est pas activé dans la configuration");
        }

        if (!File.Exists(config.JwksPath))
        {
            throw new FileNotFoundException($"Fichier JWKS introuvable: {config.JwksPath}");
        }

        var jwksJson = File.ReadAllText(config.JwksPath);
        var jwks = new JsonWebKeySet(jwksJson);

        _parameters = new TokenValidationParameters
        {
            RequireSignedTokens = true,
            ValidateIssuerSigningKey = true,
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ClockSkew = TimeSpan.FromMinutes(2),
            ValidIssuer = config.Issuer,
            ValidAudience = config.Audience,
            IssuerSigningKeys = jwks.Keys
        };

        _requiredScopes = config.RequiredScopes ?? Array.Empty<string>();
    }

    public ClaimsPrincipal Validate(string token)
    {
        if (string.IsNullOrWhiteSpace(token))
        {
            throw new SecurityException("Token OAuth vide");
        }

        try
        {
            var principal = _handler.ValidateToken(token, _parameters, out _);
            EnsureScopes(principal);
            return principal;
        }
        catch (SecurityTokenException ex)
        {
            throw new SecurityException("Token OAuth invalide", ex);
        }
    }

    private void EnsureScopes(ClaimsPrincipal principal)
    {
        if (_requiredScopes.Length == 0)
        {
            return;
        }

        var scopeClaim = principal.FindFirst("scp")?.Value ?? principal.FindFirst("scope")?.Value;
        if (string.IsNullOrWhiteSpace(scopeClaim))
        {
            throw new SecurityException("Token OAuth sans scope");
        }

        var scopes = scopeClaim.Split(new[] { ' ', ',' }, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        if (!_requiredScopes.All(scope => scopes.Contains(scope, StringComparer.OrdinalIgnoreCase)))
        {
            throw new SecurityException("Token OAuth sans scope requis");
        }
    }
}
