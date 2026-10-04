using System;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Jellyfin.Plugin.SendToKindle.Configuration;
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
    private readonly ISmtpDeliveryService _smtpDeliveryService;

    /// <summary>
    /// Initializes a new instance of the <see cref="SendToKindleController"/> class.
    /// </summary>
    /// <param name="extractionService">The extraction service.</param>
    /// <param name="smtpDeliveryService">The smtp delivery service.</param>
    public SendToKindleController(IKindleExtractionService extractionService, ISmtpDeliveryService smtpDeliveryService)
    {
        _extractionService = extractionService;
        _smtpDeliveryService = smtpDeliveryService;
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
    /// Sends a test email to verify SMTP settings.
    /// </summary>
    /// <param name="request">The test email configuration.</param>
    /// <returns>An <see cref="OkResult"/> on success, or a <see cref="BadRequestResult"/> on failure.</returns>
    [HttpPost("TestEmail")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult> TestEmail([FromBody] TestEmailRequestDto request)
    {
        var success = await _smtpDeliveryService.SendTestEmailAsync(
            request.SmtpServer,
            request.SmtpPort,
            request.SmtpUsername,
            request.SmtpPassword,
            request.TargetEmail,
            CancellationToken.None).ConfigureAwait(false);

        if (success)
        {
            return NoContent();
        }

        return StatusCode(StatusCodes.Status500InternalServerError, "Failed to send test email. Check server logs for details.");
    }

    /// <summary>
    /// Gets the current user's target Kindle email.
    /// </summary>
    /// <returns>The user email.</returns>
    [HttpGet("UserEmail")]
    public ActionResult<UserEmailDto> GetUserEmail()
    {
        var userId = GetCurrentUserId();
        if (userId == null)
        {
            return Unauthorized();
        }

        var config = Plugin.Instance?.Configuration;
        if (config != null)
        {
            var userMap = config.UserTargetKindleEmails.FirstOrDefault(u => u.UserId == userId.ToString());
            if (userMap != null && !string.IsNullOrEmpty(userMap.Email))
            {
                return Ok(new UserEmailDto { Email = userMap.Email });
            }
        }

        return Ok(new UserEmailDto { Email = config?.TargetKindleEmail ?? string.Empty });
    }

    /// <summary>
    /// Sets the current user's target Kindle email.
    /// </summary>
    /// <param name="dto">The new email.</param>
    /// <returns>A no content result.</returns>
    [HttpPost("UserEmail")]
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
            var userMap = config.UserTargetKindleEmails.FirstOrDefault(u => u.UserId == userId.ToString());
            if (userMap != null)
            {
                userMap.Email = dto.Email;
            }
            else
            {
                var list = config.UserTargetKindleEmails.ToList();
                list.Add(new UserEmailMap { UserId = userId.ToString()!, Email = dto.Email });
                config.UserTargetKindleEmails = list.ToArray();
            }

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
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult> SendItem([FromRoute, Required] Guid itemId)
    {
        var userId = GetCurrentUserId();
        if (userId == null)
        {
            return Unauthorized();
        }

        var config = Plugin.Instance?.Configuration;
        if (config == null)
        {
            return StatusCode(StatusCodes.Status412PreconditionFailed, "Configuration missing.");
        }

        var userMap = config.UserTargetKindleEmails.FirstOrDefault(u => u.UserId == userId.ToString());
        var targetEmail = userMap?.Email;

        if (string.IsNullOrEmpty(targetEmail))
        {
            targetEmail = config.TargetKindleEmail;
        }

        if (string.IsNullOrEmpty(targetEmail))
        {
            return StatusCode(StatusCodes.Status412PreconditionFailed, "Target Kindle Email not set for user.");
        }

        try
        {
            var success = await _extractionService.SendItemToKindleAsync(itemId, userId.Value, CancellationToken.None).ConfigureAwait(false);

            if (success)
            {
                return NoContent();
            }

            return StatusCode(StatusCodes.Status500InternalServerError, "Failed to send item to Kindle.");
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(ex.Message);
        }
    }

    /// <summary>
    /// Uploads a book and sends it directly to the configured Kindle email.
    /// </summary>
    /// <param name="file">The file to send.</param>
    /// <returns>An <see cref="OkResult"/> on success, or a <see cref="BadRequestResult"/> on failure.</returns>
    [HttpPost("Upload")]
    [Consumes("multipart/form-data")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status412PreconditionFailed)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult> UploadBook([FromForm] IFormFile file)
    {
        var userId = GetCurrentUserId();
        if (userId == null)
        {
            return Unauthorized();
        }

        var config = Plugin.Instance?.Configuration;
        if (config == null)
        {
            return StatusCode(StatusCodes.Status412PreconditionFailed, "Configuration missing.");
        }

        var userMap = config.UserTargetKindleEmails.FirstOrDefault(u => u.UserId == userId.ToString());
        var targetEmail = userMap?.Email;

        if (string.IsNullOrEmpty(targetEmail))
        {
            targetEmail = config.TargetKindleEmail;
        }

        if (string.IsNullOrEmpty(targetEmail))
        {
            return StatusCode(StatusCodes.Status412PreconditionFailed, "Target Kindle Email not set for user.");
        }

        if (file == null || file.Length == 0)
        {
            return BadRequest("No file uploaded.");
        }

        var ext = System.IO.Path.GetExtension(file.FileName).ToLowerInvariant();
        if (ext != ".epub" && ext != ".mobi" && ext != ".azw3" && ext != ".pdf" && ext != ".cbz" && ext != ".cbr" && ext != ".prc" && ext != ".pdb")
        {
            return BadRequest("Unsupported file type.");
        }

        var tempFile = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "sendtokindle_upload_" + Guid.NewGuid().ToString() + ext);
        try
        {
            using (var stream = new System.IO.FileStream(tempFile, System.IO.FileMode.Create))
            {
                await file.CopyToAsync(stream).ConfigureAwait(false);
            }

            var title = System.IO.Path.GetFileNameWithoutExtension(file.FileName);

            var success = await _extractionService.SendFileToKindleAsync(tempFile, title, userId.Value, CancellationToken.None).ConfigureAwait(false);

            if (success)
            {
                return NoContent();
            }

            return StatusCode(StatusCodes.Status500InternalServerError, "Failed to send uploaded item to Kindle.");
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(ex.Message);
        }
        catch (Exception)
        {
            if (System.IO.File.Exists(tempFile))
            {
                try
                {
                    System.IO.File.Delete(tempFile);
                }
                catch
                {
                }
            }

            throw;
        }
    }

    /// <summary>
    /// Gets whether Shelfmark integration is enabled.
    /// </summary>
    /// <returns>True if enabled, else false.</returns>
    [HttpGet("ShelfmarkEnabled")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public ActionResult<bool> ShelfmarkEnabled()
    {
        var config = Plugin.Instance?.Configuration;
        return Ok(config != null && config.EnableShelfmark && !string.IsNullOrWhiteSpace(config.ShelfmarkUrl) && !string.IsNullOrWhiteSpace(config.ShelfmarkApiKey));
    }

    /// <summary>
    /// Proxies requests to a configured Shelfmark instance.
    /// </summary>
    /// <param name="path">The URL path in Shelfmark.</param>
    /// <returns>A proxy response from Shelfmark.</returns>
    [AcceptVerbs("GET", "POST", "PUT", "DELETE", Route = "ShelfmarkProxy/{*path}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<ActionResult> ShelfmarkProxy([FromRoute] string path)
    {
        var config = Plugin.Instance?.Configuration;
        if (config == null || !config.EnableShelfmark || string.IsNullOrWhiteSpace(config.ShelfmarkUrl) || string.IsNullOrWhiteSpace(config.ShelfmarkApiKey))
        {
            return BadRequest("Shelfmark integration is not enabled or configured.");
        }

        var targetUrl = $"{config.ShelfmarkUrl.TrimEnd('/')}/{path}{Request.QueryString}";

        using var client = new System.Net.Http.HttpClient();
        var proxyRequest = new System.Net.Http.HttpRequestMessage(new System.Net.Http.HttpMethod(Request.Method), targetUrl);
        proxyRequest.Headers.Add("X-Api-Key", config.ShelfmarkApiKey);

        if (Request.ContentLength > 0 && Request.Body.CanRead)
        {
            var streamContent = new System.Net.Http.StreamContent(Request.Body);
            if (Request.ContentType != null)
            {
                streamContent.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue(Request.ContentType);
            }

            proxyRequest.Content = streamContent;
        }

        var response = await client.SendAsync(proxyRequest).ConfigureAwait(false);
        var responseContent = await response.Content.ReadAsByteArrayAsync().ConfigureAwait(false);

        return File(responseContent, response.Content.Headers.ContentType?.ToString() ?? "application/json");
    }
}
