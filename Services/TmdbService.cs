using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;
using Jellyfin.Plugin.JellyRequest.Api.Dtos;
using Microsoft.Extensions.Logging;

namespace Jellyfin.Plugin.JellyRequest.Services
{
    /// <summary>
    /// Service for interacting with TMDB API.
    /// </summary>
    public class TmdbService
    {
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly ILogger<TmdbService> _logger;
        private const string BaseUrl = "https://api.themoviedb.org/3";

        /// <summary>
        /// Initializes a new instance of the <see cref="TmdbService"/> class.
        /// </summary>
        /// <param name="httpClientFactory">The HTTP client factory.</param>
        /// <param name="logger">The logger.</param>
        public TmdbService(IHttpClientFactory httpClientFactory, ILogger<TmdbService> logger)
        {
            _httpClientFactory = httpClientFactory;
            _logger = logger;
        }

        /// <summary>
        /// Search for movies and TV shows using multi search.
        /// </summary>
        /// <param name="apiKey">The TMDB API key.</param>
        /// <param name="query">The search query.</param>
        /// <param name="language">The language code.</param>
        /// <param name="includeAdult">Whether to include adult content.</param>
        /// <returns>The search results.</returns>
        public async Task<SearchResultDto> SearchMultiAsync(string apiKey, string query, string language = "en-US", bool includeAdult = false)
        {
            try
            {
                var client = _httpClientFactory.CreateClient();
                var response = await GetAsync<TmdbSearchResponse>(
                    client,
                    $"{BaseUrl}/search/multi",
                    new Dictionary<string, string>
                    {
                        ["api_key"] = apiKey,
                        ["query"] = query,
                        ["language"] = language,
                        ["include_adult"] = includeAdult.ToString()
                    });

                var movies = new List<TmdbItemDto>();
                var tvShows = new List<TmdbItemDto>();

                foreach (var item in response.Results)
                {
                    if (item.MediaType == "movie")
                    {
                        movies.Add(new TmdbItemDto
                        {
                            Id = item.Id,
                            Title = item.Title ?? item.Name ?? "Unknown",
                            Overview = item.Overview ?? "",
                            PosterPath = item.PosterPath,
                            BackdropPath = item.BackdropPath,
                            ReleaseDate = item.ReleaseDate ?? item.FirstAirDate,
                            VoteAverage = item.VoteAverage,
                            MediaType = "movie"
                        });
                    }
                    else if (item.MediaType == "tv")
                    {
                        tvShows.Add(new TmdbItemDto
                        {
                            Id = item.Id,
                            Title = item.Title ?? item.Name ?? "Unknown",
                            Overview = item.Overview ?? "",
                            PosterPath = item.PosterPath,
                            BackdropPath = item.BackdropPath,
                            ReleaseDate = item.ReleaseDate ?? item.FirstAirDate,
                            VoteAverage = item.VoteAverage,
                            MediaType = "tv"
                        });
                    }
                }

                return new SearchResultDto { Movies = movies, TvShows = tvShows };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error searching TMDB for query: {Query}", query);
                return new SearchResultDto { Movies = new List<TmdbItemDto>(), TvShows = new List<TmdbItemDto>() };
            }
        }

        /// <summary>
        /// Get trending movies or TV shows.
        /// </summary>
        /// <param name="apiKey">The TMDB API key.</param>
        /// <param name="mediaType">The media type (movie or tv).</param>
        /// <param name="timeWindow">The time window (day or week).</param>
        /// <param name="language">The language code.</param>
        /// <returns>The trending results.</returns>
        public async Task<List<TmdbItemDto>> GetTrendingAsync(string apiKey, string mediaType, string timeWindow = "week", string language = "en-US")
        {
            try
            {
                var client = _httpClientFactory.CreateClient();
                var response = await GetAsync<TmdbSearchResponse>(
                    client,
                    $"{BaseUrl}/trending/{mediaType}/{timeWindow}",
                    new Dictionary<string, string>
                    {
                        ["api_key"] = apiKey,
                        ["language"] = language
                    });

                return response.Results.Select(r => new TmdbItemDto
                {
                    Id = r.Id,
                    Title = r.Title ?? r.Name ?? "Unknown",
                    Overview = r.Overview ?? "",
                    PosterPath = r.PosterPath,
                    BackdropPath = r.BackdropPath,
                    ReleaseDate = r.ReleaseDate ?? r.FirstAirDate,
                    VoteAverage = r.VoteAverage,
                    MediaType = mediaType
                }).ToList();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting trending {MediaType} from TMDB", mediaType);
                return new List<TmdbItemDto>();
            }
        }

        /// <summary>
        /// Get popular movies or TV shows.
        /// </summary>
        /// <param name="apiKey">The TMDB API key.</param>
        /// <param name="mediaType">The media type (movie or tv).</param>
        /// <param name="page">The page number.</param>
        /// <param name="language">The language code.</param>
        /// <returns>The popular results.</returns>
        public async Task<List<TmdbItemDto>> GetPopularAsync(string apiKey, string mediaType, int page = 1, string language = "en-US")
        {
            try
            {
                var client = _httpClientFactory.CreateClient();
                var endpoint = mediaType == "movie" ? "movie/popular" : "tv/popular";
                var response = await GetAsync<TmdbSearchResponse>(
                    client,
                    $"{BaseUrl}/{endpoint}",
                    new Dictionary<string, string>
                    {
                        ["api_key"] = apiKey,
                        ["language"] = language,
                        ["page"] = page.ToString()
                    });

                return response.Results.Select(r => new TmdbItemDto
                {
                    Id = r.Id,
                    Title = r.Title ?? r.Name ?? "Unknown",
                    Overview = r.Overview ?? "",
                    PosterPath = r.PosterPath,
                    BackdropPath = r.BackdropPath,
                    ReleaseDate = r.ReleaseDate ?? r.FirstAirDate,
                    VoteAverage = r.VoteAverage,
                    MediaType = mediaType
                }).ToList();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting popular {MediaType} from TMDB", mediaType);
                return new List<TmdbItemDto>();
            }
        }

        /// <summary>
        /// Get top rated movies or TV shows.
        /// </summary>
        /// <param name="apiKey">The TMDB API key.</param>
        /// <param name="mediaType">The media type (movie or tv).</param>
        /// <param name="page">The page number.</param>
        /// <param name="language">The language code.</param>
        /// <returns>The top rated results.</returns>
        public async Task<List<TmdbItemDto>> GetTopRatedAsync(string apiKey, string mediaType, int page = 1, string language = "en-US")
        {
            try
            {
                var client = _httpClientFactory.CreateClient();
                var endpoint = mediaType == "movie" ? "movie/top_rated" : "tv/top_rated";
                var response = await GetAsync<TmdbSearchResponse>(
                    client,
                    $"{BaseUrl}/{endpoint}",
                    new Dictionary<string, string>
                    {
                        ["api_key"] = apiKey,
                        ["language"] = language,
                        ["page"] = page.ToString()
                    });

                return response.Results.Select(r => new TmdbItemDto
                {
                    Id = r.Id,
                    Title = r.Title ?? r.Name ?? "Unknown",
                    Overview = r.Overview ?? "",
                    PosterPath = r.PosterPath,
                    BackdropPath = r.BackdropPath,
                    ReleaseDate = r.ReleaseDate ?? r.FirstAirDate,
                    VoteAverage = r.VoteAverage,
                    MediaType = mediaType
                }).ToList();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting top rated {MediaType} from TMDB", mediaType);
                return new List<TmdbItemDto>();
            }
        }

        /// <summary>
        /// Get now playing movies.
        /// </summary>
        /// <param name="apiKey">The TMDB API key.</param>
        /// <param name="page">The page number.</param>
        /// <param name="language">The language code.</param>
        /// <returns>The now playing results.</returns>
        public async Task<List<TmdbItemDto>> GetNowPlayingAsync(string apiKey, int page = 1, string language = "en-US")
        {
            try
            {
                var client = _httpClientFactory.CreateClient();
                var response = await GetAsync<TmdbSearchResponse>(
                    client,
                    $"{BaseUrl}/movie/now_playing",
                    new Dictionary<string, string>
                    {
                        ["api_key"] = apiKey,
                        ["language"] = language,
                        ["page"] = page.ToString()
                    });

                return response.Results.Select(r => new TmdbItemDto
                {
                    Id = r.Id,
                    Title = r.Title ?? r.Name ?? "Unknown",
                    Overview = r.Overview ?? "",
                    PosterPath = r.PosterPath,
                    BackdropPath = r.BackdropPath,
                    ReleaseDate = r.ReleaseDate ?? r.FirstAirDate,
                    VoteAverage = r.VoteAverage,
                    MediaType = "movie"
                }).ToList();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting now playing movies from TMDB");
                return new List<TmdbItemDto>();
            }
        }

        /// <summary>
        /// Get detailed information about a movie or TV show.
        /// </summary>
        /// <param name="apiKey">The TMDB API key.</param>
        /// <param name="tmdbId">The TMDB ID.</param>
        /// <param name="mediaType">The media type (movie or tv).</param>
        /// <param name="language">The language code.</param>
        /// <returns>The detailed information.</returns>
        public async Task<TmdbItemDto?> GetDetailAsync(string apiKey, int tmdbId, string mediaType, string language = "en-US")
        {
            try
            {
                var client = _httpClientFactory.CreateClient();
                var endpoint = mediaType == "movie" ? "movie" : "tv";
                
                var response = await GetAsync<TmdbDetailResponse>(
                    client,
                    $"{BaseUrl}/{endpoint}/{tmdbId}",
                    new Dictionary<string, string>
                    {
                        ["api_key"] = apiKey,
                        ["language"] = language,
                        ["append_to_response"] = "credits,videos"
                    });

                if (response == null) return null;

                return new TmdbItemDto
                {
                    Id = response.Id,
                    Title = response.Title ?? response.Name ?? "Unknown",
                    Overview = response.Overview ?? "",
                    PosterPath = response.PosterPath,
                    BackdropPath = response.BackdropPath,
                    ReleaseDate = response.ReleaseDate ?? response.FirstAirDate,
                    VoteAverage = response.VoteAverage,
                    MediaType = mediaType
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting detail for {MediaType} {TmdbId} from TMDB", mediaType, tmdbId);
                return null;
            }
        }

        private async Task<T> GetAsync<T>(HttpClient client, string url, Dictionary<string, string> parameters)
        {
            var queryString = string.Join("&", parameters.Select(p => $"{p.Key}={Uri.EscapeDataString(p.Value)}"));
            var fullUrl = $"{url}?{queryString}";

            var response = await client.GetAsync(fullUrl);
            response.EnsureSuccessStatusCode();

            var json = await response.Content.ReadAsStringAsync();
            return JsonSerializer.Deserialize<T>(json, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            }) ?? throw new InvalidOperationException("Failed to deserialize response");
        }
    }

    // TMDB Response Models
    internal class TmdbSearchResponse
    {
        public List<TmdbResult> Results { get; set; } = new();
    }

    internal class TmdbResult
    {
        public int Id { get; set; }
        public string? Title { get; set; }
        public string? Name { get; set; }
        public string? Overview { get; set; }
        public string? PosterPath { get; set; }
        public string? BackdropPath { get; set; }
        public string? ReleaseDate { get; set; }
        public string? FirstAirDate { get; set; }
        public double VoteAverage { get; set; }
        public string? MediaType { get; set; }
    }

    internal class TmdbDetailResponse
    {
        public int Id { get; set; }
        public string? Title { get; set; }
        public string? Name { get; set; }
        public string? Overview { get; set; }
        public string? PosterPath { get; set; }
        public string? BackdropPath { get; set; }
        public string? ReleaseDate { get; set; }
        public string? FirstAirDate { get; set; }
        public double VoteAverage { get; set; }
        public int VoteCount { get; set; }
        public List<TmdbGenre>? Genres { get; set; }
        public int? Runtime { get; set; }
        public string? Tagline { get; set; }
        public TmdbCredits? Credits { get; set; }
        public TmdbVideos? Videos { get; set; }
    }

    internal class TmdbGenre
    {
        public string? Name { get; set; }
    }

    internal class TmdbCredits
    {
        public List<TmdbCastMember>? Cast { get; set; }
        public List<TmdbCrewMember>? Crew { get; set; }
    }

    internal class TmdbCastMember
    {
        public string? Name { get; set; }
    }

    internal class TmdbCrewMember
    {
        public string? Job { get; set; }
        public string? Name { get; set; }
    }

    internal class TmdbVideos
    {
        public List<TmdbVideo>? Results { get; set; }
    }

    internal class TmdbVideo
    {
        public string? Key { get; set; }
        public string? Name { get; set; }
        public string? Site { get; set; }
        public string? Type { get; set; }
    }
}
