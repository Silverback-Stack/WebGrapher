using Auth.WebApi.Auth.IdentityProviders;
using Auth.WebApi.IdentityProviders;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;
using System;
using System.ComponentModel.DataAnnotations;
using System.IdentityModel.Tokens.Jwt;
using System.Text;

namespace Auth.WebApi.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AuthController : ControllerBase
    {
        private readonly IIdentityProvider _identityProvider;
        private readonly AuthSettings _authSettings;

        public AuthController(IIdentityProvider identityProvider, AuthSettings authSettings)
        {
            _authSettings = authSettings;
            _identityProvider = identityProvider;
        }

        [AllowAnonymous]
        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginRequest request)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            try
            {
                // Validate credentials
                var isValid = await _identityProvider.ValidateCredentialsAsync(request.Username, request.Password);

                if (!isValid)
                {
                    return Unauthorized("Invalid credentials.");
                }

                // Generate JWT token
                var claims = await _identityProvider.GetClaimsAsync(request.Username);
                var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_authSettings.Jwt.Key));

                var token = new JwtSecurityToken(
                    issuer: _authSettings.Jwt.Issuer,
                    audience: _authSettings.Jwt.Audience,
                    claims: claims,
                    expires: DateTime.UtcNow.AddMinutes(_authSettings.Jwt.ExpiresInMinutes),
                    signingCredentials: new SigningCredentials(key, SecurityAlgorithms.HmacSha256)
                );

                var tokenString = new JwtSecurityTokenHandler().WriteToken(token);

                return Ok(new
                {
                    Token = tokenString,
                    Expires = token.ValidTo,
                    TokenType = "Bearer"
                });

            }
            catch (UnauthorizedProviderException ex)
            {
                // Return custom metadata thrown by the provider adapter
                return Unauthorized(ex.Response);
            }
        }

        public class LoginRequest
        {
            [Required]
            public string Username { get; set; } = string.Empty;

            [Required]
            public string Password { get; set; } = string.Empty;
        }
    }
}
