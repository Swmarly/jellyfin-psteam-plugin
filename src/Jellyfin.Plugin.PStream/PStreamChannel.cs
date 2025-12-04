using System.Linq;
using MediaBrowser.Controller.Channels;
using MediaBrowser.Controller.Library;
using MediaBrowser.Controller.Providers;
using MediaBrowser.Model.Dto;
using MediaBrowser.Model.Entities;
using MediaBrowser.Model.MediaInfo;
using Microsoft.Extensions.Logging;
using ChannelEnums = MediaBrowser.Model.Channels;

namespace Jellyfin.Plugin.PStream;

public class PStreamChannel : IChannel, IHasCacheKey
{
    private readonly ILogger<PStreamChannel> _logger;
    private readonly PStreamClient _client;

    public PStreamChannel(ILogger<PStreamChannel> logger, PStreamClient client)
    {
        _logger = logger;
        _client = client;
    }

    public string Name => "P-Stream";

    public string Description => "Browse and play streams from the p-stream catalog.";

    public string DataVersion => Plugin.Instance?.Version.ToString() ?? "1.0.0";

    public string HomePageUrl => "https://server.fifthwit.net";

    public ChannelParentalRating ParentalRating => ChannelParentalRating.GeneralAudience;

    public string GetCacheKey(string? userId) => (userId ?? "anon") + "-pstream";

    public bool IsEnabledFor(string userId) => true;

    public async Task<ChannelItemResult> GetChannelItems(InternalChannelItemQuery query, CancellationToken cancellationToken)
    {
        var config = Plugin.Instance?.Configuration ?? new PluginConfiguration();
        _client.Configure(config.BaseUrl, config.ApiKey);

        // Jellyfin 10.9 no longer forwards an explicit SearchTerm, so reuse FolderId (set when searching) if present.
        var searchTerm = query.FolderId;
        IReadOnlyList<PStreamItem> items;

        if (string.IsNullOrWhiteSpace(searchTerm))
        {
            // fall back to a generic catalog fetch by asking for an empty search term
            items = await _client.SearchAsync(string.Empty, config.DefaultPageSize, cancellationToken).ConfigureAwait(false);
        }
        else
        {
            items = await _client.SearchAsync(searchTerm, config.DefaultPageSize, cancellationToken).ConfigureAwait(false);
        }

        return new ChannelItemResult
        {
            Items = items.Select(ToChannelItem).ToArray(),
            TotalRecordCount = items.Count,
        };
    }

    public async Task<IEnumerable<MediaSourceInfo>> GetChannelItemMediaSources(string id, CancellationToken cancellationToken)
    {
        var config = Plugin.Instance?.Configuration ?? new PluginConfiguration();
        _client.Configure(config.BaseUrl, config.ApiKey);

        var item = await _client.GetItemAsync(id, cancellationToken).ConfigureAwait(false);
        if (item == null || string.IsNullOrWhiteSpace(item.StreamUrl))
        {
            _logger.LogWarning("P-Stream item {Id} missing stream URL", id);
            return Array.Empty<MediaSourceInfo>();
        }

        var mediaSource = new MediaSourceInfo
        {
            Id = item.Id,
            Path = item.StreamUrl,
            Protocol = MediaProtocol.Http,
            Name = item.Title,
            MediaStreams = new List<MediaStream>
            {
                new()
                {
                    Type = MediaStreamType.Video,
                    IsInterlaced = false,
                    IsDefault = true,
                },
            },
        };

        if (item.Duration.HasValue)
        {
            mediaSource.RunTimeTicks = (long)item.Duration.Value.TotalSeconds * TimeSpan.TicksPerSecond;
        }

        return new[] { mediaSource };
    }

    public InternalChannelFeatures GetChannelFeatures()
    {
        return new InternalChannelFeatures
        {
            ContentTypes = new List<ChannelEnums.ChannelMediaContentType>
            {
                ChannelEnums.ChannelMediaContentType.Movie,
            },
            MediaTypes = new List<ChannelEnums.ChannelMediaType>
            {
                ChannelEnums.ChannelMediaType.Video,
            },
            DefaultSortFields = new List<ChannelEnums.ChannelItemSortField>
            {
                ChannelEnums.ChannelItemSortField.Name,
                ChannelEnums.ChannelItemSortField.PremiereDate,
            },
            SupportsSortOrderToggle = true,
            AutoRefreshLevels = 1,
            MaxPageSize = 50,
        };
    }

    public Task<DynamicImageResponse> GetChannelImage(ImageType type, CancellationToken cancellationToken)
    {
        var response = new DynamicImageResponse
        {
            Path = "https://raw.githubusercontent.com/Swmarly/jellyfin-psteam-plugin/main/icon.png",
            Protocol = MediaProtocol.Http,
            HasImage = true,
            Format = MediaBrowser.Model.Drawing.ImageFormat.Png,
        };

        return Task.FromResult(response);
    }

    public IEnumerable<ImageType> GetSupportedChannelImages()
    {
        return new[] { ImageType.Primary };
    }

    private ChannelItemInfo ToChannelItem(PStreamItem item)
    {
        return new ChannelItemInfo
        {
            Name = item.Title,
            Id = item.Id,
            ContentType = ChannelEnums.ChannelMediaContentType.Movie,
            MediaType = ChannelEnums.ChannelMediaType.Video,
            Overview = item.Description,
            ImageUrl = item.Poster,
            Type = ChannelItemType.Media,
        };
    }
}
