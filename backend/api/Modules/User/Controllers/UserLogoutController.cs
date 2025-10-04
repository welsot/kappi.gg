using api.Modules.Common.Controllers;
using api.Modules.Common.Data;
using api.Modules.Common.DTO;
using api.Modules.User.Auth;
using api.Modules.User.Repository;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace api.Modules.User.Controllers;

[ApiController]
public class UserLogoutController(
    Db db,
    ILogger<UserLogoutController> logger,
    IRefreshTokenRepository refreshTokenRepository
) : ApiController
{
    [Authorize(AuthenticationSchemes = ApiTokenAuthOptions.DefaultScheme)]
    [ApiTokenRequired]
    [ProducesResponseType(typeof(SuccessResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    [EndpointName("apiUsersLogout")]
    [HttpPost("api/users/logout")]
    public async Task<IActionResult> Logout()
    {
        try
        {
            var userId = GetUserId();

            // Revoke all refresh tokens for this user
            await refreshTokenRepository.RevokeAllUserTokensAsync(userId);
            await db.SaveChangesAsync();

            // Clear cookies
            Response.Cookies.Delete("apiToken");
            Response.Cookies.Delete("refreshToken");

            return Ok(new SuccessResponse("logged_out"));
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error logging out user");
            return Error(500, "unexpected_server_error");
        }
    }
}
