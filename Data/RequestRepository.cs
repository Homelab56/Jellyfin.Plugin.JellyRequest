using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Jellyfin.Plugin.JellyRequest.Api.Dtos;
using Microsoft.Data.Sqlite;
using MediaBrowser.Common.Configuration;
using Microsoft.Extensions.Logging;

namespace Jellyfin.Plugin.JellyRequest.Data
{
    /// <summary>
    /// Repository for managing request data in SQLite.
    /// </summary>
    public class RequestRepository
    {
        private readonly IApplicationPaths _applicationPaths;
        private readonly ILogger<RequestRepository> _logger;
        private readonly string _dbPath;

        /// <summary>
        /// Initializes a new instance of the <see cref="RequestRepository"/> class.
        /// </summary>
        /// <param name="applicationPaths">The application paths.</param>
        /// <param name="logger">The logger.</param>
        public RequestRepository(IApplicationPaths applicationPaths, ILogger<RequestRepository> logger)
        {
            _applicationPaths = applicationPaths;
            _logger = logger;
            _dbPath = Path.Combine(applicationPaths.DataPath, "jellyrequest.db");
            
            InitializeDatabase();
        }

        /// <summary>
        /// Create a new request.
        /// </summary>
        /// <param name="request">The request to create.</param>
        /// <returns>The created request.</returns>
        public async Task<RequestDto> CreateRequestAsync(RequestDto request)
        {
            try
            {
                using var connection = new SqliteConnection($"Data Source={_dbPath}");
                connection.Open();

                var command = connection.CreateCommand();
                command.CommandText = @"
                    INSERT INTO Requests (TmdbId, MediaType, Title, Year, UserId, Status, RequestDate, LastUpdated, RadarrId, SonarrId, PosterPath, BackdropPath)
                    VALUES (@TmdbId, @MediaType, @Title, @Year, @UserId, @Status, @RequestDate, @LastUpdated, @RadarrId, @SonarrId, @PosterPath, @BackdropPath);
                    SELECT last_insert_rowid();";

                command.Parameters.AddWithValue("@TmdbId", request.TmdbId);
                command.Parameters.AddWithValue("@MediaType", request.MediaType);
                command.Parameters.AddWithValue("@Title", request.Title);
                command.Parameters.AddWithValue("@Year", request.Year);
                command.Parameters.AddWithValue("@UserId", request.UserId);
                command.Parameters.AddWithValue("@Status", request.Status);
                command.Parameters.AddWithValue("@RequestDate", DateTime.UtcNow);
                command.Parameters.AddWithValue("@LastUpdated", DateTime.UtcNow);
                command.Parameters.AddWithValue("@RadarrId", request.RadarrId ?? (object)DBNull.Value);
                command.Parameters.AddWithValue("@SonarrId", request.SonarrId ?? (object)DBNull.Value);
                command.Parameters.AddWithValue("@PosterPath", request.PosterPath ?? (object)DBNull.Value);
                command.Parameters.AddWithValue("@BackdropPath", request.BackdropPath ?? (object)DBNull.Value);

                var id = (long?)command.ExecuteScalar();
                request.Id = (int)id;

                _logger.LogInformation("Created request: {RequestType} {TmdbId} for user {UserId}", request.MediaType, request.TmdbId, request.UserId);
                return request;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating request: {TmdbId} for user {UserId}", request.TmdbId, request.UserId);
                throw;
            }
        }

        /// <summary>
        /// Get a request by ID.
        /// </summary>
        /// <param name="id">The request ID.</param>
        /// <returns>The request, or null if not found.</returns>
        public async Task<RequestDto?> GetRequestAsync(int id)
        {
            try
            {
                using var connection = new SqliteConnection($"Data Source={_dbPath}");
                connection.Open();

                var command = connection.CreateCommand();
                command.CommandText = "SELECT * FROM Requests WHERE Id = @Id";
                command.Parameters.AddWithValue("@Id", id);

                using var reader = command.ExecuteReader();
                if (reader.Read())
                {
                    return MapReaderToRequest(reader);
                }

                return null;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting request: {Id}", id);
                return null;
            }
        }

        /// <summary>
        /// Get requests by user ID.
        /// </summary>
        /// <param name="userId">The user ID.</param>
        /// <returns>List of requests.</returns>
        public async Task<List<RequestDto>> GetRequestsByUserAsync(string userId)
        {
            try
            {
                using var connection = new SqliteConnection($"Data Source={_dbPath}");
                connection.Open();

                var command = connection.CreateCommand();
                command.CommandText = "SELECT * FROM Requests WHERE UserId = @UserId ORDER BY RequestDate DESC";
                command.Parameters.AddWithValue("@UserId", userId);

                var requests = new List<RequestDto>();
                using var reader = command.ExecuteReader();
                while (reader.Read())
                {
                    requests.Add(MapReaderToRequest(reader));
                }

                return requests;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting requests for user: {UserId}", userId);
                return new List<RequestDto>();
            }
        }

        /// <summary>
        /// Get all requests.
        /// </summary>
        /// <returns>List of all requests.</returns>
        public async Task<List<RequestDto>> GetAllRequestsAsync()
        {
            try
            {
                using var connection = new SqliteConnection($"Data Source={_dbPath}");
                connection.Open();

                var command = connection.CreateCommand();
                command.CommandText = "SELECT * FROM Requests ORDER BY RequestDate DESC";

                var requests = new List<RequestDto>();
                using var reader = command.ExecuteReader();
                while (reader.Read())
                {
                    requests.Add(MapReaderToRequest(reader));
                }

                return requests;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting all requests");
                return new List<RequestDto>();
            }
        }

        /// <summary>
        /// Update request status.
        /// </summary>
        /// <param name="id">The request ID.</param>
        /// <param name="status">The new status.</param>
        /// <param name="radarrId">The Radarr ID (optional).</param>
        /// <param name="sonarrId">The Sonarr ID (optional).</param>
        /// <returns>True if successful.</returns>
        public async Task<bool> UpdateRequestStatusAsync(int id, string status, int? radarrId = null, int? sonarrId = null)
        {
            try
            {
                using var connection = new SqliteConnection($"Data Source={_dbPath}");
                connection.Open();

                var command = connection.CreateCommand();
                command.CommandText = @"
                    UPDATE Requests 
                    SET Status = @Status, LastUpdated = @LastUpdated, RadarrId = @RadarrId, SonarrId = @SonarrId
                    WHERE Id = @Id";

                command.Parameters.AddWithValue("@Status", status);
                command.Parameters.AddWithValue("@LastUpdated", DateTime.UtcNow);
                command.Parameters.AddWithValue("@Id", id);
                command.Parameters.AddWithValue("@RadarrId", radarrId ?? (object)DBNull.Value);
                command.Parameters.AddWithValue("@SonarrId", sonarrId ?? (object)DBNull.Value);

                var rowsAffected = command.ExecuteNonQuery();
                return rowsAffected > 0;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating request status: {Id}", id);
                return false;
            }
        }

        /// <summary>
        /// Delete a request.
        /// </summary>
        /// <param name="id">The request ID.</param>
        /// <returns>True if successful.</returns>
        public async Task<bool> DeleteRequestAsync(int id)
        {
            try
            {
                using var connection = new SqliteConnection($"Data Source={_dbPath}");
                connection.Open();

                var command = connection.CreateCommand();
                command.CommandText = "DELETE FROM Requests WHERE Id = @Id";
                command.Parameters.AddWithValue("@Id", id);

                var rowsAffected = command.ExecuteNonQuery();
                _logger.LogInformation("Deleted request: {Id}", id);
                return rowsAffected > 0;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting request: {Id}", id);
                return false;
            }
        }

        /// <summary>
        /// Get requests by TMDB ID and media type.
        /// </summary>
        /// <param name="tmdbId">The TMDB ID.</param>
        /// <param name="mediaType">The media type.</param>
        /// <returns>List of matching requests.</returns>
        public async Task<List<RequestDto>> GetRequestsByTmdbIdAsync(int tmdbId, string mediaType)
        {
            try
            {
                using var connection = new SqliteConnection($"Data Source={_dbPath}");
                connection.Open();

                var command = connection.CreateCommand();
                command.CommandText = "SELECT * FROM Requests WHERE TmdbId = @TmdbId AND MediaType = @MediaType";
                command.Parameters.AddWithValue("@TmdbId", tmdbId);
                command.Parameters.AddWithValue("@MediaType", mediaType);

                var requests = new List<RequestDto>();
                using var reader = command.ExecuteReader();
                while (reader.Read())
                {
                    requests.Add(MapReaderToRequest(reader));
                }

                return requests;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting requests by TMDB ID: {TmdbId}", tmdbId);
                return new List<RequestDto>();
            }
        }

        /// <summary>
        /// Get pending requests that need status updates.
        /// </summary>
        /// <returns>List of pending requests.</returns>
        public async Task<List<RequestDto>> GetPendingRequestsAsync()
        {
            try
            {
                using var connection = new SqliteConnection($"Data Source={_dbPath}");
                connection.Open();

                var command = connection.CreateCommand();
                command.CommandText = "SELECT * FROM Requests WHERE Status IN ('Requested', 'Downloading') ORDER BY LastUpdated";

                var requests = new List<RequestDto>();
                using var reader = command.ExecuteReader();
                while (reader.Read())
                {
                    requests.Add(MapReaderToRequest(reader));
                }

                return requests;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting pending requests");
                return new List<RequestDto>();
            }
        }

        private void InitializeDatabase()
        {
            try
            {
                var directory = Path.GetDirectoryName(_dbPath);
                if (!Directory.Exists(directory))
                {
                    Directory.CreateDirectory(directory);
                }

                using var connection = new SqliteConnection($"Data Source={_dbPath}");
                connection.Open();

                var command = connection.CreateCommand();
                command.CommandText = @"
                    CREATE TABLE IF NOT EXISTS Requests (
                        Id INTEGER PRIMARY KEY AUTOINCREMENT,
                        TmdbId INTEGER NOT NULL,
                        MediaType TEXT NOT NULL,
                        Title TEXT NOT NULL,
                        Year INTEGER NOT NULL,
                        UserId TEXT NOT NULL,
                        Status TEXT NOT NULL,
                        RequestDate TEXT NOT NULL,
                        LastUpdated TEXT NOT NULL,
                        RadarrId INTEGER,
                        SonarrId INTEGER,
                        PosterPath TEXT,
                        BackdropPath TEXT
                    );

                    CREATE INDEX IF NOT EXISTS idx_requests_tmdb ON Requests(TmdbId, MediaType);
                    CREATE INDEX IF NOT EXISTS idx_requests_user ON Requests(UserId);
                    CREATE INDEX IF NOT EXISTS idx_requests_status ON Requests(Status);
                ";

                command.ExecuteNonQuery();
                _logger.LogInformation("Database initialized at: {DbPath}", _dbPath);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error initializing database");
                throw;
            }
        }

        private static RequestDto MapReaderToRequest(Microsoft.Data.Sqlite.SqliteDataReader reader)
        {
            return new RequestDto
            {
                Id = reader.GetInt32(0),
                TmdbId = reader.GetInt32(1),
                MediaType = reader.GetString(2),
                Title = reader.GetString(3),
                Year = reader.GetInt32(4),
                UserId = reader.GetString(5),
                Status = reader.GetString(6),
                RequestDate = DateTime.Parse(reader.GetString(7)),
                LastUpdated = DateTime.Parse(reader.GetString(8)),
                RadarrId = reader.IsDBNull(9) ? null : reader.GetInt32(9),
                SonarrId = reader.IsDBNull(10) ? null : reader.GetInt32(10),
                PosterPath = reader.IsDBNull(11) ? null : reader.GetString(11),
                BackdropPath = reader.IsDBNull(12) ? null : reader.GetString(12)
            };
        }
    }
}
