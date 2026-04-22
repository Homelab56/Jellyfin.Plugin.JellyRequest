using System;

namespace Jellyfin.Plugin.JellyRequest.Api.Dtos
{
    /// <summary>
    /// Data transfer object for request status information.
    /// </summary>
    public class RequestStatusDto
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
        /// Gets or sets the request status.
        /// </summary>
        public string Status { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets a value indicating whether the item is in the library.
        /// </summary>
        public bool IsInLibrary { get; set; }

        /// <summary>
        /// Gets or sets the request date.
        /// </summary>
        public DateTime? RequestDate { get; set; }

        /// <summary>
        /// Gets or sets the last updated date.
        /// </summary>
        public DateTime? LastUpdated { get; set; }

        /// <summary>
        /// Gets or sets the download progress percentage (if downloading).
        /// </summary>
        public double? ProgressPercentage { get; set; }

        /// <summary>
        /// Gets or sets the display text for the status.
        /// </summary>
        public string DisplayText => Status switch
        {
            "NotRequested" => "Not Available",
            "Requested" => "Requested",
            "Downloading" => "Downloading",
            "Completed" => "Completed",
            "Available" => "Available",
            "Failed" => "Failed",
            _ => "Unknown"
        };

        /// <summary>
        /// Gets or sets the CSS class for the status badge.
        /// </summary>
        public string StatusClass => Status switch
        {
            "NotRequested" => "status-not-requested",
            "Requested" => "status-requested",
            "Downloading" => "status-downloading",
            "Completed" => "status-completed",
            "Available" => "status-available",
            "Failed" => "status-failed",
            _ => "status-unknown"
        };

        /// <summary>
        /// Gets or sets a value indicating whether the item can be requested.
        /// </summary>
        public bool CanRequest => !IsInLibrary && Status == "NotRequested";

        /// <summary>
        /// Gets or sets a value indicating whether the item can be played.
        /// </summary>
        public bool CanPlay => IsInLibrary || Status == "Available";
    }
}
