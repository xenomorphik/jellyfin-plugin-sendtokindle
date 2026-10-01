using System;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Jellyfin.Plugin.SendToKindle.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Jellyfin.Plugin.SendToKindle.Api
{
    public class UserEmailDto
    {
        public string Email { get; set; } = string.Empty;
    }

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

        [HttpGet("UserEmail")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        public ActionResult<UserEmailDto> GetUserEmail()
        {
            var userId = GetCurrentUserId();
            if (userId == null) return Unauthorized();

            var config = Plugin.Instance?.Configuration;
            if (config != null && config.UserTargetKindleEmails.TryGetValue(userId.ToString()!, out var email))
            {
                return Ok(new UserEmailDto { Email = email });
            }

            return Ok(new UserEmailDto { Email = config?.TargetKindleEmail ?? string.Empty });
        }

        [HttpPost("UserEmail")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        public ActionResult SetUserEmail([FromBody] UserEmailDto dto)
        {
            var userId = GetCurrentUserId();
            if (userId == null) return Unauthorized();

            var config = Plugin.Instance?.Configuration;
            if (config != null)
            {
                config.UserTargetKindleEmails[userId.ToString()!] = dto.Email;
                Plugin.Instance!.SaveConfiguration();
            }

            return NoContent();
        }

        /// <summary>
        /// Sends an item to the configured Kindle email.
        /// </summary>
        /// <param name="itemId">The ID of the item to send.</param>
        /// <returns>An <see cref="OkResult"/> on success, or a <see cref="BadRequestResult"/> on failure.</returns>
        [HttpPost("Send/{itemId}")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status412PreconditionFailed)]
        public async Task<ActionResult> SendItem([FromRoute, Required] Guid itemId)
        {
            var userId = GetCurrentUserId();
            if (userId == null) return Unauthorized();

            var config = Plugin.Instance?.Configuration;
            if (config == null) return StatusCode(StatusCodes.Status412PreconditionFailed, "Configuration missing.");

            config.UserTargetKindleEmails.TryGetValue(userId.ToString()!, out var targetEmail);
            if (string.IsNullOrEmpty(targetEmail))
            {
                targetEmail = config.TargetKindleEmail;
            }

            if (string.IsNullOrEmpty(targetEmail))
            {
                return StatusCode(StatusCodes.Status412PreconditionFailed, "Target Kindle Email not set for user.");
            }

            var success = await _extractionService.SendItemToKindleAsync(itemId, userId.Value, CancellationToken.None).ConfigureAwait(false);
            
            if (success)
            {
                return NoContent();
            }

            return BadRequest("Failed to send item to Kindle. Check server logs for details.");
        }
    }
}
