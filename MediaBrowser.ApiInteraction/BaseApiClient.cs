﻿using MediaBrowser.Model.Drawing;
using MediaBrowser.Model.Dto;
using MediaBrowser.Model.Entities;
using MediaBrowser.Model.LiveTv;
using MediaBrowser.Model.Logging;
using MediaBrowser.Model.Querying;
using MediaBrowser.Model.Serialization;
using MediaBrowser.Model.Web;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace MediaBrowser.ApiInteraction
{
    /// <summary>
    /// Provides api methods that are usable on all platforms
    /// </summary>
    public abstract class BaseApiClient : IDisposable
    {
        public event EventHandler ServerLocationChanged;
        private void OnServerLocationChanged()
        {
            if (ServerLocationChanged != null)
            {
                ServerLocationChanged(this, EventArgs.Empty);
            }
        }

        /// <summary>
        /// Gets the logger.
        /// </summary>
        /// <value>The logger.</value>
        protected ILogger Logger { get; private set; }

        /// <summary>
        /// Gets the json serializer.
        /// </summary>
        /// <value>The json serializer.</value>
        public IJsonSerializer JsonSerializer { get; set; }

        /// <summary>
        ///  If specified this will be used as a default when an explicit value is not specified.
        /// </summary>
        public int? ImageQuality { get; set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="BaseApiClient" /> class.
        /// </summary>
        /// <param name="logger">The logger.</param>
        /// <param name="jsonSerializer">The json serializer.</param>
        /// <param name="serverHostName">Name of the server host.</param>
        /// <param name="serverApiPort">The server API port.</param>
        /// <param name="clientName">Name of the client.</param>
        /// <param name="deviceName">Name of the device.</param>
        /// <param name="deviceId">The device id.</param>
        /// <param name="applicationVersion">The application version.</param>
        /// <exception cref="System.ArgumentNullException">logger</exception>
        protected BaseApiClient(ILogger logger, IJsonSerializer jsonSerializer, string serverHostName, int serverApiPort, string clientName, string deviceName, string deviceId, string applicationVersion)
        {
            if (logger == null)
            {
                throw new ArgumentNullException("logger");
            }
            if (jsonSerializer == null)
            {
                throw new ArgumentNullException("jsonSerializer");
            }
            if (string.IsNullOrEmpty(serverHostName))
            {
                throw new ArgumentNullException("serverHostName");
            }

            JsonSerializer = jsonSerializer;
            Logger = logger;

            ServerHostName = serverHostName;
            ServerApiPort = serverApiPort;
            ClientName = clientName;
            DeviceName = deviceName;
            DeviceId = deviceId;
            ApplicationVersion = applicationVersion;
        }

        /// <summary>
        /// Gets the name of the server host.
        /// </summary>
        /// <value>The name of the server host.</value>
        public string ServerHostName { get; private set; }

        /// <summary>
        /// Gets the server API port.
        /// </summary>
        /// <value>The server API port.</value>
        public int ServerApiPort { get; private set; }

        /// <summary>
        /// Changes the server location.
        /// </summary>
        /// <param name="hostName">Name of the host.</param>
        /// <param name="port">The port.</param>
        public void ChangeServerLocation(string hostName, int port)
        {
            ServerHostName = hostName;
            ServerApiPort = port;

            OnServerLocationChanged();
        }

        /// <summary>
        /// Gets or sets the type of the client.
        /// </summary>
        /// <value>The type of the client.</value>
        public string ClientName { get; set; }

        /// <summary>
        /// Gets or sets the name of the device.
        /// </summary>
        /// <value>The name of the device.</value>
        public string DeviceName { get; set; }

        /// <summary>
        /// Gets or sets the application version.
        /// </summary>
        /// <value>The application version.</value>
        public string ApplicationVersion { get; set; }

        /// <summary>
        /// Gets or sets the device id.
        /// </summary>
        /// <value>The device id.</value>
        public string DeviceId { get; set; }

        /// <summary>
        /// The _current user id
        /// </summary>
        private string _currentUserId;

        /// <summary>
        /// Gets or sets the current user id.
        /// </summary>
        /// <value>The current user id.</value>
        public virtual string CurrentUserId
        {
            get { return _currentUserId; }
            set
            {
                _currentUserId = value;
                OnAuthorizationInfoChanged();
            }
        }

        /// <summary>
        /// Gets the current api url based on hostname and port.
        /// </summary>
        /// <value>The API URL.</value>
        protected string ApiUrl
        {
            get
            {
                return string.Format("http://{0}:{1}/mediabrowser", ServerHostName, ServerApiPort);
            }
        }

        /// <summary>
        /// Gets the name of the slug.
        /// </summary>
        /// <param name="name">The name.</param>
        /// <returns>System.String.</returns>
        protected string GetSlugName(string name)
        {
            return name.Replace('/', '-').Replace('?', '-');
        }

        /// <summary>
        /// Gets the name of the authorization scheme.
        /// </summary>
        /// <value>The name of the authorization scheme.</value>
        protected string AuthorizationScheme
        {
            get { return "MediaBrowser"; }
        }

        /// <summary>
        /// Gets the authorization header parameter.
        /// </summary>
        /// <value>The authorization header parameter.</value>
        protected string AuthorizationParameter
        {
            get
            {
                if (string.IsNullOrEmpty(ClientName) && string.IsNullOrEmpty(DeviceId) && string.IsNullOrEmpty(DeviceName))
                {
                    return string.Empty;
                }

                var header = string.Format("Client=\"{0}\", DeviceId=\"{1}\", Device=\"{2}\", Version=\"{3}\"", ClientName, DeviceId, DeviceName, ApplicationVersion);

                if (!string.IsNullOrEmpty(CurrentUserId))
                {
                    header += string.Format(", UserId=\"{0}\"", CurrentUserId);
                }

                return header;
            }
        }

        /// <summary>
        /// Gets the API URL.
        /// </summary>
        /// <param name="handler">The handler.</param>
        /// <returns>System.String.</returns>
        /// <exception cref="System.ArgumentNullException">handler</exception>
        public string GetApiUrl(string handler)
        {
            return GetApiUrl(handler, new QueryStringDictionary());
        }

        /// <summary>
        /// Called when [current user changed].
        /// </summary>
        protected virtual void OnAuthorizationInfoChanged()
        {

        }

        /// <summary>
        /// Gets the API URL.
        /// </summary>
        /// <param name="handler">The handler.</param>
        /// <param name="queryString">The query string.</param>
        /// <returns>System.String.</returns>
        /// <exception cref="System.ArgumentNullException">handler</exception>
        protected string GetApiUrl(string handler, QueryStringDictionary queryString)
        {
            if (string.IsNullOrEmpty(handler))
            {
                throw new ArgumentNullException("handler");
            }

            if (queryString == null)
            {
                throw new ArgumentNullException("queryString");
            }

            return queryString.GetUrl(ApiUrl + "/" + handler);
        }

        /// <summary>
        /// Creates a url to return a list of items
        /// </summary>
        /// <param name="query">The query.</param>
        /// <returns>System.String.</returns>
        /// <exception cref="System.ArgumentNullException">query</exception>
        protected string GetItemListUrl(ItemQuery query)
        {
            if (query == null)
            {
                throw new ArgumentNullException("query");
            }

            var dict = new QueryStringDictionary { };

            dict.AddIfNotNullOrEmpty("ParentId", query.ParentId);

            dict.AddIfNotNull("StartIndex", query.StartIndex);

            dict.AddIfNotNull("Limit", query.Limit);

            dict.AddIfNotNull("SortBy", query.SortBy);

            if (query.SortOrder.HasValue)
            {
                dict["sortOrder"] = query.SortOrder.ToString();
            }

            if (query.SeriesStatuses != null)
            {
                dict.Add("SeriesStatuses", query.SeriesStatuses.Select(f => f.ToString()));
            }

            if (query.Fields != null)
            {
                dict.Add("fields", query.Fields.Select(f => f.ToString()));
            }
            if (query.Filters != null)
            {
                dict.Add("Filters", query.Filters.Select(f => f.ToString()));
            }
            if (query.ImageTypes != null)
            {
                dict.Add("ImageTypes", query.ImageTypes.Select(f => f.ToString()));
            }

            dict.AddIfNotNull("Is3D", query.Is3D);
            if (query.VideoTypes != null)
            {
                dict.Add("VideoTypes", query.VideoTypes.Select(f => f.ToString()));
            }
            if (query.AirDays != null)
            {
                dict.Add("AirDays", query.AirDays.Select(f => f.ToString()));
            }

            dict.AddIfNotNullOrEmpty("MinOfficialRating", query.MinOfficialRating);
            dict.AddIfNotNullOrEmpty("MaxOfficialRating", query.MaxOfficialRating);

            dict.Add("recursive", query.Recursive);

            dict.AddIfNotNull("MinIndexNumber", query.MinIndexNumber);

            dict.AddIfNotNull("MediaTypes", query.MediaTypes);
            dict.AddIfNotNull("Genres", query.Genres, "|");
            dict.AddIfNotNull("Genres", query.AllGenres, "|");
            dict.AddIfNotNull("Ids", query.Ids);
            dict.AddIfNotNull("Studios", query.Studios, "|");
            dict.AddIfNotNull("ExcludeItemTypes", query.ExcludeItemTypes);
            dict.AddIfNotNull("IncludeItemTypes", query.IncludeItemTypes);
            dict.AddIfNotNull("Artists", query.Artists);

            dict.AddIfNotNull("IsPlayed", query.IsPlayed);
            dict.AddIfNotNull("IsInBoxSet", query.IsInBoxSet);
            
            dict.AddIfNotNullOrEmpty("Person", query.Person);
            dict.AddIfNotNull("PersonTypes", query.PersonTypes);

            dict.AddIfNotNull("Years", query.Years);

            dict.AddIfNotNull("ParentIndexNumber", query.ParentIndexNumber);
            dict.AddIfNotNull("IsHD", query.IsHD);
            dict.AddIfNotNull("HasParentalRating", query.HasParentalRating);
            
            dict.AddIfNotNullOrEmpty("SearchTerm", query.SearchTerm);

            dict.AddIfNotNull("MinCriticRating", query.MinCriticRating);
            dict.AddIfNotNull("MinCommunityRating", query.MinCommunityRating);
            
            dict.AddIfNotNull("MinPlayers", query.MinPlayers);
            dict.AddIfNotNull("MaxPlayers", query.MaxPlayers);
            dict.AddIfNotNullOrEmpty("NameStartsWithOrGreater", query.NameStartsWithOrGreater);
            dict.AddIfNotNullOrEmpty("AlbumArtistStartsWithOrGreater", query.AlbumArtistStartsWithOrGreater);

            if (query.LocationTypes != null && query.LocationTypes.Length > 0)
            {
                dict.Add("LocationTypes", query.LocationTypes.Select(f => f.ToString()));
            }
            if (query.ExcludeLocationTypes != null && query.ExcludeLocationTypes.Length > 0)
            {
                dict.Add("ExcludeLocationTypes", query.ExcludeLocationTypes.Select(f => f.ToString()));
            }

            dict.AddIfNotNull("IsMissing", query.IsMissing);
            dict.AddIfNotNull("IsUnaired", query.IsUnaired);
            dict.AddIfNotNull("IsVirtualUnaired", query.IsVirtualUnaired);

            dict.AddIfNotNull("AiredDuringSeason", query.AiredDuringSeason);
            
            return GetApiUrl("Users/" + query.UserId + "/Items", dict);
        }

        /// <summary>
        /// Gets the next up.
        /// </summary>
        /// <param name="query">The query.</param>
        /// <returns>System.String.</returns>
        /// <exception cref="System.ArgumentNullException">query</exception>
        protected string GetNextUpUrl(NextUpQuery query)
        {
            if (query == null)
            {
                throw new ArgumentNullException("query");
            }

            var dict = new QueryStringDictionary { };

            if (query.Fields != null)
            {
                dict.Add("fields", query.Fields.Select(f => f.ToString()));
            }

            dict.AddIfNotNull("Limit", query.Limit);

            dict.AddIfNotNull("StartIndex", query.StartIndex);

            dict.Add("UserId", query.UserId);

            return GetApiUrl("Shows/NextUp", dict);
        }
        
        /// <summary>
        /// Gets the similar item list URL.
        /// </summary>
        /// <param name="query">The query.</param>
        /// <param name="type">The type.</param>
        /// <returns>System.String.</returns>
        /// <exception cref="System.ArgumentNullException">
        /// query
        /// or
        /// type
        /// </exception>
        protected string GetSimilarItemListUrl(SimilarItemsQuery query, string type)
        {
            if (query == null)
            {
                throw new ArgumentNullException("query");
            }
            if (string.IsNullOrEmpty(type))
            {
                throw new ArgumentNullException("type");
            }

            var dict = new QueryStringDictionary { };

            dict.AddIfNotNull("Limit", query.Limit);
            dict.AddIfNotNullOrEmpty("UserId", query.UserId);

            if (query.Fields != null)
            {
                dict.Add("fields", query.Fields.Select(f => f.ToString()));
            }

            if (string.IsNullOrEmpty(query.Id))
            {
                throw new ArgumentNullException("query");
            }

            return GetApiUrl(type + "/" + query.Id + "/Similar", dict);
        }

        /// <summary>
        /// Gets the instant mix URL.
        /// </summary>
        /// <param name="query">The query.</param>
        /// <param name="type">The type.</param>
        /// <returns>System.String.</returns>
        /// <exception cref="System.ArgumentNullException">
        /// query
        /// or
        /// type
        /// </exception>
        protected string GetInstantMixUrl(SimilarItemsQuery query, string type)
        {
            if (query == null)
            {
                throw new ArgumentNullException("query");
            }
            if (string.IsNullOrEmpty(type))
            {
                throw new ArgumentNullException("type");
            }

            var dict = new QueryStringDictionary { };

            dict.AddIfNotNull("Limit", query.Limit);
            dict.AddIfNotNullOrEmpty("UserId", query.UserId);

            if (query.Fields != null)
            {
                dict.Add("fields", query.Fields.Select(f => f.ToString()));
            }

            if (string.IsNullOrEmpty(query.Id))
            {
                throw new ArgumentNullException("query");
            }

            return GetApiUrl(type + "/" + query.Id + "/InstantMix", dict);
        }

        /// <summary>
        /// Gets the instant mix by name URL.
        /// </summary>
        /// <param name="query">The query.</param>
        /// <param name="type">The type.</param>
        /// <returns>System.String.</returns>
        /// <exception cref="System.ArgumentNullException">
        /// query
        /// or
        /// type
        /// </exception>
        protected string GetInstantMixByNameUrl(SimilarItemsByNameQuery query, string type)
        {
            if (query == null)
            {
                throw new ArgumentNullException("query");
            }
            if (string.IsNullOrEmpty(type))
            {
                throw new ArgumentNullException("type");
            }

            var dict = new QueryStringDictionary { };

            dict.AddIfNotNull("Limit", query.Limit);
            dict.AddIfNotNullOrEmpty("UserId", query.UserId);

            if (query.Fields != null)
            {
                dict.Add("fields", query.Fields.Select(f => f.ToString()));
            }

            return GetApiUrl(type + "/" + GetSlugName(query.Name) + "/InstantMix", dict);
        }

        /// <summary>
        /// Gets the item by name list URL.
        /// </summary>
        /// <param name="type">The type.</param>
        /// <param name="query">The query.</param>
        /// <returns>System.String.</returns>
        /// <exception cref="System.ArgumentNullException">query</exception>
        protected string GetItemByNameListUrl(string type, ItemsByNameQuery query)
        {
            if (query == null)
            {
                throw new ArgumentNullException("query");
            }

            var dict = new QueryStringDictionary { };

            dict.AddIfNotNullOrEmpty("ParentId", query.ParentId);

            dict.Add("UserId", query.UserId);
            dict.AddIfNotNull("StartIndex", query.StartIndex);

            dict.AddIfNotNull("Limit", query.Limit);

            dict.AddIfNotNull("SortBy", query.SortBy);

            if (query.SortOrder.HasValue)
            {
                dict["sortOrder"] = query.SortOrder.ToString();
            }

            dict.AddIfNotNull("IsPlayed", query.IsPlayed);
            
            if (query.Fields != null)
            {
                dict.Add("fields", query.Fields.Select(f => f.ToString()));
            }

            if (query.Filters != null)
            {
                dict.Add("Filters", query.Filters.Select(f => f.ToString()));
            }
            
            if (query.ImageTypes != null)
            {
                dict.Add("ImageTypes", query.ImageTypes.Select(f => f.ToString()));
            }
            
            dict.Add("recursive", query.Recursive);

            dict.AddIfNotNull("MediaTypes", query.MediaTypes);
            dict.AddIfNotNull("ExcludeItemTypes", query.ExcludeItemTypes);
            dict.AddIfNotNull("IncludeItemTypes", query.IncludeItemTypes);

            dict.AddIfNotNullOrEmpty("NameLessThan", query.NameLessThan);
            dict.AddIfNotNullOrEmpty("NameStartsWithOrGreater", query.NameStartsWithOrGreater);

            return GetApiUrl(type, dict);
        }

        /// <summary>
        /// Gets the image URL.
        /// </summary>
        /// <param name="baseUrl">The base URL.</param>
        /// <param name="options">The options.</param>
        /// <param name="queryParams">The query params.</param>
        /// <returns>System.String.</returns>
        /// <exception cref="System.ArgumentNullException">options</exception>
        private string GetImageUrl(string baseUrl, ImageOptions options, QueryStringDictionary queryParams)
        {
            if (options == null)
            {
                throw new ArgumentNullException("options");
            }

            if (queryParams == null)
            {
                throw new ArgumentNullException("queryParams");
            }

            if (options.ImageIndex.HasValue)
            {
                baseUrl += "/" + options.ImageIndex.Value;
            }

            queryParams.AddIfNotNull("Width", options.Width);
            queryParams.AddIfNotNull("Height", options.Height);
            queryParams.AddIfNotNull("MaxWidth", options.MaxWidth);
            queryParams.AddIfNotNull("MaxHeight", options.MaxHeight);
            queryParams.AddIfNotNull("Quality", options.Quality ?? ImageQuality);

            queryParams.AddIfNotNull("Tag", options.Tag);

            queryParams.AddIfNotNull("CropWhitespace", options.CropWhitespace);
            queryParams.Add("EnableImageEnhancers", options.EnableImageEnhancers);

            if (options.Format != ImageOutputFormat.Original)
            {
                queryParams.Add("Format", options.Format.ToString());
            }

            if (options.AddPlayedIndicator)
            {
                queryParams.Add("AddPlayedIndicator", true);
            }
            queryParams.AddIfNotNull("PercentPlayed", options.PercentPlayed);
            queryParams.AddIfNotNullOrEmpty("BackgroundColor", options.BackgroundColor);

            return GetApiUrl(baseUrl, queryParams);
        }

        /// <summary>
        /// Gets the image URL.
        /// </summary>
        /// <param name="item">The item.</param>
        /// <param name="options">The options.</param>
        /// <returns>System.String.</returns>
        /// <exception cref="System.ArgumentNullException">item</exception>
        public string GetImageUrl(BaseItemDto item, ImageOptions options)
        {
            if (item == null)
            {
                throw new ArgumentNullException("item");
            }

            if (options == null)
            {
                throw new ArgumentNullException("options");
            }

            options.Tag = GetImageTag(item, options);

            return GetImageUrl(item.Id, options);
        }

        public string GetImageUrl(RecordingInfoDto item, ImageOptions options)
        {
            if (item == null)
            {
                throw new ArgumentNullException("item");
            }

            if (options == null)
            {
                throw new ArgumentNullException("options");
            }

            options.Tag = item.ImageTags[options.ImageType];

            return GetImageUrl(item.Id, options);
        }

        public string GetImageUrl(ChannelInfoDto item, ImageOptions options)
        {
            if (item == null)
            {
                throw new ArgumentNullException("item");
            }

            if (options == null)
            {
                throw new ArgumentNullException("options");
            }

            options.Tag = item.ImageTags[options.ImageType];

            return GetImageUrl(item.Id, options);
        }

        public string GetImageUrl(ProgramInfoDto item, ImageOptions options)
        {
            if (item == null)
            {
                throw new ArgumentNullException("item");
            }

            if (options == null)
            {
                throw new ArgumentNullException("options");
            }

            options.Tag = item.ImageTags[options.ImageType];

            return GetImageUrl(item.Id, options);
        }

        /// <summary>
        /// Gets an image url that can be used to download an image from the api
        /// </summary>
        /// <param name="itemId">The Id of the item</param>
        /// <param name="options">The options.</param>
        /// <returns>System.String.</returns>
        /// <exception cref="System.ArgumentNullException">itemId</exception>
        public string GetImageUrl(string itemId, ImageOptions options)
        {
            if (string.IsNullOrEmpty(itemId))
            {
                throw new ArgumentNullException("itemId");
            }

            var url = "Items/" + itemId + "/Images/" + options.ImageType;

            return GetImageUrl(url, options, new QueryStringDictionary());
        }

        /// <summary>
        /// Gets the user image URL.
        /// </summary>
        /// <param name="user">The user.</param>
        /// <param name="options">The options.</param>
        /// <returns>System.String.</returns>
        /// <exception cref="System.ArgumentNullException">user</exception>
        public string GetUserImageUrl(UserDto user, ImageOptions options)
        {
            if (user == null)
            {
                throw new ArgumentNullException("user");
            }

            if (options == null)
            {
                throw new ArgumentNullException("options");
            }

            options.Tag = user.PrimaryImageTag;

            return GetUserImageUrl(user.Id, options);
        }

        /// <summary>
        /// Gets an image url that can be used to download an image from the api
        /// </summary>
        /// <param name="userId">The Id of the user</param>
        /// <param name="options">The options.</param>
        /// <returns>System.String.</returns>
        /// <exception cref="System.ArgumentNullException">userId</exception>
        public string GetUserImageUrl(string userId, ImageOptions options)
        {
            if (string.IsNullOrEmpty(userId))
            {
                throw new ArgumentNullException("userId");
            }

            var url = "Users/" + userId + "/Images/" + options.ImageType;

            return GetImageUrl(url, options, new QueryStringDictionary());
        }

        /// <summary>
        /// Gets the person image URL.
        /// </summary>
        /// <param name="item">The item.</param>
        /// <param name="options">The options.</param>
        /// <returns>System.String.</returns>
        /// <exception cref="System.ArgumentNullException">item</exception>
        public string GetPersonImageUrl(BaseItemPerson item, ImageOptions options)
        {
            if (item == null)
            {
                throw new ArgumentNullException("item");
            }

            if (options == null)
            {
                throw new ArgumentNullException("options");
            }

            options.Tag = item.PrimaryImageTag;

            return GetPersonImageUrl(item.Name, options);
        }

        /// <summary>
        /// Gets the image tag.
        /// </summary>
        /// <param name="item">The item.</param>
        /// <param name="options">The options.</param>
        /// <returns>Guid.</returns>
        private Guid GetImageTag(BaseItemDto item, ImageOptions options)
        {
            if (options.ImageType == ImageType.Backdrop)
            {
                return item.BackdropImageTags[options.ImageIndex ?? 0];
            }

            if (options.ImageType == ImageType.Screenshot)
            {
                //return item.scree[options.ImageIndex ?? 0];
            }

            if (options.ImageType == ImageType.Chapter)
            {
                return item.Chapters[options.ImageIndex ?? 0].ImageTag.Value;
            }

            return item.ImageTags[options.ImageType];
        }

        /// <summary>
        /// Gets an image url that can be used to download an image from the api
        /// </summary>
        /// <param name="name">The name of the person</param>
        /// <param name="options">The options.</param>
        /// <returns>System.String.</returns>
        /// <exception cref="System.ArgumentNullException">name</exception>
        public string GetPersonImageUrl(string name, ImageOptions options)
        {
            if (string.IsNullOrEmpty(name))
            {
                throw new ArgumentNullException("name");
            }

            var url = "Persons/" + GetSlugName(name) + "/Images/" + options.ImageType;

            return GetImageUrl(url, options, new QueryStringDictionary());
        }

        /// <summary>
        /// Gets an image url that can be used to download an image from the api
        /// </summary>
        /// <param name="year">The year.</param>
        /// <param name="options">The options.</param>
        /// <returns>System.String.</returns>
        public string GetYearImageUrl(int year, ImageOptions options)
        {
            var url = "Years/" + year + "/Images/" + options.ImageType;

            return GetImageUrl(url, options, new QueryStringDictionary());
        }

        /// <summary>
        /// Gets an image url that can be used to download an image from the api
        /// </summary>
        /// <param name="name">The name.</param>
        /// <param name="options">The options.</param>
        /// <returns>System.String.</returns>
        /// <exception cref="System.ArgumentNullException">name</exception>
        public string GetGenreImageUrl(string name, ImageOptions options)
        {
            if (string.IsNullOrEmpty(name))
            {
                throw new ArgumentNullException("name");
            }

            var url = "Genres/" + GetSlugName(name) + "/Images/" + options.ImageType;

            return GetImageUrl(url, options, new QueryStringDictionary());
        }

        /// <summary>
        /// Gets the music genre image URL.
        /// </summary>
        /// <param name="name">The name.</param>
        /// <param name="options">The options.</param>
        /// <returns>System.String.</returns>
        /// <exception cref="System.ArgumentNullException">name</exception>
        public string GetMusicGenreImageUrl(string name, ImageOptions options)
        {
            if (string.IsNullOrEmpty(name))
            {
                throw new ArgumentNullException("name");
            }

            var url = "MusicGenres/" + GetSlugName(name) + "/Images/" + options.ImageType;

            return GetImageUrl(url, options, new QueryStringDictionary());
        }

        /// <summary>
        /// Gets the game genre image URL.
        /// </summary>
        /// <param name="name">The name.</param>
        /// <param name="options">The options.</param>
        /// <returns>System.String.</returns>
        /// <exception cref="System.ArgumentNullException">name</exception>
        public string GetGameGenreImageUrl(string name, ImageOptions options)
        {
            if (string.IsNullOrEmpty(name))
            {
                throw new ArgumentNullException("name");
            }

            var url = "GameGenres/" + GetSlugName(name) + "/Images/" + options.ImageType;

            return GetImageUrl(url, options, new QueryStringDictionary());
        }

        /// <summary>
        /// Gets an image url that can be used to download an image from the api
        /// </summary>
        /// <param name="name">The name.</param>
        /// <param name="options">The options.</param>
        /// <returns>System.String.</returns>
        /// <exception cref="System.ArgumentNullException">name</exception>
        public string GetStudioImageUrl(string name, ImageOptions options)
        {
            if (string.IsNullOrEmpty(name))
            {
                throw new ArgumentNullException("name");
            }

            var url = "Studios/" + GetSlugName(name) + "/Images/" + options.ImageType;

            return GetImageUrl(url, options, new QueryStringDictionary());
        }

        /// <summary>
        /// Gets the artist image URL.
        /// </summary>
        /// <param name="name">The name.</param>
        /// <param name="options">The options.</param>
        /// <returns>System.String.</returns>
        /// <exception cref="System.ArgumentNullException">name</exception>
        public string GetArtistImageUrl(string name, ImageOptions options)
        {
            if (string.IsNullOrEmpty(name))
            {
                throw new ArgumentNullException("name");
            }

            var url = "Artists/" + GetSlugName(name) + "/Images/" + options.ImageType;

            return GetImageUrl(url, options, new QueryStringDictionary());
        }

        /// <summary>
        /// This is a helper to get a list of backdrop url's from a given ApiBaseItemWrapper. If the actual item does not have any backdrops it will return backdrops from the first parent that does.
        /// </summary>
        /// <param name="item">A given item.</param>
        /// <param name="options">The options.</param>
        /// <returns>System.String[][].</returns>
        /// <exception cref="System.ArgumentNullException">item</exception>
        public string[] GetBackdropImageUrls(BaseItemDto item, ImageOptions options)
        {
            if (item == null)
            {
                throw new ArgumentNullException("item");
            }

            if (options == null)
            {
                throw new ArgumentNullException("options");
            }

            options.ImageType = ImageType.Backdrop;

            string backdropItemId;
            List<Guid> backdropImageTags;

            if (item.BackdropCount == 0)
            {
                backdropItemId = item.ParentBackdropItemId;
                backdropImageTags = item.ParentBackdropImageTags;
            }
            else
            {
                backdropItemId = item.Id;
                backdropImageTags = item.BackdropImageTags;
            }

            if (string.IsNullOrEmpty(backdropItemId))
            {
                return new string[] { };
            }

            var files = new string[backdropImageTags.Count];

            for (var i = 0; i < backdropImageTags.Count; i++)
            {
                options.ImageIndex = i;
                options.Tag = backdropImageTags[i];

                files[i] = GetImageUrl(backdropItemId, options);
            }

            return files;
        }

        /// <summary>
        /// This is a helper to get the logo image url from a given ApiBaseItemWrapper. If the actual item does not have a logo, it will return the logo from the first parent that does, or null.
        /// </summary>
        /// <param name="item">A given item.</param>
        /// <param name="options">The options.</param>
        /// <returns>System.String.</returns>
        /// <exception cref="System.ArgumentNullException">item</exception>
        public string GetLogoImageUrl(BaseItemDto item, ImageOptions options)
        {
            if (item == null)
            {
                throw new ArgumentNullException("item");
            }

            if (options == null)
            {
                throw new ArgumentNullException("options");
            }

            options.ImageType = ImageType.Logo;

            var logoItemId = item.HasLogo ? item.Id : item.ParentLogoItemId;
            var imageTag = item.HasLogo ? item.ImageTags[ImageType.Logo] : item.ParentLogoImageTag;

            if (!string.IsNullOrEmpty(logoItemId))
            {
                options.Tag = imageTag;

                return GetImageUrl(logoItemId, options);
            }

            return null;
        }

        public string GetThumbImageUrl(BaseItemDto item, ImageOptions options)
        {
            if (item == null)
            {
                throw new ArgumentNullException("item");
            }

            if (options == null)
            {
                throw new ArgumentNullException("options");
            }

            options.ImageType = ImageType.Thumb;

            var itemId = item.HasThumb ? item.Id : item.SeriesThumbImageTag.HasValue ? item.SeriesId : item.ParentThumbItemId;
            var imageTag = item.HasThumb ? item.ImageTags[ImageType.Thumb] : item.SeriesThumbImageTag ?? item.ParentThumbImageTag;

            if (!string.IsNullOrEmpty(itemId))
            {
                options.Tag = imageTag;

                return GetImageUrl(itemId, options);
            }

            return null;
        }

        /// <summary>
        /// This is a helper to get the art image url from a given BaseItemDto. If the actual item does not have a logo, it will return the logo from the first parent that does, or null.
        /// </summary>
        /// <param name="item">A given item.</param>
        /// <param name="options">The options.</param>
        /// <returns>System.String.</returns>
        /// <exception cref="System.ArgumentNullException">item</exception>
        public string GetArtImageUrl(BaseItemDto item, ImageOptions options)
        {
            if (item == null)
            {
                throw new ArgumentNullException("item");
            }

            if (options == null)
            {
                throw new ArgumentNullException("options");
            }

            options.ImageType = ImageType.Art;

            var artItemId = item.HasArtImage ? item.Id : item.ParentArtItemId;
            var imageTag = item.HasArtImage ? item.ImageTags[ImageType.Art] : item.ParentArtImageTag;

            if (!string.IsNullOrEmpty(artItemId))
            {
                options.Tag = imageTag;

                return GetImageUrl(artItemId, options);
            }

            return null;
        }

        /// <summary>
        /// Gets the url needed to stream an audio file
        /// </summary>
        /// <param name="options">The options.</param>
        /// <returns>System.String.</returns>
        /// <exception cref="System.ArgumentNullException">options</exception>
        public string GetAudioStreamUrl(StreamOptions options)
        {
            if (options == null)
            {
                throw new ArgumentNullException("options");
            }

            var handler = "Audio/" + options.ItemId + "/stream";

            if (!string.IsNullOrEmpty(options.OutputFileExtension))
            {
                handler += "." + options.OutputFileExtension.TrimStart('.');
            }

            return GetMediaStreamUrl(handler, options, new QueryStringDictionary());
        }

        /// <summary>
        /// Gets the url needed to stream a video file
        /// </summary>
        /// <param name="options">The options.</param>
        /// <returns>System.String.</returns>
        /// <exception cref="System.ArgumentNullException">options</exception>
        public string GetVideoStreamUrl(VideoStreamOptions options)
        {
            if (options == null)
            {
                throw new ArgumentNullException("options");
            }

            var handler = "Videos/" + options.ItemId + "/stream";

            if (!string.IsNullOrEmpty(options.OutputFileExtension))
            {
                handler += "." + options.OutputFileExtension.TrimStart('.');
            }

            return GetVideoStreamUrl(handler, options);
        }

        /// <summary>
        /// Formulates a url for streaming audio using the HLS protocol
        /// </summary>
        /// <param name="options">The options.</param>
        /// <returns>System.String.</returns>
        /// <exception cref="System.ArgumentNullException">options</exception>
        public string GetHlsAudioStreamUrl(StreamOptions options)
        {
            if (options == null)
            {
                throw new ArgumentNullException("options");
            }

            return GetMediaStreamUrl("audio.m3u8", options, new QueryStringDictionary());
        }

        /// <summary>
        /// Formulates a url for streaming video using the HLS protocol
        /// </summary>
        /// <param name="options">The options.</param>
        /// <returns>System.String.</returns>
        /// <exception cref="System.ArgumentNullException">options</exception>
        public string GetHlsVideoStreamUrl(VideoStreamOptions options)
        {
            if (options == null)
            {
                throw new ArgumentNullException("options");
            }

            return GetVideoStreamUrl("video.m3u8", options);
        }

        /// <summary>
        /// Gets the video stream URL.
        /// </summary>
        /// <param name="handler">The handler.</param>
        /// <param name="options">The options.</param>
        /// <returns>System.String.</returns>
        private string GetVideoStreamUrl(string handler, VideoStreamOptions options)
        {
            var queryParams = new QueryStringDictionary();

            if (options.VideoCodec.HasValue)
            {
                queryParams["VideoCodec"] = options.VideoCodec.Value.ToString();
            }

            queryParams.AddIfNotNull("VideoBitRate", options.VideoBitRate);
            queryParams.AddIfNotNull("Width", options.Width);
            queryParams.AddIfNotNull("Height", options.Height);
            queryParams.AddIfNotNull("MaxWidth", options.MaxWidth);
            queryParams.AddIfNotNull("MaxHeight", options.MaxHeight);
            queryParams.AddIfNotNull("FrameRate", options.FrameRate);
            queryParams.AddIfNotNull("AudioStreamIndex", options.AudioStreamIndex);
            queryParams.AddIfNotNull("VideoStreamIndex", options.VideoStreamIndex);
            queryParams.AddIfNotNull("SubtitleStreamIndex", options.SubtitleStreamIndex);

            queryParams.AddIfNotNullOrEmpty("Profile", options.Profile);
            queryParams.AddIfNotNullOrEmpty("Level", options.Level);

            queryParams.AddIfNotNull("TimeStampOffsetMs", options.TimeStampOffsetMs);

            return GetMediaStreamUrl(handler, options, queryParams);
        }

        /// <summary>
        /// Gets the media stream URL.
        /// </summary>
        /// <param name="handler">The handler.</param>
        /// <param name="options">The options.</param>
        /// <param name="queryParams">The query params.</param>
        /// <returns>System.String.</returns>
        /// <exception cref="System.ArgumentNullException">handler</exception>
        private string GetMediaStreamUrl(string handler, StreamOptions options, QueryStringDictionary queryParams)
        {
            if (string.IsNullOrEmpty(handler))
            {
                throw new ArgumentNullException("handler");
            }

            if (options == null)
            {
                throw new ArgumentNullException("options");
            }

            if (queryParams == null)
            {
                throw new ArgumentNullException("queryParams");
            }

            if (options.AudioCodec.HasValue)
            {
                queryParams["audioCodec"] = options.AudioCodec.Value.ToString();
            }

            queryParams.AddIfNotNull("audiochannels", options.MaxAudioChannels);
            queryParams.AddIfNotNull("audiosamplerate", options.MaxAudioSampleRate);
            queryParams.AddIfNotNull("AudioBitRate", options.AudioBitRate);
            queryParams.AddIfNotNull("StartTimeTicks", options.StartTimeTicks);
            queryParams.AddIfNotNull("Static", options.Static);

            return GetApiUrl(handler, queryParams);
        }

        /// <summary>
        /// Deserializes from stream.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="stream">The stream.</param>
        /// <returns>``0.</returns>
        protected T DeserializeFromStream<T>(Stream stream)
            where T : class
        {
            return (T)DeserializeFromStream(stream, typeof(T));
        }

        /// <summary>
        /// Deserializes from stream.
        /// </summary>
        /// <param name="stream">The stream.</param>
        /// <param name="type">The type.</param>
        /// <returns>System.Object.</returns>
        /// <exception cref="System.NotImplementedException"></exception>
        protected object DeserializeFromStream(Stream stream, Type type)
        {
            return JsonSerializer.DeserializeFromStream(stream, type);
        }

        /// <summary>
        /// Serializers to json.
        /// </summary>
        /// <param name="obj">The obj.</param>
        /// <returns>System.String.</returns>
        protected string SerializeToJson(object obj)
        {
            return JsonSerializer.SerializeToString(obj);
        }

        /// <summary>
        /// Adds the data format.
        /// </summary>
        /// <param name="url">The URL.</param>
        /// <returns>System.String.</returns>
        protected string AddDataFormat(string url)
        {
            const string format = "json";

            if (url.IndexOf('?') == -1)
            {
                url += "?format=" + format;
            }
            else
            {
                url += "&format=" + format;
            }

            return url;
        }

        /// <summary>
        /// Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.
        /// </summary>
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// Releases unmanaged and - optionally - managed resources.
        /// </summary>
        /// <param name="disposing"><c>true</c> to release both managed and unmanaged resources; <c>false</c> to release only unmanaged resources.</param>
        protected virtual void Dispose(bool disposing)
        {

        }
    }
}
