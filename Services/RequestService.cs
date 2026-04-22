using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Jellyfin.Plugin.JellyRequest.Api.Dtos;
using Jellyfin.Plugin.JellyRequest.Configuration;
using Jellyfin.Plugin.JellyRequest.Data;
using Microsoft.Extensions.Logging;
using MediaBrowser.Controller.Library;
using MediaBrowser.Model.Entities;

namespace Jellyfin.Plugin.JellyRequest.Services
{
    /// <summary>
    /// Service for managing content requests.
    /// </summary>
    public class RequestService
    {
        private readonly RequestRepository _repository;
        private readonly TmdbService _tmdbService;
        private readonly RadarrService _radarrService;
        private readonly SonarrService _sonarrService;
        private readonly LibraryMatchService _libraryMatchService;
        private readonly PluginConfiguration _config;
        private readonly ILogger<RequestService> _logger;

        /// <summary>
        /// Initializes a new instance of the <see cref="RequestService"/> class.
        /// </summary>
        /// <param name="repository">The request repository.</param>
        /// <param name="tmdbService">The TMDB service.</param>
        /// <param name="radarrService">The Radarr service.</param>
        /// <param name="sonarrService">The Sonarr service.</param>
        /// <param name="libraryMatchService">The library match service.</param>
        /// <param name="config">The plugin configuration.</param>
        /// <param name="logger">The logger.</param>
        public RequestService(
            RequestRepository repository,
            TmdbService tmdbService,
            RadarrService radarrService,
            SonarrService sonarrService,
            LibraryMatchService libraryMatchService,
            PluginConfiguration config,
            ILogger<RequestService> logger)
        {
            _repository = repository;
            _tmdbService = tmdbService;
            _radarrService = radarrService;
            _sonarrService = sonarrService;
            _libraryMatchService = libraryMatchService;
            _config = config;
            _logger = logger;
        }

        /// <summary>
        /// Create a new content request.
        /// </summary>
        /// <param name="tmdbId">The TMDB ID.</param>
        /// <param name="mediaType">The media type (movie or tv).</param>
        /// <param name="userId">The user ID.</param>
        /// <returns>The created request, or null if failed.</returns>
        public async Task<RequestDto?> CreateRequestAsync(int tmdbId, string mediaType, string userId)
        {
            try
            {
                // Check if item already exists in library
                var existsInLibrary = mediaType == "movie" 
                    ? await _libraryMatchService.MovieExistsAsync(tmdbId)
                    : await _libraryMatchService.SeriesExistsAsync(tmdbId);

                if (existsInLibrary)
                {
                    _logger.LogWarning("Item already exists in library: {MediaType} {TmdbId}", mediaType, tmdbId);
                    return null;
                }

                // Check if request already exists
                var existingRequests = await _repository.GetRequestsByTmdbIdAsync(tmdbId, mediaType);
                if (existingRequests.Any())
                {
                    _logger.LogWarning("Request already exists: {MediaType} {TmdbId}", mediaType, tmdbId);
                    return existingRequests.First();
                }

                // Get TMDB details
                var detail = await _tmdbService.GetDetailAsync(_config.TmdbApiKey, tmdbId, mediaType, _config.TmdbLanguage);
                if (detail == null)
                {
                    _logger.LogError("Failed to get TMDB details: {MediaType} {TmdbId}", mediaType, tmdbId);
                    return null;
                }

                // Create request record
                var request = new RequestDto
                {
                    TmdbId = tmdbId,
                    MediaType = mediaType,
                    Title = detail.Title,
                    Year = int.Parse(detail.ReleaseDate?.Split('-').FirstOrDefault() ?? "0"),
                    UserId = userId,
                    Status = "Requested",
                    PosterPath = detail.PosterPath,
                    BackdropPath = detail.BackdropPath
                };

                request = await _repository.CreateRequestAsync(request);

                // Send to Radarr/Sonarr
                if (mediaType == "movie")
                {
                    await SendToRadarrAsync(request);
                }
                else if (mediaType == "tv")
                {
                    await SendToSonarrAsync(request);
                }

                return request;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating request: {MediaType} {TmdbId} for user {UserId}", mediaType, tmdbId, userId);
                return null;
            }
        }

        /// <summary>
        /// Get requests for a specific user.
        /// </summary>
        /// <param name="userId">The user ID.</param>
        /// <returns>List of user's requests.</returns>
        public async Task<List<RequestDto>> GetUserRequestsAsync(string userId)
        {
            try
            {
                var requests = await _repository.GetRequestsByUserAsync(userId);
                
                // Update status for requests that might be available now
                foreach (var request in requests)
                {
                    if (request.Status != "Available")
                    {
                        var existsInLibrary = request.MediaType == "movie"
                            ? await _libraryMatchService.MovieExistsAsync(request.TmdbId)
                            : await _libraryMatchService.SeriesExistsAsync(request.TmdbId);

                        if (existsInLibrary)
                        {
                            await _repository.UpdateRequestStatusAsync(request.Id, "Available");
                            request.Status = "Available";
                        }
                    }
                }

                return requests;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting user requests: {UserId}", userId);
                return new List<RequestDto>();
            }
        }

        /// <summary>
        /// Delete a request.
        /// </summary>
        /// <param name="requestId">The request ID.</param>
        /// <param name="userId">The user ID making the request.</param>
        /// <returns>True if successful.</returns>
        public async Task<bool> DeleteRequestAsync(int requestId, string userId)
        {
            try
            {
                var request = await _repository.GetRequestAsync(requestId);
                if (request == null || request.UserId != userId)
                {
                    return false;
                }

                // Delete from Radarr/Sonarr if applicable
                if (request.MediaType == "movie" && request.RadarrId.HasValue)
                {
                    await _radarrService.DeleteMovieAsync(_config.RadarrUrl, _config.RadarrApiKey, request.RadarrId.Value);
                }
                else if (request.MediaType == "tv" && request.SonarrId.HasValue)
                {
                    await _sonarrService.DeleteSeriesAsync(_config.SonarrUrl, _config.SonarrApiKey, request.SonarrId.Value);
                }

                return await _repository.DeleteRequestAsync(requestId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting request: {RequestId}", requestId);
                return false;
            }
        }

        /// <summary>
        /// Update request statuses by checking Radarr/Sonarr queues.
        /// </summary>
        public async Task UpdateRequestStatusesAsync()
        {
            try
            {
                var pendingRequests = await _repository.GetPendingRequestsAsync();
                
                foreach (var request in pendingRequests)
                {
                    if (request.MediaType == "movie" && request.RadarrId.HasValue)
                    {
                        await UpdateMovieRequestStatusAsync(request);
                    }
                    else if (request.MediaType == "tv" && request.SonarrId.HasValue)
                    {
                        await UpdateSeriesRequestStatusAsync(request);
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating request statuses");
            }
        }

        /// <summary>
        /// Get request status for a specific TMDB item.
        /// </summary>
        /// <param name="tmdbId">The TMDB ID.</param>
        /// <param name="mediaType">The media type.</param>
        /// <returns>The status DTO.</returns>
        public async Task<StatusDto?> GetRequestStatusAsync(int tmdbId, string mediaType)
        {
            try
            {
                var requests = await _repository.GetRequestsByTmdbIdAsync(tmdbId, mediaType);
                if (!requests.Any())
                {
                    return new StatusDto
                    {
                        TmdbId = tmdbId,
                        MediaType = mediaType,
                        Status = "NotRequested",
                        IsInLibrary = mediaType == "movie"
                            ? await _libraryMatchService.MovieExistsAsync(tmdbId)
                            : await _libraryMatchService.SeriesExistsAsync(tmdbId)
                    };
                }

                var request = requests.First();
                var isInLibrary = mediaType == "movie"
                    ? await _libraryMatchService.MovieExistsAsync(tmdbId)
                    : await _libraryMatchService.SeriesExistsAsync(tmdbId);

                if (isInLibrary && request.Status != "Available")
                {
                    await _repository.UpdateRequestStatusAsync(request.Id, "Available");
                    request.Status = "Available";
                }

                return new StatusDto
                {
                    TmdbId = tmdbId,
                    MediaType = mediaType,
                    Status = request.Status,
                    IsInLibrary = isInLibrary,
                    RequestDate = request.RequestDate,
                    LastUpdated = request.LastUpdated
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting request status: {MediaType} {TmdbId}", mediaType, tmdbId);
                return null;
            }
        }

        private async Task SendToRadarrAsync(RequestDto request)
        {
            try
            {
                var radarrMovie = await _radarrService.AddMovieAsync(
                    _config.RadarrUrl,
                    _config.RadarrApiKey,
                    request.TmdbId,
                    _config.RadarrQualityProfileId,
                    _config.RadarrRootFolderId.ToString(),
                    request.Title,
                    request.Year);

                if (radarrMovie != null)
                {
                    await _repository.UpdateRequestStatusAsync(request.Id, "Requested", radarrMovie.Id);
                    request.RadarrId = radarrMovie.Id;
                    _logger.LogInformation("Sent movie request to Radarr: {Title} ({TmdbId})", request.Title, request.TmdbId);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error sending movie request to Radarr: {TmdbId}", request.TmdbId);
            }
        }

        private async Task SendToSonarrAsync(RequestDto request)
        {
            try
            {
                var sonarrSeries = await _sonarrService.AddSeriesAsync(
                    _config.SonarrUrl,
                    _config.SonarrApiKey,
                    request.TmdbId,
                    _config.SonarrQualityProfileId,
                    _config.SonarrRootFolderId.ToString(),
                    request.Title,
                    request.Year);

                if (sonarrSeries != null)
                {
                    await _repository.UpdateRequestStatusAsync(request.Id, "Requested", null, sonarrSeries.Id);
                    request.SonarrId = sonarrSeries.Id;
                    _logger.LogInformation("Sent series request to Sonarr: {Title} ({TmdbId})", request.Title, request.TmdbId);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error sending series request to Sonarr: {TmdbId}", request.TmdbId);
            }
        }

        private async Task UpdateMovieRequestStatusAsync(RequestDto request)
        {
            try
            {
                if (!request.RadarrId.HasValue) return;

                var queue = await _radarrService.GetQueueAsync(_config.RadarrUrl, _config.RadarrApiKey);
                var queueItem = queue.FirstOrDefault(q => q.TmdbId == request.TmdbId);

                if (queueItem != null)
                {
                    var progress = queueItem.Size.HasValue && queueItem.Sizeleft.HasValue
                        ? (int)((1 - (queueItem.Sizeleft.Value / queueItem.Size.Value)) * 100)
                        : 0;

                    var status = progress > 0 && progress < 100 ? "Downloading" : "Requested";
                    await _repository.UpdateRequestStatusAsync(request.Id, status);
                }
                else
                {
                    // Check if movie is now available in Radarr
                    var movies = await _radarrService.GetMoviesAsync(_config.RadarrUrl, _config.RadarrApiKey);
                    var movie = movies.FirstOrDefault(m => m.TmdbId == request.TmdbId && m.HasFile);

                    if (movie != null)
                    {
                        await _repository.UpdateRequestStatusAsync(request.Id, "Completed");
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating movie request status: {TmdbId}", request.TmdbId);
            }
        }

        private async Task UpdateSeriesRequestStatusAsync(RequestDto request)
        {
            try
            {
                if (!request.SonarrId.HasValue) return;

                var queue = await _sonarrService.GetQueueAsync(_config.SonarrUrl, _config.SonarrApiKey);
                var queueItem = queue.FirstOrDefault(q => q.TmdbId == request.TmdbId);

                if (queueItem != null)
                {
                    var progress = queueItem.Size.HasValue && queueItem.Sizeleft.HasValue
                        ? (int)((1 - (queueItem.Sizeleft.Value / queueItem.Size.Value)) * 100)
                        : 0;

                    var status = progress > 0 && progress < 100 ? "Downloading" : "Requested";
                    await _repository.UpdateRequestStatusAsync(request.Id, status);
                }
                else
                {
                    // Check if series is now available in Sonarr
                    var series = await _sonarrService.GetSeriesAsync(_config.SonarrUrl, _config.SonarrApiKey);
                    var show = series.FirstOrDefault(s => s.TmdbId == request.TmdbId && s.HasFile);

                    if (show != null)
                    {
                        await _repository.UpdateRequestStatusAsync(request.Id, "Completed");
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating series request status: {TmdbId}", request.TmdbId);
            }
        }
    }
}
