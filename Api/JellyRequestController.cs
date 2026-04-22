using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Jellyfin.Plugin.JellyRequest.Api.Dtos;
using Jellyfin.Plugin.JellyRequest.Configuration;
using Jellyfin.Plugin.JellyRequest.Services;
using MediaBrowser.Controller.Library;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Jellyfin.Plugin.JellyRequest.Api
{
    /// <summary>
    /// API controller for JellyRequest plugin.
    /// </summary>
    [ApiController]
    [Route("JellyRequest")]
    [Authorize]
    public class JellyRequestController : ControllerBase
    {
        private readonly TmdbService _tmdbService;
        private readonly RequestService _requestService;
        private readonly LibraryMatchService _libraryMatchService;
        private readonly RadarrService _radarrService;
        private readonly SonarrService _sonarrService;
        private readonly PluginConfiguration _config;
        private readonly IUserManager _userManager;

        /// <summary>
        /// Initializes a new instance of the <see cref="JellyRequestController"/> class.
        /// </summary>
        /// <param name="tmdbService">The TMDB service.</param>
        /// <param name="requestService">The request service.</param>
        /// <param name="libraryMatchService">The library match service.</param>
        /// <param name="config">The plugin configuration.</param>
        /// <param name="userManager">The user manager.</param>
        public JellyRequestController(
            TmdbService tmdbService,
            RequestService requestService,
            LibraryMatchService libraryMatchService,
            RadarrService radarrService,
            SonarrService sonarrService,
            PluginConfiguration config,
            IUserManager userManager)
        {
            _tmdbService = tmdbService;
            _requestService = requestService;
            _libraryMatchService = libraryMatchService;
            _radarrService = radarrService;
            _sonarrService = sonarrService;
            _config = config;
            _userManager = userManager;
        }

        /// <summary>
        /// Search for movies and TV shows.
        /// </summary>
        /// <param name="query">The search query.</param>
        /// <param name="type">The media type (movie, tv, or all).</param>
        /// <param name="page">The page number.</param>
        /// <returns>The search results.</returns>
        [HttpGet("search")]
        public async Task<ActionResult<SearchResultDto>> Search([FromQuery] string query, [FromQuery] string type = "all", [FromQuery] int page = 1)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(_config.TmdbApiKey))
                {
                    return BadRequest("TMDB API key not configured");
                }

                if (string.IsNullOrWhiteSpace(query))
                {
                    return BadRequest("Search query is required");
                }

                var result = await _tmdbService.SearchMultiAsync(_config.TmdbApiKey, query, _config.TmdbLanguage, _config.IncludeAdultContent);

                // Filter by type if specified
                if (type == "movie")
                {
                    result.TvShows.Clear();
                }
                else if (type == "tv")
                {
                    result.Movies.Clear();
                }

                // Check library availability for all items
                await UpdateLibraryAvailability(result.Movies);
                await UpdateLibraryAvailability(result.TvShows);

                return Ok(result);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Internal server error: {ex.Message}");
            }
        }

        /// <summary>
        /// Get trending movies or TV shows.
        /// </summary>
        /// <param name="type">The media type (movie or tv).</param>
        /// <param name="page">The page number.</param>
        /// <returns>The trending results.</returns>
        [HttpGet("trending")]
        public async Task<ActionResult<List<SearchResultDto>>> GetTrending([FromQuery] string type = "movie", [FromQuery] int page = 1)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(_config.TmdbApiKey))
                {
                    return BadRequest("TMDB API key not configured");
                }

                var results = await _tmdbService.GetTrendingAsync(_config.TmdbApiKey, type, "week", _config.TmdbLanguage);
                
                // Check library availability
                await UpdateLibraryAvailability(results);

                return Ok(results);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Internal server error: {ex.Message}");
            }
        }

        /// <summary>
        /// Get detailed information about a movie or TV show.
        /// </summary>
        /// <param name="tmdbId">The TMDB ID.</param>
        /// <param name="type">The media type (movie or tv).</param>
        /// <returns>The detailed information.</returns>
        [HttpGet("detail")]
        public async Task<ActionResult<TmdbItemDto>> GetDetail([FromQuery] int tmdbId, [FromQuery] string type)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(_config.TmdbApiKey))
                {
                    return BadRequest("TMDB API key not configured");
                }

                var detail = await _tmdbService.GetDetailAsync(_config.TmdbApiKey, tmdbId, type, _config.TmdbLanguage);
                if (detail == null)
                {
                    return NotFound($"Item not found: {type} {tmdbId}");
                }

                return Ok(detail);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Internal server error: {ex.Message}");
            }
        }

        /// <summary>
        /// Create a new content request.
        /// </summary>
        /// <param name="request">The request data.</param>
        /// <returns>The created request.</returns>
        [HttpPost("request")]
        public async Task<ActionResult<RequestDto>> CreateRequest([FromBody] CreateRequestDto request)
        {
            try
            {
                // Validate configuration
                if (string.IsNullOrWhiteSpace(_config.TmdbApiKey))
                {
                    return BadRequest("TMDB API key not configured");
                }

                if (request.MediaType == "movie" && (string.IsNullOrWhiteSpace(_config.RadarrUrl) || string.IsNullOrWhiteSpace(_config.RadarrApiKey)))
                {
                    return BadRequest("Radarr configuration not found");
                }

                if (request.MediaType == "tv" && (string.IsNullOrWhiteSpace(_config.SonarrUrl) || string.IsNullOrWhiteSpace(_config.SonarrApiKey)))
                {
                    return BadRequest("Sonarr configuration not found");
                }

                // Check user permissions
                var user = _userManager.GetUserById(Guid.Parse(request.UserId));
                if (user == null)
                {
                    return Unauthorized("User not found");
                }

                // TODO: Fix permission check for Jellyfin 10.11.8.0
                // if (!_config.AllowRegularUserRequests && !user.Policy.IsAdministrator)
                {
                    return Forbid("Regular users are not allowed to make requests");
                }

                // Check user request limit
                var userRequests = await _requestService.GetUserRequestsAsync(request.UserId);
                if (userRequests.Count >= _config.MaxRequestsPerUser)
                {
                    return BadRequest($"Request limit exceeded ({_config.MaxRequestsPerUser})");
                }

                var createdRequest = await _requestService.CreateRequestAsync(request.TmdbId, request.MediaType, request.UserId);
                if (createdRequest == null)
                {
                    return BadRequest("Failed to create request");
                }

                return Ok(createdRequest);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Internal server error: {ex.Message}");
            }
        }

        /// <summary>
        /// Get requests for the current user.
        /// </summary>
        /// <param name="userId">The user ID.</param>
        /// <returns>The user's requests.</returns>
        [HttpGet("requests")]
        public async Task<ActionResult<List<RequestDto>>> GetUserRequests([FromQuery] string userId)
        {
            try
            {
                var requests = await _requestService.GetUserRequestsAsync(userId);
                return Ok(requests);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Internal server error: {ex.Message}");
            }
        }

        /// <summary>
        /// Delete a request.
        /// </summary>
        /// <param name="id">The request ID.</param>
        /// <param name="userId">The user ID.</param>
        /// <returns>Success status.</returns>
        [HttpDelete("request/{id}")]
        public async Task<ActionResult> DeleteRequest(int id, [FromQuery] string userId)
        {
            try
            {
                var success = await _requestService.DeleteRequestAsync(id, userId);
                if (!success)
                {
                    return NotFound("Request not found or access denied");
                }

                return Ok();
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Internal server error: {ex.Message}");
            }
        }

        /// <summary>
        /// Get request status for a specific TMDB item.
        /// </summary>
        /// <param name="tmdbId">The TMDB ID.</param>
        /// <param name="type">The media type.</param>
        /// <returns>The status information.</returns>
        [HttpGet("status/{tmdbId}")]
        public async Task<ActionResult<RequestStatusDto>> GetRequestStatus(int tmdbId, [FromQuery] string type)
        {
            try
            {
                var status = await _requestService.GetRequestStatusAsync(tmdbId, type);
                if (status == null)
                {
                    return NotFound("Status not found");
                }

                return Ok(status);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Internal server error: {ex.Message}");
            }
        }

        /// <summary>
        /// Get popular movies or TV shows.
        /// </summary>
        /// <param name="type">The media type (movie or tv).</param>
        /// <param name="page">The page number.</param>
        /// <returns>The popular results.</returns>
        [HttpGet("popular")]
        public async Task<ActionResult<List<TmdbItemDto>>> GetPopular([FromQuery] string type = "movie", [FromQuery] int page = 1)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(_config.TmdbApiKey))
                {
                    return BadRequest("TMDB API key not configured");
                }

                var results = await _tmdbService.GetPopularAsync(_config.TmdbApiKey, type, page, _config.TmdbLanguage);
                await UpdateLibraryAvailability(results);
                return Ok(results);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Internal server error: {ex.Message}");
            }
        }

        /// <summary>
        /// Get top rated movies or TV shows.
        /// </summary>
        /// <param name="type">The media type (movie or tv).</param>
        /// <param name="page">The page number.</param>
        /// <returns>The top rated results.</returns>
        [HttpGet("toprated")]
        public async Task<ActionResult<List<TmdbItemDto>>> GetTopRated([FromQuery] string type = "movie", [FromQuery] int page = 1)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(_config.TmdbApiKey))
                {
                    return BadRequest("TMDB API key not configured");
                }

                var results = await _tmdbService.GetTopRatedAsync(_config.TmdbApiKey, type, page, _config.TmdbLanguage);
                await UpdateLibraryAvailability(results);
                return Ok(results);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Internal server error: {ex.Message}");
            }
        }

        /// <summary>
        /// Get now playing movies.
        /// </summary>
        /// <param name="page">The page number.</param>
        /// <returns>The now playing results.</returns>
        [HttpGet("nowplaying")]
        public async Task<ActionResult<List<TmdbItemDto>>> GetNowPlaying([FromQuery] int page = 1)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(_config.TmdbApiKey))
                {
                    return BadRequest("TMDB API key not configured");
                }

                var results = await _tmdbService.GetNowPlayingAsync(_config.TmdbApiKey, page, _config.TmdbLanguage);
                await UpdateLibraryAvailability(results);
                return Ok(results);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Internal server error: {ex.Message}");
            }
        }

        /// <summary>
        /// Get Radarr quality profiles.
        /// </summary>
        /// <returns>The quality profiles.</returns>
        [HttpGet("radarr/qualityprofiles")]
        public async Task<ActionResult<List<RadarrQualityProfile>>> GetRadarrQualityProfiles()
        {
            try
            {
                if (string.IsNullOrWhiteSpace(_config.RadarrUrl) || string.IsNullOrWhiteSpace(_config.RadarrApiKey))
                {
                    return BadRequest("Radarr configuration not found");
                }

                var profiles = await _radarrService.GetQualityProfilesAsync(_config.RadarrUrl, _config.RadarrApiKey);
                return Ok(profiles);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Internal server error: {ex.Message}");
            }
        }

        /// <summary>
        /// Get Radarr root folders.
        /// </summary>
        /// <returns>The root folders.</returns>
        [HttpGet("radarr/rootfolders")]
        public async Task<ActionResult<List<RadarrRootFolder>>> GetRadarrRootFolders()
        {
            try
            {
                if (string.IsNullOrWhiteSpace(_config.RadarrUrl) || string.IsNullOrWhiteSpace(_config.RadarrApiKey))
                {
                    return BadRequest("Radarr configuration not found");
                }

                var folders = await _radarrService.GetRootFoldersAsync(_config.RadarrUrl, _config.RadarrApiKey);
                return Ok(folders);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Internal server error: {ex.Message}");
            }
        }

        /// <summary>
        /// Get Sonarr quality profiles.
        /// </summary>
        /// <returns>The quality profiles.</returns>
        [HttpGet("sonarr/qualityprofiles")]
        public async Task<ActionResult<List<SonarrQualityProfile>>> GetSonarrQualityProfiles()
        {
            try
            {
                if (string.IsNullOrWhiteSpace(_config.SonarrUrl) || string.IsNullOrWhiteSpace(_config.SonarrApiKey))
                {
                    return BadRequest("Sonarr configuration not found");
                }

                var profiles = await _sonarrService.GetQualityProfilesAsync(_config.SonarrUrl, _config.SonarrApiKey);
                return Ok(profiles);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Internal server error: {ex.Message}");
            }
        }

        /// <summary>
        /// Get Sonarr root folders.
        /// </summary>
        /// <returns>The root folders.</returns>
        [HttpGet("sonarr/rootfolders")]
        public async Task<ActionResult<List<SonarrRootFolder>>> GetSonarrRootFolders()
        {
            try
            {
                if (string.IsNullOrWhiteSpace(_config.SonarrUrl) || string.IsNullOrWhiteSpace(_config.SonarrApiKey))
                {
                    return BadRequest("Sonarr configuration not found");
                }

                var folders = await _sonarrService.GetRootFoldersAsync(_config.SonarrUrl, _config.SonarrApiKey);
                return Ok(folders);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Internal server error: {ex.Message}");
            }
        }

        private async Task UpdateLibraryAvailability(List<TmdbItemDto> items)
        {
            var tmdbIds = items.ToDictionary(i => i.Id, i => i.MediaType);
            var availability = await _libraryMatchService.CheckMultipleItemsExistAsync(tmdbIds);

            foreach (var item in items)
            {
                item.IsInLibrary = availability.ContainsKey(item.Id) && availability[item.Id];
            }
        }
    }

    /// <summary>
    /// Data transfer object for creating requests.
    /// </summary>
    public class CreateRequestDto
    {
        /// <summary>
        /// Gets or sets the TMDB ID.
        /// </summary>
        public int TmdbId { get; set; }

        /// <summary>
        /// Gets or sets the media type.
        /// </summary>
        public string MediaType { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the title.
        /// </summary>
        public string Title { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the year.
        /// </summary>
        public int Year { get; set; }

        /// <summary>
        /// Gets or sets the poster path.
        /// </summary>
        public string? PosterPath { get; set; }

        /// <summary>
        /// Gets or sets the user ID.
        /// </summary>
        public string UserId { get; set; } = string.Empty;
    }
}
