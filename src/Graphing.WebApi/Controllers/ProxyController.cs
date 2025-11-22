using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System;

namespace Graphing.WebApi.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ProxyController : ControllerBase
    {
        private readonly IHttpClientFactory _httpClientFactory;

        public ProxyController(IHttpClientFactory httpClientFactory)
        {
            _httpClientFactory = httpClientFactory;
        }

        [AllowAnonymous]
        [HttpGet("image", Name = "Image")]
        public async Task<IActionResult> GetImageAsync([FromQuery] string url)
        {
            if (string.IsNullOrWhiteSpace(url))
                return BadRequest("Missing url parameter.");

            if (!Uri.TryCreate(url, UriKind.Absolute, out var uri))
                return BadRequest("Invalid url.");

            var http = _httpClientFactory.CreateClient("ProxyClient");

            HttpResponseMessage remoteResponse;
            try
            {
                remoteResponse = await http.GetAsync(uri, HttpCompletionOption.ResponseHeadersRead);
            }
            catch
            {
                return StatusCode(502, "Failed to fetch remote image.");
            }

            if (!remoteResponse.IsSuccessStatusCode)
                return StatusCode((int)remoteResponse.StatusCode);

            var bytes = await remoteResponse.Content.ReadAsByteArrayAsync();
            var contentType = remoteResponse.Content.Headers.ContentType?.MediaType
                              ?? "application/octet-stream";

            var result = File(bytes, contentType);

            // ---- HEADER PRESERVATION ----
            if (remoteResponse.Headers.CacheControl != null)
                Response.Headers["Cache-Control"] = remoteResponse.Headers.CacheControl.ToString();

            if (remoteResponse.Content.Headers.Expires.HasValue)
                Response.Headers["Expires"] =
                    remoteResponse.Content.Headers.Expires.Value.ToString("R");

            if (remoteResponse.Headers.ETag != null)
                Response.Headers["ETag"] = remoteResponse.Headers.ETag.ToString();

            if (remoteResponse.Content.Headers.LastModified.HasValue)
                Response.Headers["Last-Modified"] =
                    remoteResponse.Content.Headers.LastModified.Value.ToString("R");

            // Add CORS support so images can load in client canvas
            Response.Headers["Access-Control-Allow-Origin"] = "*";

            return result;
        }
    }
}
