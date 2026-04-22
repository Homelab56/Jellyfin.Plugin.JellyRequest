using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace Jellyfin.Plugin.JellyRequest.Services
{
    /// <summary>
    /// Service for interacting with Radarr API.
    /// </summary>
    public class RadarrService
    {
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly ILogger<RadarrService> _logger;

        /// <summary>
        /// Initializes a new instance of the <see cref="RadarrService"/> class.
        /// </summary>
        /// <param name="httpClientFactory">The HTTP client factory.</param>
        /// <param name="logger">The logger.</param>
        public RadarrService(IHttpClientFactory httpClientFactory, ILogger<RadarrService> logger)
        {
            _httpClientFactory = httpClientFactory;
            _logger = logger;
        }

        /// <summary>
        /// Test connection to Radarr.
        /// </summary>
        /// <param name="url">The Radarr URL.</param>
        /// <param name="apiKey">The Radarr API key.</param>
        /// <returns>True if connection is successful.</returns>
        public async Task<bool> TestConnectionAsync(string url, string apiKey)
        {
            try
            {
                var client = _httpClientFactory.CreateClient();
                var response = await GetAsync<List<RadarrMovie>>(client, $"{url}/api/v3/movie", apiKey);
                return response != null;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error testing Radarr connection");
                return false;
            }
        }

        /// <summary>
        /// Get all movies from Radarr.
        /// </summary>
        /// <param name="url">The Radarr URL.</param>
        /// <param name="apiKey">The Radarr API key.</param>
        /// <returns>List of movies.</returns>
        public async Task<List<RadarrMovie>> GetMoviesAsync(string url, string apiKey)
        {
            try
            {
                var client = _httpClientFactory.CreateClient();
                return await GetAsync<List<RadarrMovie>>(client, $"{url}/api/v3/movie", apiKey) ?? new List<RadarrMovie>();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting movies from Radarr");
                return new List<RadarrMovie>();
            }
        }

        /// <summary>
        /// Add a movie to Radarr.
        /// </summary>
        /// <param name="url">The Radarr URL.</param>
        /// <param name="apiKey">The Radarr API key.</param>
        /// <param name="tmdbId">The TMDB ID.</param>
        /// <param name="qualityProfileId">The quality profile ID.</param>
        /// <param name="rootFolderPath">The root folder path.</param>
        /// <param name="title">The movie title.</param>
        /// <param name="year">The release year.</param>
        /// <returns>The added movie, or null if failed.</returns>
        public async Task<RadarrMovie?> AddMovieAsync(string url, string apiKey, int tmdbId, int qualityProfileId, string rootFolderPath, string title, int year)
        {
            try
            {
                var client = _httpClientFactory.CreateClient();
                
                // First, lookup the movie to get the correct details
                var lookupResponse = await GetAsync<List<RadarrMovie>>(
                    client, 
                    $"{url}/api/v3/movie/lookup", 
                    apiKey, 
                    new Dictionary<string, string> { ["term"] = $"tmdb:{tmdbId}" });

                if (lookupResponse == null || !lookupResponse.Any())
                {
                    _logger.LogWarning("Movie not found in Radarr lookup: TMDB {TmdbId}", tmdbId);
                    return null;
                }

                var movie = lookupResponse.First();
                movie.QualityProfileId = qualityProfileId;
                movie.RootFolderPath = rootFolderPath;
                movie.Monitored = true;
                movie.Added = DateTime.UtcNow;

                var json = JsonSerializer.Serialize(movie, new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase });
                var content = new StringContent(json, System.Text.Encoding.UTF8, "application/json");

                var response = await client.PostAsync($"{url}/api/v3/movie?apikey={apiKey}", content);
                response.EnsureSuccessStatusCode();

                var responseJson = await response.Content.ReadAsStringAsync();
                return JsonSerializer.Deserialize<RadarrMovie>(responseJson, new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error adding movie to Radarr: TMDB {TmdbId}", tmdbId);
                return null;
            }
        }

        /// <summary>
        /// Delete a movie from Radarr.
        /// </summary>
        /// <param name="url">The Radarr URL.</param>
        /// <param name="apiKey">The Radarr API key.</param>
        /// <param name="movieId">The Radarr movie ID.</param>
        /// <returns>True if successful.</returns>
        public async Task<bool> DeleteMovieAsync(string url, string apiKey, int movieId)
        {
            try
            {
                var client = _httpClientFactory.CreateClient();
                var response = await client.DeleteAsync($"{url}/api/v3/movie/{movieId}?apikey={apiKey}&deleteFiles=true");
                return response.IsSuccessStatusCode;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting movie from Radarr: {MovieId}", movieId);
                return false;
            }
        }

        /// <summary>
        /// Get the queue from Radarr.
        /// </summary>
        /// <param name="url">The Radarr URL.</param>
        /// <param name="apiKey">The Radarr API key.</param>
        /// <returns>The queue items.</returns>
        public async Task<List<RadarrQueueItem>> GetQueueAsync(string url, string apiKey)
        {
            try
            {
                var client = _httpClientFactory.CreateClient();
                return await GetAsync<List<RadarrQueueItem>>(client, $"{url}/api/v3/queue", apiKey) ?? new List<RadarrQueueItem>();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting queue from Radarr");
                return new List<RadarrQueueItem>();
            }
        }

        /// <summary>
        /// Get quality profiles from Radarr.
        /// </summary>
        /// <param name="url">The Radarr URL.</param>
        /// <param name="apiKey">The Radarr API key.</param>
        /// <returns>List of quality profiles.</returns>
        public async Task<List<RadarrQualityProfile>> GetQualityProfilesAsync(string url, string apiKey)
        {
            try
            {
                var client = _httpClientFactory.CreateClient();
                return await GetAsync<List<RadarrQualityProfile>>(client, $"{url}/api/v3/qualityprofile", apiKey) ?? new List<RadarrQualityProfile>();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting quality profiles from Radarr");
                return new List<RadarrQualityProfile>();
            }
        }

        /// <summary>
        /// Get root folders from Radarr.
        /// </summary>
        /// <param name="url">The Radarr URL.</param>
        /// <param name="apiKey">The Radarr API key.</param>
        /// <returns>List of root folders.</returns>
        public async Task<List<RadarrRootFolder>> GetRootFoldersAsync(string url, string apiKey)
        {
            try
            {
                var client = _httpClientFactory.CreateClient();
                return await GetAsync<List<RadarrRootFolder>>(client, $"{url}/api/v3/rootfolder", apiKey) ?? new List<RadarrRootFolder>();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting root folders from Radarr");
                return new List<RadarrRootFolder>();
            }
        }

        private async Task<T?> GetAsync<T>(HttpClient client, string url, string apiKey, Dictionary<string, string>? parameters = null)
        {
            var fullUrl = $"{url}?apikey={apiKey}";
            if (parameters != null)
            {
                var queryString = string.Join("&", parameters.Select(p => $"{p.Key}={Uri.EscapeDataString(p.Value)}"));
                fullUrl += $"&{queryString}";
            }

            var response = await client.GetAsync(fullUrl);
            response.EnsureSuccessStatusCode();

            var json = await response.Content.ReadAsStringAsync();
            return JsonSerializer.Deserialize<T>(json, new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase });
        }
    }

    // Radarr Models
    public class RadarrMovie
    {
        public int Id { get; set; }
        public string Title { get; set; } = string.Empty;
        public int Year { get; set; }
        public int TmdbId { get; set; }
        public int QualityProfileId { get; set; }
        public string RootFolderPath { get; set; } = string.Empty;
        public bool Monitored { get; set; }
        public DateTime Added { get; set; }
        public string? Status { get; set; }
        public bool HasFile { get; set; }
        public string? PosterPath { get; set; }
        public string? BackdropPath { get; set; }
    }

    public class RadarrQueueItem
    {
        public int Id { get; set; }
        public string Title { get; set; } = string.Empty;
        public int TmdbId { get; set; }
        public string Status { get; set; } = string.Empty;
        public double? Sizeleft { get; set; }
        public double? Size { get; set; }
        public string? DownloadId { get; set; }
        public DateTime? Timeleft { get; set; }
    }

    public class RadarrQualityProfile
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
    }

    public class RadarrRootFolder
    {
        public int Id { get; set; }
        public string Path { get; set; } = string.Empty;
        public int? FreeSpace { get; set; }
    }
}
