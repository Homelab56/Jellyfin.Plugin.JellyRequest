using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using Jellyfin.Plugin.JellyRequest.Data;
using Jellyfin.Plugin.JellyRequest.Configuration;
using Jellyfin.Plugin.JellyRequest.Services;
using MediaBrowser.Common.Configuration;
using MediaBrowser.Common.Plugins;
using MediaBrowser.Model.Plugins;
using MediaBrowser.Model.Serialization;
using Microsoft.Extensions.DependencyInjection;


namespace Jellyfin.Plugin.JellyRequest
{
    /// <summary>
    /// The main plugin entry point for JellyRequest.
    /// </summary>
    public class Plugin : BasePlugin<PluginConfiguration>, IHasWebPages
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="Plugin"/> class.
        /// </summary>
        /// <param name="applicationPaths">Instance of the <see cref="IApplicationPaths"/> interface.</param>
        /// <param name="xmlSerializer">Instance of the <see cref="IXmlSerializer"/> interface.</param>
        public Plugin(IApplicationPaths applicationPaths, IXmlSerializer xmlSerializer)
            : base(applicationPaths, xmlSerializer)
        {
            Instance = this;
        }

        /// <summary>
        /// Gets the current plugin instance.
        /// </summary>
        public static Plugin? Instance { get; private set; }

        /// <inheritdoc />
        public override string Name => "JellyRequest";

        /// <inheritdoc />
        public override string Description => "Netflix-style content discovery and request system for Jellyfin.";

        /// <inheritdoc />
        public override Guid Id => Guid.Parse("c8b5e8a2-4d7c-4e9f-8a1b-2c3d4e5f6a7b");

        /// <inheritdoc />
        public IEnumerable<PluginPageInfo> GetPages()
        {
            return new[]
            {
                new PluginPageInfo
                {
                    Name = "JellyRequest",
                    EmbeddedResourcePath = GetType().Namespace + ".Web.discover.html"
                },
                new PluginPageInfo
                {
                    Name = "JellyRequestConfig",
                    EmbeddedResourcePath = GetType().Namespace + ".Configuration.config.html"
                },
                new PluginPageInfo
                {
                    Name = "MyRequests",
                    EmbeddedResourcePath = GetType().Namespace + ".Web.myrequests.html"
                }
            };
        }

        public void ConfigureServices(IServiceCollection serviceCollection)
        {
            serviceCollection.AddHostedService<PollingService>();
            serviceCollection.AddSingleton<TmdbService>();
            serviceCollection.AddSingleton<RadarrService>();
            serviceCollection.AddSingleton<SonarrService>();
            serviceCollection.AddSingleton<LibraryMatchService>();
            serviceCollection.AddSingleton<RequestRepository>();
            serviceCollection.AddSingleton<RequestService>();
        }
    }
}
