using System.Collections.Generic;

namespace Jellyfin.Plugin.JellyRequest.Api.Dtos
{
    /// <summary>
    /// Data transfer object for search results.
    /// </summary>
    public class SearchResultDto
    {
        /// <summary>
        /// Gets or sets the list of movies.
        /// </summary>
        public List<TmdbItemDto> Movies { get; set; } = new();

        /// <summary>
        /// Gets or sets the list of TV shows.
        /// </summary>
        public List<TmdbItemDto> TvShows { get; set; } = new();
    }
}
