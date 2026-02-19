using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using Core.Application.Ports;
using Microsoft.AspNetCore.Identity;
using Microsoft.IdentityModel.Tokens;

namespace Infrastructure.Identity.Authentication;

public class AuthService(IPasswordHasher passwordHasher) : IAuthService
{
    private List<IdentityUser> IdentityUsers { get; } =
    [
        new()
        {
            Id = "aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa",
            UserName = "admin",
            PasswordHash = passwordHasher.Hash("password")
        },

        new()
        {
            Id = "bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb",
            UserName = "Luke",
            PasswordHash = passwordHasher.Hash("password")
        },

        new()
        {
            Id = "cccccccc-cccc-cccc-cccc-cccccccccccc",
            UserName = "Leia",
            PasswordHash = passwordHasher.Hash("password")
        },

        new()
        {
            Id = "dddddddd-dddd-dddd-dddd-dddddddddddd",
            UserName = "Padme",
            PasswordHash = passwordHasher.Hash("password")
        },

        new()
        {
            Id = "eeeeeeee-eeee-eeee-eeee-eeeeeeeeeeee",
            UserName = "Obi-Wan",
            PasswordHash = passwordHasher.Hash("password")
        }
    ];

    private RsaSecurityKey? SigningKey { get; set; }

    public bool CreateIdentityUser(Guid userId, string userName, string password)
    {
        var identityUser = new IdentityUser
        {
            Id = userId.ToString(),
            UserName = userName,
            PasswordHash = passwordHasher.Hash(password)
        };
        IdentityUsers.Add(identityUser);
        return true;
    }

    public void DeleteIdentityUser(Guid userIdentityId)
    {
        var user = IdentityUsers.FirstOrDefault(iu => iu.Id == userIdentityId.ToString());

        if (user != null) IdentityUsers.Remove(user);
    }

    public string GetToken(Guid userIdentityId, string requestEmail, string clearPassword, string userRole)
    {
        if (SigningKey == null) LoadSigningKey();

        var user = IdentityUsers.FirstOrDefault(iu => iu.Id == userIdentityId.ToString());

        if (user == null || user.UserName != requestEmail || string.IsNullOrEmpty(user.PasswordHash) ||
            !passwordHasher.Verify(clearPassword, user.PasswordHash))
            return string.Empty;

        var signingCredentials = new SigningCredentials(SigningKey, SecurityAlgorithms.RsaSha256);

        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.UniqueName, user.UserName!),
            new(JwtRegisteredClaimNames.Sub, userIdentityId.ToString()),
            new(ClaimTypes.Role, userRole)
        };

        var tokenDescriptor = new SecurityTokenDescriptor
        {
            Subject = new ClaimsIdentity(claims),
            Expires = DateTime.UtcNow.AddDays(7), // TODO
            SigningCredentials = signingCredentials,
            Issuer = AuthStatics.Issuer,
            Audience = AuthStatics.Audience
        };

        var tokenHandler = new JwtSecurityTokenHandler();
        var token = tokenHandler.CreateToken(tokenDescriptor);

        return tokenHandler.WriteToken(token);
    }

    private void LoadSigningKey()
    {
        var signingRsa = RSA.Create(2048);
        signingRsa.ImportFromPem(File.ReadAllText(AuthStatics.PrivateKeyPath));

        SigningKey = new RsaSecurityKey(signingRsa)
        {
            KeyId = "auth-server-signing-key"
        };
    }
}