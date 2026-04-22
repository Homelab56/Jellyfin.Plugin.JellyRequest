using System;

namespace Jellyfin.Plugin.JellyRequest.Api.Dtos
{
    public class StatusDto
    {
        public int TmdbId { get; set; }
        public string MediaType { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public bool IsInLibrary { get; set; }
        public DateTime? RequestDate { get; set; }
        public DateTime? LastUpdated { get; set; }
    }
}
