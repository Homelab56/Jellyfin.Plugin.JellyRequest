using System;

namespace Jellyfin.Plugin.JellyRequest.Api.Dtos
{
    /// <summary>
    /// Data transfer object for content requests.
    /// </summary>
    public class RequestDto
    {
        /// <summary>
        /// Gets or sets the request ID.
        /// </summary>
        public int Id { get; set; }

        /// <summary>
        /// Gets or sets the TMDB ID.
        /// </summary>
        public int TmdbId { get; set; }

        /// <summary>
        /// Gets or sets the media type (movie or tv).
        /// </summary>
        public string MediaType { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the title.
        /// </summary>
        public string Title { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the release year.
        /// </summary>
        public int Year { get; set; }

        /// <summary>
        /// Gets or sets the user ID who made the request.
        /// </summary>
        public string UserId { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the request status.
        /// </summary>
        public string Status { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the request date.
        /// </summary>
        public DateTime RequestDate { get; set; }

        /// <summary>
        /// Gets or sets the last updated date.
        /// </summary>
        public DateTime LastUpdated { get; set; }

        /// <summary>
        /// Gets or sets the Radarr ID (for movies).
        /// </summary>
        public int? RadarrId { get; set; }

        /// <summary>
        /// Gets or sets the Sonarr ID (for TV shows).
        /// </summary>
        public int? SonarrId { get; set; }

        /// <summary>
        /// Gets or sets the poster path.
        /// </summary>
        public string? PosterPath { get; set; }

        /// <summary>
        /// Gets or sets the backdrop path.
        /// </summary>
        public string? BackdropPath { get; set; }
    }
}
