using System;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Jellyfin.Plugin.SendToKindle.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Jellyfin.Plugin.SendToKindle.Api;

/// <summary>
/// Controller for the Send to Kindle plugin API.
/// </summary>
[ApiController]
[Authorize]
[Route("SendToKindle")]
public class SendToKindleController : ControllerBase
{
    private readonly IKindleExtractionService _extractionService;

    /// <summary>
    /// Initializes a new instance of the <see cref="SendToKindleController"/> class.
    /// </summary>
    /// <param name="extractionService">The extraction service.</param>
    public SendToKindleController(IKindleExtractionService extractionService)
    {
        _extractionService = extractionService;
    }

    private Guid? GetCurrentUserId()
    {
        var userIdClaim = User.Claims.FirstOrDefault(c => c.Type == "Jellyfin-UserId")?.Value;
        if (Guid.TryParse(userIdClaim, out var userId))
        {
            return userId;
        }

        return null;
    }

    /// <summary>
    /// Gets the user email.
    /// </summary>
    /// <returns>The user email.</returns>
    [HttpGet("UserEmail")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public ActionResult<UserEmailDto> GetUserEmail()
    {
        var userId = GetCurrentUserId();
        if (userId == null)
        {
            return Unauthorized();
        }

        var config = Plugin.Instance?.Configuration;
        if (config != null && config.UserTargetKindleEmails.TryGetValue(userId.ToString()!, out var email))
        {
            return Ok(new UserEmailDto { Email = email });
        }

        return Ok(new UserEmailDto { Email = config?.TargetKindleEmail ?? string.Empty });
    }

    /// <summary>
    /// Sets the user email.
    /// </summary>
    /// <param name="dto">The dto.</param>
    /// <returns>The result.</returns>
    [HttpPost("UserEmail")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public ActionResult SetUserEmail([FromBody] UserEmailDto dto)
    {
        var userId = GetCurrentUserId();
        if (userId == null)
        {
            return Unauthorized();
        }

        var config = Plugin.Instance?.Configuration;
        if (config != null)
        {
            config.UserTargetKindleEmails[userId.ToString()!] = dto.Email;
            Plugin.Instance?.SaveConfiguration();
        }

        return NoContent();
    }

    /// <summary>
    /// Send an item.
    /// </summary>
    /// <param name="itemId">The item ID.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A task.</returns>
    [HttpPost("Send/{itemId}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    [ProducesResponseType(StatusCodes.Status412PreconditionFailed)]
    public async Task<ActionResult> SendItem([FromRoute, Required] Guid itemId, CancellationToken cancellationToken)
    {
        var userId = GetCurrentUserId();
        if (userId == null)
        {
            return Unauthorized();
        }

        var config = Plugin.Instance?.Configuration;
        bool hasEmail = false;

        if (config != null && config.UserTargetKindleEmails.TryGetValue(userId.ToString()!, out var email))
        {
            hasEmail = !string.IsNullOrWhiteSpace(email);
        }
        else if (!string.IsNullOrWhiteSpace(config?.TargetKindleEmail))
        {
            hasEmail = true;
        }

        if (!hasEmail)
        {
            return StatusCode(StatusCodes.Status412PreconditionFailed, "User has not configured a target Kindle email address.");
        }

        try
        {
            await _extractionService.SendItemToKindleAsync(itemId, userId.Value, cancellationToken).ConfigureAwait(false);
            return NoContent();
        }
        catch (Exception)
        {
            return StatusCode(StatusCodes.Status500InternalServerError, "Failed to send item to Kindle.");
        }
    }
}
