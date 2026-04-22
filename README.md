# JellyRequest Plugin

A Netflix-style content discovery and request system for Jellyfin with seamless Radarr/Sonarr integration. Transform your Jellyfin server into a modern streaming platform where users can discover, browse, and request content with one click.

## Quick Installation

### From Repository (Recommended)
```bash
# Clone the repository
git clone https://github.com/yourusername/Jellyfin.Plugin.JellyRequest.git
cd Jellyfin.Plugin.JellyRequest

# Build the plugin
dotnet build -c Release

# Copy to your Jellyfin plugins directory
cp -r bin/Release/* /path/to/jellyfin/config/plugins/JellyRequest/

# Restart Jellyfin
sudo systemctl restart jellyfin
```

### Manual Installation
1. Download the latest release from the [Releases page](https://github.com/yourusername/Jellyfin.Plugin.JellyRequest/releases)
2. Extract to your Jellyfin plugins directory
3. Restart Jellyfin
4. Configure in Dashboard > Plugins > JellyRequest > Settings

## Features

### Netflix-Style Discovery Interface
- **Horizontal Carousels**: Browse trending movies, TV shows, and popular content with smooth scrolling
- **Rich Content Cards**: Poster images, ratings, year, and genre information
- **Smart Search**: Full-text search across TMDB's extensive database with 400ms debouncing
- **Visual Indicators**: See at a glance what's already in your library vs. what can be requested

### Intelligent Request System
- **One-Click Requests**: Request movies and TV shows directly from the discovery interface
- **Automatic Integration**: Seamlessly sends requests to Radarr (movies) or Sonarr (TV shows)
- **Real-Time Status**: Track request progress from "Requested" to "Downloading" to "Available"
- **Smart Detection**: Automatically marks items as "Available" when they appear in your Jellyfin library

### User Management
- **Request History**: View and manage all your requests in the "My Requests" page
- **Status Tracking**: Monitor download progress with visual progress bars
- **Cancel Requests**: Remove unwanted requests before they complete
- **Permission Control**: Admins can restrict request permissions by user role

### Admin Configuration
- **Intuitive Setup**: Web-based configuration page for all settings
- **Connection Testing**: Verify Radarr/Sonarr connectivity with one click
- **Quality Profiles**: Configure default quality profiles for new requests
- **User Limits**: Set maximum requests per user to manage demand

## Environment

### Dumbarr Docker Setup
- **Runtime**: .NET 9
- **Container**: "dumb" (Dumbarr all-in-one image)
- **Jellyfin**: http://localhost:8096
- **Radarr**: http://localhost:7878 (inside container)
- **Sonarr**: http://localhost:8989 (inside container)
- **Plugin Path**: `/docker/dumbarr/jellyfin/config/plugins/JellyRequest/`

### API Keys Required
- **TMDB API Key**: Free from [The Movie Database](https://www.themoviedb.org/settings/api)
- **Radarr API Key**: From Radarr Settings > General > Security
- **Sonarr API Key**: From Sonarr Settings > General > Security

## Installation

### Dumbarr Docker Installation

1. **Build the Plugin**
   ```bash
   cd JellyRequest
   dotnet restore
   dotnet build -c Release
   ```

2. **Install to Docker Container**
   ```bash
   # Copy built files to Dumbarr plugin directory
   cp -r bin/Release/net9.0/* /docker/dumbarr/jellyfin/config/plugins/JellyRequest/
   ```

3. **Restart Jellyfin**
   ```bash
   docker restart dumb
   ```

4. **Check Installation**
   ```bash
   # Check logs for any errors
   docker logs dumb 2>&1 | grep -i jellyrequest
   ```

5. **Configure the Plugin**
   - Open Jellyfin: http://localhost:8096
   - Navigate to Dashboard > Plugins > JellyRequest > Settings
   - Enter your TMDB API key (free at themoviedb.org/settings/api)
   - Enter Radarr API key (Radarr > Settings > General > API Key)
   - Enter Sonarr API key (Sonarr > Settings > General > API Key)
   - Click "Test Connection" for both, then Save

### Method 2: Plugin Repository (Coming Soon)

Add the JellyRequest plugin repository to Jellyfin for automatic updates:
1. Go to Dashboard > Plugins > Repositories
2. Add repository URL: `https://your-repo-url/jellyrequest/manifest.json`
3. Install JellyRequest from the catalog

## Configuration

### Basic Setup

1. **TMDB Configuration**
   - Get a free API key from [TMDB](https://www.themoviedb.org/settings/api)
   - Enter the key in the plugin configuration
   - Choose your preferred language and content settings

2. **Radarr Setup (Movies)**
   - Enter your Radarr URL (e.g., `http://localhost:7878`)
   - Add your Radarr API key from Settings > General > Security
   - Test the connection to verify setup
   - Set default quality profile and root folder

3. **Sonarr Setup (TV Shows)**
   - Enter your Sonarr URL (e.g., `http://localhost:8989`)
   - Add your Sonarr API key from Settings > General > Security
   - Test the connection to verify setup
   - Set default quality profile and root folder

### Advanced Settings

- **User Permissions**: Control whether regular users can make requests
- **Request Limits**: Set maximum active requests per user (default: 10)
- **Polling Interval**: How often to check Radarr/Sonarr for status updates (default: 5 minutes)
- **Notifications**: Enable/disable request completion notifications

## Usage

### For Users

1. **Discover Content**
   - Navigate to the "Discover" tab in Jellyfin
   - Browse trending movies and TV shows in horizontal carousels
   - Use the search bar to find specific content

2. **Request Content**
   - Click the "+" button on any content card to request it
   - View detailed information including trailers, cast, and ratings
   - Click "Request" to send it to Radarr/Sonarr automatically

3. **Track Requests**
   - Visit "My Requests" to see all your active and completed requests
   - Monitor download progress with visual indicators
   - Cancel unwanted requests before they complete

4. **Watch Content**
   - When requests show "Available" or "In Library", click "Play Now"
   - Content opens directly in Jellyfin's media player

### For Administrators

1. **Monitor Requests**
   - View all user requests through the SQLite database
   - Check Radarr/Sonarr queues for download status
   - Manage user permissions and request limits

2. **Troubleshooting**
   - Use the connection test feature to verify API connectivity
   - Check Jellyfin logs for plugin errors
   - Monitor Radarr/Sonarr logs for download issues

## Troubleshooting

### Common Issues

#### Plugin Not Loading
- **Symptom**: Plugin doesn't appear in Jellyfin dashboard
- **Solution**: 
  - Verify .NET 8.0 runtime is installed
  - Check plugin file permissions
  - Restart Jellyfin service
  - Check Jellyfin logs for loading errors

#### TMDB API Errors
- **Symptom**: "TMDB API key not configured" or search failures
- **Solution**:
  - Verify API key is correct and active
  - Check TMDB account status and API quota
  - Ensure network connectivity to api.themoviedb.org

#### Radarr/Sonarr Connection Issues
- **Symptom**: Connection test fails or requests don't appear
- **Solution**:
  - Verify URLs are accessible from Jellyfin server
  - Check API keys are correct and have proper permissions
  - Ensure Radarr/Sonarr are running and accessible
  - Check for firewall or network restrictions

#### Requests Not Processing
- **Symptom**: Requests show "Requested" but never download
- **Solution**:
  - Check Radarr/Sonarr queues for stuck downloads
  - Verify quality profiles and root folders exist
  - Check indexer connectivity in Radarr/Sonarr
  - Review download client settings

#### Content Not Marking as Available
- **Symptom**: Downloads complete but don't show as "Available"
- **Solution**:
  - Verify library paths match between Jellyfin and Radarr/Sonarr
  - Check if metadata refresh is enabled in Jellyfin
  - Manually trigger library scan in Jellyfin
  - Verify file permissions and access

### Debug Mode

Enable debug logging for detailed troubleshooting:

1. Edit Jellyfin's `logging.json` configuration
2. Add or modify the JellyRequest namespace:
   ```json
   {
     "Name": "Jellyfin.Plugin.JellyRequest",
     "Console": {
       "Enabled": true
     },
     "File": {
       "Enabled": true
     }
   }
   ```
3. Restart Jellyfin and check logs in `/var/log/jellyfin/` or Jellyfin dashboard

### Log Locations
- **Linux**: `/var/log/jellyfin/log.log`
- **Windows**: `C:\ProgramData\Jellyfin\Server\log\log.log`
- **Docker**: Check container logs with `docker logs jellyfin`

## Development

### Building from Source

1. **Prerequisites**
   - .NET 8.0 SDK
   - Git
   - Your preferred IDE (Visual Studio, VS Code, etc.)

2. **Clone Repository**
   ```bash
   git clone https://github.com/your-repo/JellyRequest.git
   cd JellyRequest
   ```

3. **Build Project**
   ```bash
   dotnet build --configuration Release
   ```

4. **Run Tests**
   ```bash
   dotnet test
   ```

5. **Package Plugin**
   ```bash
   dotnet publish --configuration Release --output dist
   ```

### Project Structure

```
JellyRequest/
|-- Api/                     # Web API controllers and models
|-- Data/                    # SQLite database layer
|-- Services/                # Business logic and external APIs
|-- Web/                     # Frontend HTML, CSS, and JavaScript
|-- Configuration/           # Plugin configuration
|-- Plugin.cs               # Plugin entry point
|-- JellyRequest.csproj     # Project configuration
```

### Contributing

1. Fork the repository
2. Create a feature branch: `git checkout -b feature/amazing-feature`
3. Commit changes: `git commit -m 'Add amazing feature'`
4. Push to branch: `git push origin feature/amazing-feature`
5. Submit a pull request

## Support

### Getting Help
- **GitHub Issues**: Report bugs and request features
- **Discord**: Join our community for real-time support
- **Documentation**: Check the wiki for detailed guides

### FAQ

**Q: Does this work with *arr v4?**  
A: Currently only v3.x is supported. v4 support is planned for a future release.

**Q: Can I use this without Radarr/Sonarr?**  
A: The plugin requires Radarr for movies and Sonarr for TV shows. These services handle the actual downloading and library management.

**Q: Is my data secure?**  
A: The plugin only stores request metadata locally. All API keys are stored securely in Jellyfin's configuration. No data is sent to external servers except TMDB for content metadata.

**Q: Can I customize the UI?**  
A: The CSS can be modified in the `Web/assets/jellyrequest.css` file. UI customization options will be added in future versions.

## License

This project is licensed under the GPL v3 License - see the [LICENSE](LICENSE) file for details.

## Changelog

### v1.0.0 (2024-04-21)
- Initial release
- Netflix-style discovery interface
- Radarr/Sonarr integration
- User request management
- Admin configuration panel
- SQLite database for request tracking

## Credits

- **Jellyfin Team**: For the amazing media server platform
- **TMDB**: For providing comprehensive movie and TV metadata
- **Radarr/Sonarr Teams**: For excellent content automation tools
- **Netflix**: For inspiring the user interface design

---

**Enjoy your enhanced Jellyfin experience with JellyRequest!**
