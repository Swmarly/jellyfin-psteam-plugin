using MediaBrowser.Model.Plugins;

namespace Jellyfin.Plugin.PStream;

public class PluginConfiguration : BasePluginConfiguration
{
    public string BaseUrl { get; set; } = "https://api.p-stream.example";

    public string ApiKey { get; set; } = string.Empty;

    public int DefaultPageSize { get; set; } = 25;
}
