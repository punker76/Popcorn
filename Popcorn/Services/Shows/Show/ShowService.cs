using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Configuration;
using System.Data;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using NLog;
using Popcorn.Models.Genres;
using Popcorn.Models.Shows;
using Popcorn.Models.User;
using System.Linq;
using GalaSoft.MvvmLight.Ioc;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Polly;
using Polly.Timeout;
using Popcorn.Database;
using Popcorn.Services.Tmdb;
using Popcorn.Exceptions;
using Popcorn.Helpers;
using Popcorn.Models.Episode;
using Popcorn.Models.Image;
using Popcorn.Models.Rating;
using Popcorn.Models.Torrent.Show;
using Popcorn.ViewModels.Pages.Home.Settings.ApplicationSettings;
using VideoLibrary;

namespace Popcorn.Services.Shows.Show
{
    public class ShowService : IShowService
    {
        /// <summary>
        /// Logger of the class
        /// </summary>
        private static Logger Logger { get; } = LogManager.GetCurrentClassLogger();

        private readonly ITmdbService _tmdbService;

        /// <summary>
        /// Change the culture of TMDb
        /// </summary>
        /// <param name="language">Language to set</param>
        public async Task ChangeTmdbLanguage(Language language)
        {
            (await _tmdbService.GetClient).DefaultLanguage = language.Culture;
        }

        /// <summary>
        /// Constructor
        /// </summary>
        public ShowService(ITmdbService tmdbService)
        {
            _tmdbService = tmdbService;
        }

        /// <summary>
        /// Get show by its Imdb code
        /// </summary>
        /// <param name="imdbId">Show's Imdb code</param>
        /// <param name="ct">Cancellation token</param>
        /// <returns>The show</returns>
        public async Task<ShowJson> GetShowAsync(string imdbId, CancellationToken ct)
        {
            var timeoutPolicy =
                Policy.TimeoutAsync(Constants.DefaultRequestTimeoutInSecond, TimeoutStrategy.Optimistic);
            try
            {
                return await timeoutPolicy.ExecuteAsync(async cancellation =>
                {
                    var optionsBuilder = new DbContextOptionsBuilder<PopcornContext>();
                    optionsBuilder.UseSqlServer(
                        (ConfigurationManager.GetSection("settings") as NameValueCollection)["SQLConnectionString"],
                        builder =>
                        {
                            builder.CommandTimeout(Convert.ToInt32(TimeSpan.FromSeconds(60).TotalSeconds));
                            builder.EnableRetryOnFailure();
                        });
                    await using var context = new PopcornContext(optionsBuilder.Options);

                    var watch = Stopwatch.StartNew();
                    var showJson = new ShowJson();
                    try
                    {
                        var show = await context.ShowSet.Include(a => a.Rating)
                            .Include(a => a.Episodes)
                            .ThenInclude(episode => episode.Torrents)
                            .ThenInclude(torrent => torrent.Torrent0)
                            .Include(a => a.Episodes)
                            .ThenInclude(episode => episode.Torrents)
                            .ThenInclude(torrent => torrent.Torrent1080p)
                            .Include(a => a.Episodes)
                            .ThenInclude(episode => episode.Torrents)
                            .ThenInclude(torrent => torrent.Torrent480p)
                            .Include(a => a.Episodes)
                            .Include(a => a.Episodes)
                            .ThenInclude(episode => episode.Torrents)
                            .ThenInclude(torrent => torrent.Torrent720p)
                            .Include(a => a.Genres)
                            .Include(a => a.Images)
                            .Include(a => a.Similars).AsQueryable()
                            .FirstOrDefaultAsync(a => a.ImdbId == imdbId, cancellation);
                        showJson = ConvertShowToJson(show);
                        var shows = await (await _tmdbService.GetClient).SearchTvShowAsync(show.Title,
                            cancellationToken: cancellation);
                        if (shows.Results.Any())
                        {
                            foreach (var tvShow in shows.Results)
                            {
                                try
                                {
                                    var result =
                                        await (await _tmdbService.GetClient).GetTvShowExternalIdsAsync(tvShow.Id,
                                            cancellation);
                                    if (result.ImdbId == show.ImdbId)
                                    {
                                        showJson.TmdbId = result.Id;
                                    }
                                }
                                catch (Exception ex)
                                {
                                    Logger.Error(ex);
                                }
                            }
                        }
                    }
                    catch (Exception exception) when (exception is TaskCanceledException)
                    {
                        Logger.Debug(
                            "GetShowAsync cancelled.");
                    }
                    catch (Exception exception)
                    {
                        Logger.Error(
                            $"GetShowAsync: {exception.Message}");
                        throw;
                    }
                    finally
                    {
                        watch.Stop();
                        var elapsedMs = watch.ElapsedMilliseconds;
                        Logger.Trace(
                            $"GetShowAsync ({imdbId}) in {elapsedMs} milliseconds.");
                    }

                    return showJson;
                }, ct);
            }
            catch (Exception ex)
            {
                Logger.Error(ex);
                throw;
            }
        }

        /// <summary>
        /// Get show light by its Imdb code
        /// </summary>
        /// <param name="imdbId">Show's Imdb code</param>
        /// <param name="ct">Cancellation token</param>
        /// <returns>The show</returns>
        public async Task<ShowLightJson> GetShowLightAsync(string imdbId, CancellationToken ct)
        {
            var timeoutPolicy =
                Policy.TimeoutAsync(Constants.DefaultRequestTimeoutInSecond, TimeoutStrategy.Optimistic);
            try
            {
                return await timeoutPolicy.ExecuteAsync(async cancellation =>
                {
                    var optionsBuilder = new DbContextOptionsBuilder<PopcornContext>();
                    optionsBuilder.UseSqlServer(
                        (ConfigurationManager.GetSection("settings") as NameValueCollection)["SQLConnectionString"],
                        builder =>
                        {
                            builder.CommandTimeout(Convert.ToInt32(TimeSpan.FromSeconds(60).TotalSeconds));
                            builder.EnableRetryOnFailure();
                        });
                    await using var context = new PopcornContext(optionsBuilder.Options);

                    var watch = Stopwatch.StartNew();
                    var imdbParameter = new SqlParameter("@imdbId", imdbId);
                    var query = @"
                    SELECT 
                        Show.Title, Show.Year, Rating.Percentage, Rating.Loved, Rating.Votes, Rating.Hated, Rating.Watching, Show.LastUpdated, Image.Banner, Image.Poster, Show.ImdbId, Show.TvdbId, Show.GenreNames
                    FROM 
                        ShowSet AS Show
                    INNER JOIN 
                        ImageShowSet AS Image
                    ON 
                        Image.Id = Show.ImagesId
                    INNER JOIN 
                        RatingSet AS Rating
                    ON 
                        Rating.Id = Show.RatingId
                    WHERE
                        Show.ImdbId = @imdbId";

                    var show = new ShowLightJson();

                    try
                    {
                        await using var command = context.Database.GetDbConnection().CreateCommand();
                        command.CommandText = query;
                        command.CommandType = CommandType.Text;
                        command.Parameters.Add(imdbParameter);
                        await context.Database.OpenConnectionAsync(cancellation);
                        await using var reader = await command.ExecuteReaderAsync(cancellation);
                        while (await reader.ReadAsync(cancellation))
                        {
                            show.Title = !await reader.IsDBNullAsync(0, cancellation)
                                ? reader.GetString(0)
                                : string.Empty;
                            show.Year = !await reader.IsDBNullAsync(1, cancellation) ? reader.GetInt32(1) : 0;
                            show.Rating = new RatingJson
                            {
                                Percentage = !await reader.IsDBNullAsync(2, cancellation) ? reader.GetInt32(2) : 0,
                                Loved = !await reader.IsDBNullAsync(3, cancellation) ? reader.GetInt32(3) : 0,
                                Votes = !await reader.IsDBNullAsync(4, cancellation) ? reader.GetInt32(4) : 0,
                                Hated = !await reader.IsDBNullAsync(5, cancellation) ? reader.GetInt32(5) : 0,
                                Watching = !await reader.IsDBNullAsync(6, cancellation) ? reader.GetInt32(6) : 0
                            };
                            show.Images = new ImageShowJson
                            {
                                Banner = !await reader.IsDBNullAsync(8, cancellation)
                                    ? reader.GetString(8)
                                    : string.Empty,
                                Poster = !await reader.IsDBNullAsync(9, cancellation)
                                    ? reader.GetString(9)
                                    : string.Empty,
                            };
                            show.ImdbId = !await reader.IsDBNullAsync(10, cancellation)
                                ? reader.GetString(10)
                                : string.Empty;
                            show.TvdbId = !await reader.IsDBNullAsync(11, cancellation)
                                ? reader.GetString(11)
                                : string.Empty;
                            show.Genres = !await reader.IsDBNullAsync(12, cancellation)
                                ? reader.GetString(12)
                                : string.Empty;
                        }
                    }
                    catch (Exception exception) when (exception is TaskCanceledException)
                    {
                        Logger.Debug(
                            "GetShowLightAsync cancelled.");
                    }
                    catch (Exception exception)
                    {
                        Logger.Error(
                            $"GetShowLightAsync: {exception.Message}");
                        throw;
                    }
                    finally
                    {
                        watch.Stop();
                        var elapsedMs = watch.ElapsedMilliseconds;
                        Logger.Trace(
                            $"GetShowLightAsync ({imdbId}) in {elapsedMs} milliseconds.");
                    }

                    return show;
                }, ct);
            }
            catch (Exception ex)
            {
                Logger.Error(ex);
                throw;
            }
        }

        /// <summary>
        /// Get shows by ids
        /// </summary>
        /// <param name="imdbIds">The imdbIds of the shows, split by comma</param>
        /// <param name="ct">Cancellation token</param>
        /// <returns>Shows</returns>
        public async Task<(IEnumerable<ShowLightJson> movies, int nbMovies)> GetShowsByIds(IList<string> imdbIds,
            CancellationToken ct)
        {
            var timeoutPolicy =
                Policy.TimeoutAsync(Constants.DefaultRequestTimeoutInSecond, TimeoutStrategy.Optimistic);
            try
            {
                return await timeoutPolicy.ExecuteAsync(async cancellation =>
                {
                    var optionsBuilder = new DbContextOptionsBuilder<PopcornContext>();
                    optionsBuilder.UseSqlServer(
                        (ConfigurationManager.GetSection("settings") as NameValueCollection)["SQLConnectionString"],
                        builder =>
                        {
                            builder.CommandTimeout(Convert.ToInt32(TimeSpan.FromSeconds(60).TotalSeconds));
                            builder.EnableRetryOnFailure();
                        });
                    await using var context = new PopcornContext(optionsBuilder.Options);

                    var watch = Stopwatch.StartNew();
                    var query = @"
                    SELECT DISTINCT
                        Show.Title, Show.Year, Rating.Percentage, Rating.Loved, Rating.Votes, Rating.Hated, Rating.Watching, Show.LastUpdated, Image.Banner, Image.Poster, Show.ImdbId, Show.TvdbId, Show.GenreNames, COUNT(*) OVER () as TotalCount
                    FROM 
                        ShowSet AS Show
                    INNER JOIN 
                        ImageShowSet AS Image
                    ON 
                        Image.Id = Show.ImagesId
                    INNER JOIN 
                        RatingSet AS Rating
                    ON 
                        Rating.Id = Show.RatingId
                    WHERE
                        Show.ImdbId IN ({0})";

                    await using var command = context.Database.GetDbConnection().CreateCommand();
                    command.CommandType = CommandType.Text;
                    var imdbParameters = new List<string>();
                    var index = 0;
                    foreach (var imdb in imdbIds)
                    {
                        var paramName = "@imdb" + index;
                        command.Parameters.Add(new SqlParameter(paramName, imdb));
                        imdbParameters.Add(paramName);
                        index++;
                    }

                    command.CommandText = string.Format(query, string.Join(",", imdbParameters));
                    var count = 0;
                    var shows = new List<ShowLightJson>();
                    try
                    {
                        await context.Database.OpenConnectionAsync(cancellation);
                        await using var reader = await command.ExecuteReaderAsync(cancellation);
                        while (await reader.ReadAsync(cancellation))
                        {
                            var show = new ShowLightJson
                            {
                                Title = !await reader.IsDBNullAsync(0, cancellation)
                                    ? reader.GetString(0)
                                    : string.Empty,
                                Year = !await reader.IsDBNullAsync(1, cancellation) ? reader.GetInt32(1) : 0,
                                Rating = new RatingJson
                                {
                                    Percentage = !await reader.IsDBNullAsync(2, cancellation) ? reader.GetInt32(2) : 0,
                                    Loved = !await reader.IsDBNullAsync(3, cancellation) ? reader.GetInt32(3) : 0,
                                    Votes = !await reader.IsDBNullAsync(4, cancellation) ? reader.GetInt32(4) : 0,
                                    Hated = !await reader.IsDBNullAsync(5, cancellation) ? reader.GetInt32(5) : 0,
                                    Watching = !await reader.IsDBNullAsync(6, cancellation) ? reader.GetInt32(6) : 0
                                },
                                Images = new ImageShowJson
                                {
                                    Banner =
                                        !await reader.IsDBNullAsync(8, cancellation)
                                            ? reader.GetString(8)
                                            : string.Empty,
                                    Poster =
                                        !await reader.IsDBNullAsync(9, cancellation)
                                            ? reader.GetString(9)
                                            : string.Empty,
                                },
                                ImdbId = !await reader.IsDBNullAsync(10, cancellation)
                                    ? reader.GetString(10)
                                    : string.Empty,
                                TvdbId = !await reader.IsDBNullAsync(11, cancellation)
                                    ? reader.GetString(11)
                                    : string.Empty,
                                Genres = !await reader.IsDBNullAsync(12, cancellation)
                                    ? reader.GetString(12)
                                    : string.Empty
                            };

                            shows.Add(show);
                            count = !await reader.IsDBNullAsync(13, cancellation) ? reader.GetInt32(13) : 0;
                        }
                    }
                    catch (Exception exception) when (exception is TaskCanceledException)
                    {
                        Logger.Debug(
                            "GetShowsByIds cancelled.");
                    }
                    catch (Exception exception)
                    {
                        Logger.Error(
                            $"GetShowsByIds: {exception.Message}");
                        throw;
                    }
                    finally
                    {
                        watch.Stop();
                        var elapsedMs = watch.ElapsedMilliseconds;
                        Logger.Trace(
                            $"GetShowsByIds ({string.Join(",", imdbIds)}) in {elapsedMs} milliseconds.");
                    }

                    return (shows, count);
                }, ct);
            }
            catch (Exception ex)
            {
                Logger.Error(ex);
                return (new List<ShowLightJson>(), 0);
            }
        }

        /// <summary>
        /// Get popular shows by page
        /// </summary>
        /// <param name="page">Page to return</param>
        /// <param name="limit">The maximum number of shows to return</param>
        /// <param name="ct">Cancellation token</param>
        /// <param name="genre">The genre to filter</param>
        /// <param name="sortBy">The sort</param>
        /// <param name="ratingFilter">Used to filter by rating</param>
        /// <returns>Popular shows and the number of shows found</returns>
        public async Task<(IEnumerable<ShowLightJson> shows, int nbShows)> GetShowsAsync(int page,
            int limit,
            double ratingFilter,
            string sortBy,
            CancellationToken ct,
            GenreJson genre = null)
        {
            return await SearchShowsAsync(string.Empty,
                page,
                limit,
                genre,
                ratingFilter,
                sortBy,
                ct);
        }

        /// <summary>
        /// Search shows by criteria
        /// </summary>
        /// <param name="criteria">Criteria used for search</param>
        /// <param name="page">Page to return</param>
        /// <param name="limit">The maximum number of movies to return</param>
        /// <param name="genre">The genre to filter</param>
        /// <param name="ratingFilter">Used to filter by rating</param>
        /// <param name="sortBy">Sort by</param>
        /// <param name="ct">Cancellation token</param>
        /// <returns>Searched shows and the number of movies found</returns>
        public async Task<(IEnumerable<ShowLightJson> shows, int nbShows)> SearchShowsAsync(string criteria,
            int page,
            int limit,
            GenreJson genre,
            double ratingFilter,
            string sortBy,
            CancellationToken ct)
        {
            var timeoutPolicy =
                Policy.TimeoutAsync(Constants.DefaultRequestTimeoutInSecond, TimeoutStrategy.Optimistic);
            try
            {
                return await timeoutPolicy.ExecuteAsync(async cancellation =>
                {
                    var optionsBuilder = new DbContextOptionsBuilder<PopcornContext>();
                    optionsBuilder.UseSqlServer(
                        (ConfigurationManager.GetSection("settings") as NameValueCollection)["SQLConnectionString"],
                        builder =>
                        {
                            builder.CommandTimeout(Convert.ToInt32(TimeSpan.FromSeconds(60).TotalSeconds));
                            builder.EnableRetryOnFailure();
                        });
                    await using var context = new PopcornContext(optionsBuilder.Options);

                    var watch = Stopwatch.StartNew();
                    if (limit < 1 || limit > 50)
                        limit = Constants.MaxShowsPerPage;

                    if (page < 1)
                        page = 1;

                    var count = 0;
                    var shows = new List<ShowLightJson>();

                    var skipParameter = new SqlParameter("@skip", (page - 1) * limit);
                    var takeParameter = new SqlParameter("@take", limit);
                    var ratingParameter = new SqlParameter("@rating", Convert.ToInt32(ratingFilter).ToString());
                    var queryParameter = new SqlParameter("@Keywords", string.Format(@"""{0}""", criteria));
                    var genreParameter = new SqlParameter("@genre", genre != null ? genre.EnglishName : string.Empty);
                    var query = @"
                    SELECT 
                        Show.Title, Show.Year, Rating.Percentage, Rating.Loved, Rating.Votes, Rating.Hated, Rating.Watching, Show.LastUpdated, Image.Banner, Image.Poster, Show.ImdbId, Show.TvdbId, Show.GenreNames, COUNT(*) OVER () as TotalCount
                    FROM 
                        ShowSet AS Show
                    INNER JOIN 
                        ImageShowSet AS Image
                    ON 
                        Image.Id = Show.ImagesId
                    INNER JOIN 
                        RatingSet AS Rating
                    ON 
                        Rating.Id = Show.RatingId
                    WHERE
                        Show.NumSeasons <> 0";

                    if (ratingFilter >= 0 && ratingFilter <= 100)
                    {
                        query += @" AND
                        Rating.Percentage >= @rating";
                    }

                    if (!string.IsNullOrWhiteSpace(criteria))
                    {
                        query += @" AND
                        (CONTAINS(Title, @Keywords) OR CONTAINS(ImdbId, @Keywords) OR CONTAINS(TvdbId, @Keywords))";
                    }

                    if (!string.IsNullOrWhiteSpace(genre != null ? genre.EnglishName : string.Empty))
                    {
                        query += @" AND
                        CONTAINS(GenreNames, @genre)";
                    }

                    if (!string.IsNullOrWhiteSpace(sortBy))
                    {
                        switch (sortBy)
                        {
                            case "title":
                                query += " ORDER BY Show.Title ASC";
                                break;
                            case "year":
                                query += " ORDER BY Show.Year DESC";
                                break;
                            case "rating":
                                query += " ORDER BY Rating.Percentage DESC";
                                break;
                            case "loved":
                                query += " ORDER BY Rating.Loved DESC";
                                break;
                            case "votes":
                                query += " ORDER BY Rating.Votes DESC";
                                break;
                            case "watching":
                                query += " ORDER BY Rating.Watching DESC";
                                break;
                            case "date_added":
                                query += " ORDER BY Show.LastUpdated DESC";
                                break;
                            default:
                                query += " ORDER BY Show.LastUpdated DESC";
                                break;
                        }
                    }
                    else
                    {
                        query += " ORDER BY Show.LastUpdated DESC";
                    }

                    query += @" OFFSET @skip ROWS 
                    FETCH NEXT @take ROWS ONLY";
                    try
                    {
                        await using var command = context.Database.GetDbConnection().CreateCommand();
                        command.CommandText = query;
                        command.CommandType = CommandType.Text;
                        command.Parameters.Add(skipParameter);
                        command.Parameters.Add(takeParameter);
                        command.Parameters.Add(ratingParameter);
                        command.Parameters.Add(queryParameter);
                        command.Parameters.Add(genreParameter);
                        await context.Database.OpenConnectionAsync(cancellation);
                        await using var reader = await command.ExecuteReaderAsync(cancellation);

                        while (await reader.ReadAsync(cancellation))
                        {
                            var show = new ShowLightJson
                            {
                                Title = !await reader.IsDBNullAsync(0, cancellation)
                                    ? reader.GetString(0)
                                    : string.Empty,
                                Year = !await reader.IsDBNullAsync(1, cancellation) ? reader.GetInt32(1) : 0,
                                Rating = new RatingJson
                                {
                                    Percentage = !await reader.IsDBNullAsync(2, cancellation) ? reader.GetInt32(2) : 0,
                                    Loved = !await reader.IsDBNullAsync(3, cancellation) ? reader.GetInt32(3) : 0,
                                    Votes = !await reader.IsDBNullAsync(4, cancellation) ? reader.GetInt32(4) : 0,
                                    Hated = !await reader.IsDBNullAsync(5, cancellation) ? reader.GetInt32(5) : 0,
                                    Watching = !await reader.IsDBNullAsync(6, cancellation) ? reader.GetInt32(6) : 0
                                },
                                Images = new ImageShowJson
                                {
                                    Banner =
                                        !await reader.IsDBNullAsync(8, cancellation)
                                            ? reader.GetString(8)
                                            : string.Empty,
                                    Poster =
                                        !await reader.IsDBNullAsync(9, cancellation)
                                            ? reader.GetString(9)
                                            : string.Empty,
                                },
                                ImdbId = !await reader.IsDBNullAsync(10, cancellation)
                                    ? reader.GetString(10)
                                    : string.Empty,
                                TvdbId = !await reader.IsDBNullAsync(11, cancellation)
                                    ? reader.GetString(11)
                                    : string.Empty,
                                Genres = !await reader.IsDBNullAsync(12, cancellation)
                                    ? reader.GetString(12)
                                    : string.Empty
                            };
                            shows.Add(show);
                            count = !await reader.IsDBNullAsync(13, cancellation) ? reader.GetInt32(13) : 0;
                        }
                    }
                    catch (Exception exception) when (exception is TaskCanceledException)
                    {
                        Logger.Debug(
                            "SearchShowsAsync cancelled.");
                    }
                    catch (Exception exception)
                    {
                        Logger.Error(
                            $"SearchShowsAsync: {exception.Message}");
                        throw;
                    }
                    finally
                    {
                        watch.Stop();
                        var elapsedMs = watch.ElapsedMilliseconds;
                        Logger.Trace(
                            $"SearchShowsAsync ({criteria}, {page}, {limit}) in {elapsedMs} milliseconds.");
                    }

                    return (shows, count);
                }, ct);
            }
            catch (Exception ex)
            {
                Logger.Error(ex);
                throw;
            }
        }

        /// <summary>
        /// Get the link to the youtube trailer of a show
        /// </summary>
        /// <param name="show">The show</param>
        /// <param name="ct">Used to cancel loading trailer</param>
        /// <returns>Video trailer</returns>
        public async Task<string> GetShowTrailerAsync(ShowJson show, CancellationToken ct)
        {
            var timeoutPolicy =
                Policy.TimeoutAsync(Constants.DefaultRequestTimeoutInSecond, TimeoutStrategy.Optimistic);
            try
            {
                return await timeoutPolicy.ExecuteAsync(async cancellation =>
                {
                    var watch = Stopwatch.StartNew();
                    var uri = string.Empty;
                    try
                    {
                        var tmdbVideos = await (await _tmdbService.GetClient).GetTvShowVideosAsync(show.TmdbId);
                        if (tmdbVideos != null && tmdbVideos.Results.Any())
                        {
                            var trailer = tmdbVideos.Results.FirstOrDefault();
                            using var service = Client.For(YouTube.Default);
                            var videos = (await service
                                    .GetAllVideosAsync("https://youtube.com/watch?v=" + trailer.Key))
                                .ToList();
                            if (videos.Any())
                            {
                                var settings = SimpleIoc.Default.GetInstance<ApplicationSettingsViewModel>();
                                var maxRes = settings.DefaultHdQuality ? 1080 : 720;
                                uri =
                                    await videos
                                        .Where(a => !a.Is3D && a.Resolution <= maxRes &&
                                                    a.Format == VideoFormat.Mp4 &&
                                                    a.AudioBitrate > 0)
                                        .Aggregate((i1, i2) => i1.Resolution > i2.Resolution ? i1 : i2)
                                        .GetUriAsync();
                            }
                        }
                        else
                        {
                            throw new PopcornException("No trailer found.");
                        }
                    }
                    catch (Exception exception) when (exception is TaskCanceledException ||
                                                      exception is OperationCanceledException)
                    {
                        Logger.Debug(
                            "GetShowTrailerAsync cancelled.");
                    }
                    catch (Exception exception)
                    {
                        Logger.Error(
                            $"GetShowTrailerAsync: {exception.Message}");
                        throw;
                    }
                    finally
                    {
                        watch.Stop();
                        var elapsedMs = watch.ElapsedMilliseconds;
                        Logger.Trace(
                            $"GetShowTrailerAsync ({show.ImdbId}) in {elapsedMs} milliseconds.");
                    }

                    return uri;
                }, ct);
            }
            catch (Exception ex)
            {
                Logger.Error(ex);
                throw;
            }
        }

        private ShowJson ConvertShowToJson(Database.Show show)
        {
            return new ShowJson
            {
                AirDay = show.AirDay,
                Rating = new RatingJson
                {
                    Hated = show.Rating?.Hated,
                    Loved = show.Rating?.Loved,
                    Percentage = show.Rating?.Percentage,
                    Votes = show.Rating?.Votes,
                    Watching = show.Rating?.Watching
                },
                Title = show.Title,
                Genres = show.Genres.Select(genre => genre.Name).ToList(),
                Year = show.Year,
                ImdbId = show.ImdbId,
                Episodes = show.Episodes.Select(episode => new EpisodeShowJson
                {
                    DateBased = episode.DateBased,
                    EpisodeNumber = episode.EpisodeNumber,
                    Torrents = new TorrentShowNodeJson
                    {
                        Torrent_0 = new TorrentShowJson
                        {
                            Peers = episode.Torrents?.Torrent0?.Peers,
                            Seeds = episode.Torrents?.Torrent0?.Seeds,
                            Provider = episode.Torrents?.Torrent0?.Provider,
                            Url = episode.Torrents?.Torrent0?.Url
                        },
                        Torrent_1080p = new TorrentShowJson
                        {
                            Peers = episode.Torrents?.Torrent1080p?.Peers,
                            Seeds = episode.Torrents?.Torrent1080p?.Seeds,
                            Provider = episode.Torrents?.Torrent1080p?.Provider,
                            Url = episode.Torrents?.Torrent1080p?.Url
                        },
                        Torrent_720p = new TorrentShowJson
                        {
                            Peers = episode.Torrents?.Torrent720p?.Peers,
                            Seeds = episode.Torrents?.Torrent720p?.Seeds,
                            Provider = episode.Torrents?.Torrent720p?.Provider,
                            Url = episode.Torrents?.Torrent720p?.Url
                        },
                        Torrent_480p = new TorrentShowJson
                        {
                            Peers = episode.Torrents?.Torrent480p?.Peers,
                            Seeds = episode.Torrents?.Torrent480p?.Seeds,
                            Provider = episode.Torrents?.Torrent480p?.Provider,
                            Url = episode.Torrents?.Torrent480p?.Url
                        }
                    },
                    FirstAired = episode.FirstAired,
                    Title = episode.Title,
                    Overview = episode.Overview,
                    Season = episode.Season,
                    TvdbId = episode.TvdbId
                }).ToList(),
                TvdbId = show.TvdbId,
                AirTime = show.AirTime,
                Country = show.Country,
                Images = new ImageShowJson
                {
                    Banner = show.Images?.Banner,
                    Poster = show.Images?.Poster
                },
                LastUpdated = show.LastUpdated,
                Network = show.Network,
                NumSeasons = show.NumSeasons,
                Runtime = show.Runtime,
                Slug = show.Slug,
                Status = show.Status,
                Synopsis = show.Synopsis,
                Similars = show.Similars.Select(a => a.TmdbId).ToList()
            };
        }
    }
}