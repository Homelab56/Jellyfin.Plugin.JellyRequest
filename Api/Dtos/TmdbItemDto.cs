using System;

namespace Jellyfin.Plugin.JellyRequest.Api.Dtos
{
    /// <summary>
    /// Data transfer object for TMDB items.
    /// </summary>
    public class TmdbItemDto
    {
        /// <summary>
        /// Gets or sets the TMDB ID.
        /// </summary>
        public int Id { get; set; }

        /// <summary>
        /// Gets or sets the title.
        /// </summary>
        public string Title { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the overview.
        /// </summary>
        public string Overview { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the poster path.
        /// </summary>
        public string? PosterPath { get; set; }

        /// <summary>
        /// Gets or sets the backdrop path.
        /// </summary>
        public string? BackdropPath { get; set; }

        /// <summary>
        /// Gets or sets the release date.
        /// </summary>
        public string? ReleaseDate { get; set; }

        /// <summary>
        /// Gets or sets the vote average.
        /// </summary>
        public double VoteAverage { get; set; }

        /// <summary>
        /// Gets or sets the media type.
        /// </summary>
        public string MediaType { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets a value indicating whether the item is in the library.
        /// </summary>
        public bool IsInLibrary { get; set; }

        /// <summary>
        /// Gets or sets the full poster URL.
        /// </summary>
        public string? PosterUrl => PosterPath != null ? $"https://image.tmdb.org/t/p/w500{PosterPath}" : null;

        /// <summary>
        /// Gets or sets the full backdrop URL.
        /// </summary>
        public string? BackdropUrl => BackdropPath != null ? $"https://image.tmdb.org/t/p/original{BackdropPath}" : null;

        /// <summary>
        /// Gets or sets the formatted release year.
        /// </summary>
        public string? Year => ReleaseDate?.Split('-')[0];
    }
}
