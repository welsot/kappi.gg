using api.Modules.Common.Controllers;
using api.Modules.Common.Data;
using api.Modules.Common.DTO;
using api.Modules.User.DTOs;
using api.Modules.User.Models;
using api.Modules.User.Repository;
using api.Modules.User.Services;

using Microsoft.AspNetCore.Mvc;

namespace api.Modules.User.Controllers;

[ApiController]
public class TokenRefreshController(
    Db db,
    ILogger<TokenRefreshController> logger,
    IRefreshTokenRepository refreshTokenRepository,
    IApiTokenRepository apiTokenRepository,
    UserMapper mapper,
    IWebHostEnvironment env
) : ApiController
{
    [ProducesResponseType(typeof(ApiTokenResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    [EndpointName("apiTokenRefresh")]
    [HttpPost("api/token/refresh")]
    public async Task<IActionResult> Refresh([FromBody] TokenRefreshDto dto)
    {
        if (!ModelState.IsValid) return BadRequest(ModelState);

        try
        {
            var refreshToken = await refreshTokenRepository.FindByTokenAsync(dto.RefreshToken);

            if (refreshToken == null || !refreshToken.IsValid())
            {
                return Error(401, "invalid_refresh_token");
            }

            // Create new access token
            var apiToken = new ApiToken(refreshToken.User, expirationMinutes: 30);
            apiTokenRepository.Add(apiToken);

            await db.SaveChangesAsync();
            var userDto = mapper.Map(refreshToken.User);

            // Set cookie for new access token
            var accessTokenCookieOptions = new CookieOptions
            {
                HttpOnly = true,
                Secure = env.IsProduction(),
                SameSite = SameSiteMode.Lax,
                Expires = DateTime.UtcNow.AddMinutes(30)
            };

            Response.Cookies.Append("apiToken", apiToken.Token, accessTokenCookieOptions);

            return Ok(
                new ApiTokenResponse(
                    Token: apiToken.Token,
                    RefreshToken: refreshToken.Token,
                    User: userDto
                )
            );
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error refreshing token");
            return Error(500, "unexpected_server_error");
        }
    }
}
