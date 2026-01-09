using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Xml;
using System.Net.Http;
using System.Net.Http.Headers;
using MediaBrowser.Common.Configuration;
using MediaBrowser.Common.Net;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Entities.TV;
using MediaBrowser.Controller.Entities.Movies;
using MediaBrowser.Controller.Providers;
using MediaBrowser.Model.Entities;
using MediaBrowser.Model.Providers;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace Jellyfin.Plugin.AniDB.Providers.AniDB.Metadata
{
    public class AniDbImageProvider : IRemoteImageProvider
    {
        public string Name => "AniDB";
        private readonly IApplicationPaths _appPaths;
        private readonly ILogger<AniDbImageProvider> _logger;

        public AniDbImageProvider(IApplicationPaths appPaths, ILogger<AniDbImageProvider> logger = null)
        {
            _appPaths = appPaths;
            _logger = logger ?? NullLogger<AniDbImageProvider>.Instance;
        }

        public async Task<HttpResponseMessage> GetImageResponse(string url, CancellationToken cancellationToken)
        {
            await AniDbSeriesProvider.RequestLimiter.Tick().ConfigureAwait(false);
            var httpClient = Plugin.Instance.GetHttpClient();

            return await httpClient.GetAsync(url).ConfigureAwait(false);
        }

        public Task<IEnumerable<RemoteImageInfo>> GetImages(BaseItem item, CancellationToken cancellationToken)
        {
            var seriesId = item.GetProviderId(ProviderNames.AniDb);
            return GetImages(seriesId, cancellationToken);
        }

        public async Task<IEnumerable<RemoteImageInfo>> GetImages(string aniDbId, CancellationToken cancellationToken)
        {
            var list = new List<RemoteImageInfo>();

            if (!string.IsNullOrEmpty(aniDbId) && 
                !(Plugin.Instance.BannedRecently && !AniDbSeriesProvider.HasExistingSeriesData(_appPaths, aniDbId)))
            {
                try
                {
                    var seriesDataPath = await AniDbSeriesProvider.GetSeriesData(_appPaths, aniDbId, cancellationToken);
                    var imageUrl = await FindImageUrl(seriesDataPath).ConfigureAwait(false);

                    if (!string.IsNullOrEmpty(imageUrl))
                    {
                        list.Add(new RemoteImageInfo
                        {
                            ProviderName = Name,
                            Url = imageUrl
                        });
                    }
                }
                catch (Exception ex)
                {
                    if (ex.Message != null && ex.Message.IndexOf("<error code=\"500\">banned</error>", StringComparison.Ordinal) >= 0)
                    {
                        Plugin.Instance.MarkBanned();
                    }
                    _logger.LogWarning(ex, "Failed to fetch AniDB image for {AniDbId}", aniDbId);
                }
            }

            return list;
        }

        public IEnumerable<ImageType> GetSupportedImages(BaseItem item)
        {
            return new[] { ImageType.Primary };
        }

        public bool Supports(BaseItem item)
        {
            return item is Series || item is Season || item is Movie;
        }

        private async Task<string> FindImageUrl(string seriesDataPath)
        {
            var settings = new XmlReaderSettings
            {
                Async = true,
                CheckCharacters = false,
                IgnoreProcessingInstructions = true,
                IgnoreComments = true,
                ValidationType = ValidationType.None
            };

            using (var streamReader = new StreamReader(seriesDataPath, Encoding.UTF8))
            {
                using (XmlReader reader = XmlReader.Create(streamReader, settings))
                {
                    await reader.MoveToContentAsync().ConfigureAwait(false);

                    while (await reader.ReadAsync().ConfigureAwait(false))
                    {
                        if (reader.NodeType == XmlNodeType.Element && reader.Name == "picture")
                        {
                            return "https://cdn.anidb.net/images/main/" + reader.ReadElementContentAsString();
                        }
                    }
                }
            }

            return null;
        }
    }
}
