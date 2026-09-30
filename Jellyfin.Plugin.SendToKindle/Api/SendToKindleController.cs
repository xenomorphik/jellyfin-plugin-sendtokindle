using System;
using System.ComponentModel.DataAnnotations;
using System.Threading;
using System.Threading.Tasks;
using Jellyfin.Plugin.SendToKindle.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Jellyfin.Plugin.SendToKindle.Api
{
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

        /// <summary>
        /// Sends an item to the configured Kindle email.
        /// </summary>
        /// <param name="itemId">The ID of the item to send.</param>
        /// <returns>An <see cref="OkResult"/> on success, or a <see cref="BadRequestResult"/> on failure.</returns>
        [HttpPost("Send/{itemId}")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<ActionResult> SendItem([FromRoute, Required] Guid itemId)
        {
            var success = await _extractionService.SendItemToKindleAsync(itemId, CancellationToken.None).ConfigureAwait(false);
            
            if (success)
            {
                return NoContent();
            }

            return BadRequest("Failed to send item to Kindle. Check server logs for details.");
        }
    }
}
