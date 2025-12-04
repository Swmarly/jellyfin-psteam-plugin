using MediaBrowser.Common.Configuration;
using MediaBrowser.Common.Plugins;
using MediaBrowser.Controller;
using MediaBrowser.Controller.Channels;
using MediaBrowser.Controller.Plugins;
using MediaBrowser.Model.Plugins;
using MediaBrowser.Model.Serialization;
using Microsoft.Extensions.DependencyInjection;

namespace Jellyfin.Plugin.PStream;

public class Plugin : BasePlugin<PluginConfiguration>, IHasWebPages
{
    public Plugin(IApplicationPaths applicationPaths, IXmlSerializer xmlSerializer)
        : base(applicationPaths, xmlSerializer)
    {
        Instance = this;
    }

    public static Plugin? Instance { get; private set; }

    public override string Name => "P-Stream";

    public override Guid Id => Guid.Parse("c1cfe730-699c-46a8-a299-8fae4482fb27");

    public IEnumerable<PluginPageInfo> GetPages()
    {
        return new[]
        {
            new PluginPageInfo
            {
                Name = "pstream",
                EmbeddedResourcePath = GetType().Namespace + ".Configuration.configPage.html",
            },
        };
    }
}

public class PluginServiceRegistrator : IPluginServiceRegistrator
{
    public void RegisterServices(IServiceCollection serviceCollection, IServerApplicationHost applicationHost)
    {
        serviceCollection.AddHttpClient<PStreamClient>();
        serviceCollection.AddSingleton<PStreamClient>();
        serviceCollection.AddSingleton<PStreamChannel>();
        serviceCollection.AddSingleton<IChannel>(sp => sp.GetRequiredService<PStreamChannel>());
    }
}
