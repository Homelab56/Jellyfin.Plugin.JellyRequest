# JellyRequest Installation Guide

This guide will walk you through installing and configuring the JellyRequest plugin for your Jellyfin server.

## Quick Start (TL;DR)

1. Get API keys from TMDB, Radarr, and Sonarr
2. Install the plugin to your Jellyfin plugins directory
3. Restart Jellyfin
4. Configure the plugin in the dashboard
5. Start discovering and requesting content!

## Prerequisites Check

Before installing, ensure you have:

### Required Software Versions
- [ ] **Jellyfin 10.9.x** or later
- [ ] **.NET 8.0 Runtime** installed on the server
- [ ] **Radarr v3.x** (for movie requests)
- [ ] **Sonarr v3.x** (for TV show requests)

### Network Access
- [ ] Jellyfin server can access TMDB API (`api.themoviedb.org`)
- [ ] Jellyfin server can access your Radarr instance
- [ ] Jellyfin server can access your Sonarr instance
- [ ] Radarr/Sonarr can access the internet (for indexers)

### API Keys Ready
- [ ] **TMDB API Key**: Get from [TMDB Settings](https://www.themoviedb.org/settings/api)
- [ ] **Radarr API Key**: Found in Radarr > Settings > General > Security
- [ ] **Sonarr API Key**: Found in Sonarr > Settings > General > Security

## Step-by-Step Installation

### Step 1: Obtain API Keys

#### TMDB API Key (Free)
1. Visit [TMDB](https://www.themoviedb.org/)
2. Create a free account or log in
3. Go to Settings > API > Request an API Key
4. Choose "Developer" type
5. Accept the terms and submit
6. Copy your API key (v3 auth)

#### Radarr API Key
1. Open your Radarr web interface
2. Go to Settings > General > Security
3. Your API key will be listed there
4. Click to copy the key

#### Sonarr API Key
1. Open your Sonarr web interface
2. Go to Settings > General > Security
3. Your API key will be listed there
4. Click to copy the key

### Step 2: Download the Plugin

#### Option A: Download Pre-built Release
1. Visit the [JellyRequest Releases](https://github.com/your-repo/JellyRequest/releases)
2. Download the latest `JellyRequest.zip` file
3. Extract the zip file to a temporary location

#### Option B: Build from Source
```bash
# Clone the repository
git clone https://github.com/your-repo/JellyRequest.git
cd JellyRequest

# Build the plugin
dotnet build --configuration Release

# The plugin DLL will be in: bin/Release/net8.0/JellyRequest.dll
```

### Step 3: Install Plugin to Jellyfin

#### Windows Installation
1. Stop the Jellyfin service
2. Navigate to your Jellyfin plugins directory:
   ```
   C:\ProgramData\Jellyfin\Server\plugins\
   ```
3. Create a new folder named `JellyRequest`
4. Copy `JellyRequest.dll` to the new folder
5. Restart the Jellyfin service

#### Linux Installation
1. Stop the Jellyfin service:
   ```bash
   sudo systemctl stop jellyfin
   ```
2. Navigate to the plugins directory:
   ```bash
   cd /var/lib/jellyfin/plugins/
   ```
3. Create a new folder and copy the plugin:
   ```bash
   sudo mkdir JellyRequest
   sudo cp /path/to/JellyRequest.dll JellyRequest/
   sudo chown -R jellyfin:jellyfin JellyRequest/
   ```
4. Restart the Jellyfin service:
   ```bash
   sudo systemctl start jellyfin
   ```

#### Docker Installation
1. Add the plugin to your Docker volume:
   ```bash
   # Copy plugin to mounted plugins directory
   docker cp JellyRequest.dll jellyfin-container:/config/plugins/JellyRequest/
   ```
2. Restart the Docker container:
   ```bash
   docker restart jellyfin-container
   ```

### Step 4: Verify Installation

1. Open your Jellyfin web interface
2. Go to Dashboard > Plugins
3. You should see "JellyRequest" in the installed plugins list
4. If not listed, check the Jellyfin logs for errors

### Step 5: Configure the Plugin

1. In the Jellyfin Dashboard, click on "JellyRequest" in the plugins list
2. Click the "Configuration" tab
3. Fill in the required settings:

#### TMDB Configuration
- **TMDB API Key**: Paste your TMDB API key
- **Default Language**: Choose your preferred language
- **Include Adult Content**: Check if desired

#### Radarr Configuration (Movies)
- **Radarr URL**: Full URL to your Radarr instance (e.g., `http://localhost:7878`)
- **Radarr API Key**: Paste your Radarr API key
- **Quality Profile ID**: Default profile for movie requests
- **Root Folder ID**: Default location for movie downloads

#### Sonarr Configuration (TV Shows)
- **Sonarr URL**: Full URL to your Sonarr instance (e.g., `http://localhost:8989`)
- **Sonarr API Key**: Paste your Sonarr API key
- **Quality Profile ID**: Default profile for TV show requests
- **Root Folder ID**: Default location for TV show downloads

#### User Permissions
- **Allow Regular User Requests**: Enable/disable for non-admin users
- **Max Requests Per User**: Set request limits (default: 10)

#### Advanced Settings
- **Status Polling Interval**: How often to check for updates (default: 5 minutes)
- **Enable Notifications**: Show completion notifications

### Step 6: Test Connections

1. Click "Test Connection" next to Radarr settings
2. You should see "Connected" if successful
3. Click "Test Connection" next to Sonarr settings
4. You should see "Connected" if successful
5. Click "Save Configuration" at the bottom

### Step 7: Access the Plugin

1. Refresh your Jellyfin web interface
2. You should now see a "Discover" tab in the main navigation
3. Click "Discover" to start browsing content
4. Click "My Requests" to view your request history

## Configuration Details

### Finding Quality Profile IDs

#### In Radarr:
1. Go to Settings > Profiles > Quality
2. Click on your desired quality profile
3. The URL will show the ID (e.g., `/qualityprofile/edit/1`)
4. The number at the end is the profile ID

#### In Sonarr:
1. Go to Settings > Profiles > Quality
2. Click on your desired quality profile
3. The URL will show the ID (e.g., `/qualityprofile/edit/1`)
4. The number at the end is the profile ID

### Finding Root Folder IDs

#### In Radarr:
1. Go to Settings > Media Management > Root Folders
2. Click on your desired root folder
3. The URL will show the ID (e.g., `/rootfolder/edit/1`)
4. The number at the end is the folder ID

#### In Sonarr:
1. Go to Settings > Media Management > Root Folders
2. Click on your desired root folder
3. The URL will show the ID (e.g., `/rootfolder/edit/1`)
4. The number at the end is the folder ID

## Troubleshooting Installation

### Plugin Not Showing Up

**Symptoms**: Plugin doesn't appear in Dashboard > Plugins

**Solutions**:
1. Verify .NET 8.0 runtime is installed on the server
2. Check that the DLL is in the correct plugins directory
3. Ensure proper file permissions:
   - Windows: Read/Execute for the Jellyfin service account
   - Linux: `chown jellyfin:jellyfin JellyRequest.dll`
4. Restart Jellyfin service completely
5. Check Jellyfin logs for loading errors

### Connection Test Failures

**Radarr/Sonarr Connection Failed**:
1. Verify the URLs are correct (include http:// or https://)
2. Check that Radarr/Sonarr are running and accessible
3. Ensure API keys are correct and have proper permissions
4. Check for firewall blocking connections
5. Try accessing the URLs from the Jellyfin server

**TMDB API Errors**:
1. Verify the API key is correct and active
2. Check TMDB account hasn't exceeded API limits
3. Ensure network can reach `api.themoviedb.org`
4. Try regenerating the TMDB API key

### Permission Issues

**Windows Service Issues**:
1. Ensure Jellyfin service has read access to plugins directory
2. Run Jellyfin service with appropriate permissions
3. Check Windows Event Viewer for service errors

**Linux Permission Issues**:
1. Ensure jellyfin user owns the plugin files:
   ```bash
   sudo chown -R jellyfin:jellyfin /var/lib/jellyfin/plugins/JellyRequest/
   ```
2. Check SELinux permissions if enabled:
   ```bash
   sudo semanage fcontext -a -t httpd_sys_content_t "/var/lib/jellyfin/plugins/JellyRequest(/.*)?"
   sudo restorecon -R /var/lib/jellyfin/plugins/JellyRequest/
   ```

### Docker-Specific Issues

**Plugin Not Persisting**:
1. Ensure plugins directory is properly mounted
2. Check Docker volume permissions
3. Verify the plugin is copied to the correct location inside the container

**Network Connectivity**:
1. Use Docker network aliases for Radarr/Sonarr
2. Ensure containers can communicate (same Docker network)
3. Use container names instead of localhost in URLs

## Post-Installation Setup

### Configure Radarr/Sonarr

For optimal performance, ensure your *arr services are properly configured:

#### Indexers Setup
1. Add multiple indexers for better availability
2. Configure retention settings appropriately
3. Set up automatic download client connections

#### Quality Profiles
1. Create appropriate quality profiles for different user preferences
2. Configure upgrade rules for better quality when available
3. Set proper file size limits

#### Root Folders
1. Organize with proper folder structure
2. Ensure adequate disk space
3. Set appropriate permissions for Jellyfin access

### Jellyfin Library Setup

1. Add your media libraries to Jellyfin
2. Configure metadata refresh settings
3. Set up automatic library scanning
4. Verify file access permissions

## Getting Help

If you encounter issues during installation:

1. **Check the Logs**: Review Jellyfin logs for error messages
2. **GitHub Issues**: Search existing issues or create a new one
3. **Community Support**: Join our Discord community
4. **Documentation**: Check the main README for troubleshooting

### Useful Commands

```bash
# Check Jellyfin service status (Linux)
sudo systemctl status jellyfin

# View Jellyfin logs (Linux)
sudo journalctl -u jellyfin -f

# Restart Jellyfin (Linux)
sudo systemctl restart jellyfin

# Check file permissions (Linux)
ls -la /var/lib/jellyfin/plugins/JellyRequest/

# Test network connectivity
curl -I https://api.themoviedb.org/3/movie/550?api_key=YOUR_KEY
```

## Verification

After installation, verify everything is working:

1. **Plugin Loaded**: Plugin appears in Dashboard > Plugins
2. **Configuration Saved**: All settings are saved without errors
3. **Connections Working**: Radarr/Sonarr connection tests pass
4. **UI Accessible**: "Discover" tab appears and loads content
5. **Search Working**: Can search for movies and TV shows
6. **Request Test**: Can successfully request content

## Next Steps

Once installed and configured:

1. **Train Users**: Show users how to discover and request content
2. **Monitor Usage**: Check request patterns and adjust limits if needed
3. **Optimize Settings**: Fine-tune quality profiles and download settings
4. **Stay Updated**: Check for plugin updates regularly

---

**Congratulations! You've successfully installed JellyRequest and transformed your Jellyfin server into a modern streaming platform!**
