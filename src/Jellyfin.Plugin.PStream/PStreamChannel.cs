using System.Linq;
using MediaBrowser.Controller.Channels;
using MediaBrowser.Controller.Library;
using MediaBrowser.Model.Channels;
using MediaBrowser.Model.Dto;
using MediaBrowser.Model.Entities;
using MediaBrowser.Model.MediaInfo;
using MediaBrowser.Model.Serialization;
using Microsoft.Extensions.Logging;

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

    public string GetCacheKey(string userId) => userId + "-pstream";

    public bool IsEnabledFor(string userId) => true;

    public async Task<ChannelItemResult> GetChannelItems(InternalChannelItemQuery query, CancellationToken cancellationToken)
    {
        var config = Plugin.Instance?.Configuration ?? new PluginConfiguration();
        _client.Configure(config.BaseUrl, config.ApiKey);

        var searchTerm = query.SearchTerm;
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

        var results = new ChannelItemResult
        {
            Items = items.Select(ToChannelItem).ToArray(),
            TotalRecordCount = items.Count,
        };

        return results;
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

    public ChannelFeatures GetChannelFeatures()
    {
        return new ChannelFeatures
        {
            ContentTypes = new List<ChannelMediaContentType>
            {
                ChannelMediaContentType.Movie,
                ChannelMediaContentType.Video,
            },
            MediaTypes = new List<ChannelMediaType>
            {
                ChannelMediaType.Video,
            },
            DefaultSortFields = new List<ChannelItemSortField>
            {
                ChannelItemSortField.Name,
                ChannelItemSortField.PremiereDate,
            },
            SupportsSortOrderToggle = true,
            SupportsSort = true,
            AutoRefreshLevels = 1,
            MaxPageSize = 50,
            CanSearch = true,
        };
    }

    private ChannelItemInfo ToChannelItem(PStreamItem item)
    {
        return new ChannelItemInfo
        {
            Name = item.Title,
            Id = item.Id,
            ContentType = ChannelMediaContentType.Video,
            MediaType = ChannelMediaType.Video,
            Overview = item.Description,
            ImageUrl = item.Poster,
            ImageContentType = ChannelMediaImageType.Thumbnail,
            Type = ChannelItemType.Media,
        };
    }
}
