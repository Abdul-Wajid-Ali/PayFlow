using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using PayFlow.Application.Common.Interfaces;
using PayFlow.Application.Common.Models;
using PayFlow.Domain.Entities;
using PayFlow.Infrastructure.Settings;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace PayFlow.Infrastructure.Services
{
    public class JwtService : IJwtService
    {
        private readonly JwtSettings _jwtSettings;
        private readonly IDateTimeProvider _dateTimeProvider;

        public JwtService(IOptions<JwtSettings> settings, IDateTimeProvider dateTimeProvider)
        {
            _jwtSettings = settings.Value;
            _dateTimeProvider = dateTimeProvider;
        }

        public JwtTokenResult GenerateToken(User user)
        {
            //1: Preparing token configuration (expiry time and signing credentials)
            var expiresAt = _dateTimeProvider.UtcNow.AddMinutes(_jwtSettings.ExpiryInMinutes);
            var siginingKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_jwtSettings.SecretKey));
            var credentials = new SigningCredentials(siginingKey, SecurityAlgorithms.HmacSha256);

            //2: Defining claims that will be embedded inside the JWT
            var claims = new[]
            {
                new Claim(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
                new Claim(JwtRegisteredClaimNames.Email, user.Email.ToString()),
                new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
                new Claim("userId", user.Id.ToString())
            };

            //3: Creating the JWT token with issuer, audience, claims and signature
            var token = new JwtSecurityToken(
                issuer: _jwtSettings.Issuer,
                audience: _jwtSettings.Audience,
                claims: claims,
                expires: expiresAt,
                signingCredentials: credentials
            );

            //4: Serializing the token to string and returning result with expiry
            return new JwtTokenResult(
                Value: new JwtSecurityTokenHandler().WriteToken(token),
                ExpiresAt: expiresAt
            );
        }
    }
}