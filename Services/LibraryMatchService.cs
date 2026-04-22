using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Jellyfin.Data.Enums;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Library;
using MediaBrowser.Model.Entities;
using MediaBrowser.Model.Querying;
using Microsoft.Extensions.Logging;

namespace Jellyfin.Plugin.JellyRequest.Services
{
    public class LibraryMatchService
    {
        private readonly ILibraryManager _libraryManager;
        private readonly ILogger<LibraryMatchService> _logger;

        public LibraryMatchService(ILibraryManager libraryManager, ILogger<LibraryMatchService> logger)
        {
            _libraryManager = libraryManager;
            _logger = logger;
        }

        public Task<bool> MovieExistsAsync(int tmdbId)
        {
            try
            {
                var items = _libraryManager.GetItemList(new InternalItemsQuery
                {
                    IncludeItemTypes = new[] { BaseItemKind.Movie },
                    HasTmdbId = true
                });
                return Task.FromResult(items.Any(i => GetTmdbId(i) == tmdbId));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking if movie exists: TMDB {TmdbId}", tmdbId);
                return Task.FromResult(false);
            }
        }

        public Task<bool> SeriesExistsAsync(int tmdbId)
        {
            try
            {
                var items = _libraryManager.GetItemList(new InternalItemsQuery
                {
                    IncludeItemTypes = new[] { BaseItemKind.Series },
                    HasTmdbId = true
                });
                return Task.FromResult(items.Any(i => GetTmdbId(i) == tmdbId));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking if series exists: TMDB {TmdbId}", tmdbId);
                return Task.FromResult(false);
            }
        }

        public Task<Dictionary<int, bool>> CheckMultipleItemsExistAsync(Dictionary<int, string> tmdbIds)
        {
            var result = new Dictionary<int, bool>();
            try
            {
                var movies = _libraryManager.GetItemList(new InternalItemsQuery
                {
                    IncludeItemTypes = new[] { BaseItemKind.Movie },
                    HasTmdbId = true
                });
                var series = _libraryManager.GetItemList(new InternalItemsQuery
                {
                    IncludeItemTypes = new[] { BaseItemKind.Series },
                    HasTmdbId = true
                });

                var movieIds = new HashSet<int>(movies.Select(GetTmdbId).Where(id => id > 0));
                var seriesIds = new HashSet<int>(series.Select(GetTmdbId).Where(id => id > 0));

                foreach (var kvp in tmdbIds)
                {
                    result[kvp.Key] = kvp.Value == "movie"
                        ? movieIds.Contains(kvp.Key)
                        : seriesIds.Contains(kvp.Key);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking multiple items existence");
            }
            return Task.FromResult(result);
        }

        public string? GetPlaybackUrl(BaseItem item)
        {
            try
            {
                if (item == null) return null;
                return $"/web/index.html#!/item?id={item.Id}";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting playback URL for item: {ItemId}", item?.Id);
                return null;
            }
        }

        private static int GetTmdbId(BaseItem item)
        {
            var tmdbIdStr = item.GetProviderId(MediaBrowser.Model.Entities.MetadataProvider.Tmdb);
            return int.TryParse(tmdbIdStr, out var id) ? id : 0;
        }
    }
}
