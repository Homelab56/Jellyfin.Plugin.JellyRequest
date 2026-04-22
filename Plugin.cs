cat <<EOF > Plugin.cs
using System;
using System.Collections.Generic;
using Jellyfin.Plugin.JellyRequest.Configuration;
using MediaBrowser.Common.Configuration;
using MediaBrowser.Common.Plugins;
using MediaBrowser.Model.Plugins;
using MediaBrowser.Model.Serialization;

namespace Jellyfin.Plugin.JellyRequest
{
    public class Plugin : BasePlugin<PluginConfiguration>, IHasWebPages
    {
        public override string Name => "JellyRequest";
        public override Guid Id => Guid.Parse("c8b5e8a2-4d7c-4e9f-8a1b-2c3d4e5f6a7b");

        public Plugin(IApplicationPaths applicationPaths, IXmlSerializer xmlSerializer)
            : base(applicationPaths, xmlSerializer)
        {
            Instance = this;
        }

        public static Plugin Instance { get; private set; }

        public IEnumerable<PluginPageInfo> GetPages()
        {
            return new[]
            {
                new PluginPageInfo
                {
                    Name = "JellyRequest",
                    EmbeddedResourcePath = GetType().Namespace + ".Web.index.html"
                }
            };
        }
    }
}
EOF