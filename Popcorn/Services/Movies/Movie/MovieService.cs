using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Configuration;
using System.Data;
using System.Diagnostics;
using System.Linq;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Threading;
using System.Threading.Tasks;
using NLog;
using Popcorn.Models.Movie;
using TMDbLib.Objects.Movies;
using GalaSoft.MvvmLight.Ioc;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Popcorn.Models.Genres;
using Popcorn.Models.User;
using Popcorn.Exceptions;
using Polly;
using Polly.Timeout;
using Popcorn.Database;
using Popcorn.Extensions;
using Popcorn.Helpers;
using Popcorn.Models.Cast;
using Popcorn.Models.Torrent.Movie;
using Popcorn.Services.Tmdb;
using Popcorn.ViewModels.Pages.Home.Settings.ApplicationSettings;
using TMDbLib.Objects.Find;
using TMDbLib.Objects.People;
using VideoLibrary;

namespace Popcorn.Services.Movies.Movie
{
    /// <summary>
    /// Services used to interact with movies
    /// </summary>
    public class MovieService : IMovieService
    {
        /// <summary>
        /// Logger of the class
        /// </summary>
        private static Logger Logger { get; } = LogManager.GetCurrentClassLogger();

        /// <summary>
        /// Movies to translate
        /// </summary>
        private readonly Subject<IMovie> _moviesToTranslateObservable;

        /// <summary>
        /// <see cref="ITmdbService"/>
        /// </summary>
        private readonly ITmdbService _tmdbService;

        /// <summary>
        /// Initialize a new instance of MovieService class
        /// </summary>
        public MovieService(ITmdbService tmdbService)
        {
            _moviesToTranslateObservable = new Subject<IMovie>();
            _tmdbService = tmdbService;

            try
            {
                _moviesToTranslateObservable.Drain(s => Observable.Return(s).Delay(TimeSpan.FromMilliseconds(250)))
                    .Subscribe(async movieToTranslate =>
                    {
                        if (movieToTranslate == null)
                            return;

                        var timeoutPolicy =
                            Policy.TimeoutAsync(Constants.DefaultRequestTimeoutInSecond,
                                TimeoutStrategy.Optimistic);
                        try
                        {
                            await timeoutPolicy.ExecuteAsync(async () =>
                            {
                                try
                                {
                                    var movie = await (await _tmdbService.GetClient).GetMovieAsync(
                                        movieToTranslate.ImdbId,
                                        MovieMethods.Credits);
                                    if (movieToTranslate is MovieJson refMovie)
                                    {
                                        refMovie.TranslationLanguage = (await _tmdbService.GetClient).DefaultLanguage;
                                        refMovie.Title = movie?.Title;
                                        refMovie.Genres = movie?.Genres?.Select(a => a.Name).ToList();
                                        refMovie.DescriptionFull = movie?.Overview;
                                    }
                                    else if (movieToTranslate is MovieLightJson refMovieLight)
                                    {
                                        refMovieLight.TranslationLanguage =
                                            (await _tmdbService.GetClient).DefaultLanguage;
                                        refMovieLight.Title = movie?.Title;
                                        refMovieLight.Genres = movie?.Genres != null
                                            ? string.Join(", ", movie.Genres?.Select(a => a.Name))
                                            : string.Empty;
                                    }
                                }
                                catch (Exception exception) when (exception is TaskCanceledException)
                                {
                                    Logger.Debug(
                                        "TranslateMovieAsync cancelled.");
                                }
                                catch (Exception exception)
                                {
                                    Logger.Error(
                                        $"TranslateMovieAsync: {exception.Message}");
                                }
                            });
                        }
                        catch (Exception ex)
                        {
                            Logger.Warn(
                                $"Movie {movieToTranslate.ImdbId} has not been translated in {Constants.DefaultRequestTimeoutInSecond} seconds. Error {ex.Message}");
                        }
                    });
            }
            catch (Exception ex)
            {
                Logger.Error(ex);
            }
        }

        /// <summary>
        /// Change the culture of TMDb
        /// </summary>
        /// <param name="language">Language to set</param>
        public async Task ChangeTmdbLanguage(Language language)
        {
            (await _tmdbService.GetClient).DefaultLanguage = language.Culture;
        }

        /// <summary>
        /// Get movie by its Imdb code
        /// </summary>
        /// <param name="imdbCode">Movie's Imdb code</param>
        /// <param name="ct">Cancellation</param>
        /// <returns>The movie</returns>
        public async Task<MovieJson> GetMovieAsync(string imdbCode, CancellationToken ct)
        {
            var timeoutPolicy =
                Policy.TimeoutAsync(Constants.DefaultRequestTimeoutInSecond, TimeoutStrategy.Optimistic);
            try
            {
                return await timeoutPolicy.ExecuteAsync(async cancellation =>
                {
                    var watch = Stopwatch.StartNew();

                    var optionsBuilder = new DbContextOptionsBuilder<PopcornContext>();
                    optionsBuilder.UseSqlServer(
                        (ConfigurationManager.GetSection("settings") as NameValueCollection)["SQLConnectionString"],
                        builder =>
                        {
                            builder.CommandTimeout(Convert.ToInt32(TimeSpan.FromSeconds(60).TotalSeconds));
                            builder.EnableRetryOnFailure();
                        });
                    await using var context = new PopcornContext(optionsBuilder.Options);

                    var movie = new MovieJson();
                    try
                    {
                        var movieJson =
                            await context.MovieSet.Include(a => a.Torrents)
                                .Include(a => a.Cast)
                                .Include(a => a.Similars)
                                .Include(a => a.Genres).AsQueryable()
                                .FirstOrDefaultAsync(
                                    document => document.ImdbCode == imdbCode, cancellation);
                        movie = ConvertMovieToJson(movieJson);
                        movie.TranslationLanguage = (await _tmdbService.GetClient).DefaultLanguage;
                        movie.TmdbId =
                            (await (await _tmdbService.GetClient).GetMovieAsync(movie.ImdbId,
                                cancellationToken: cancellation)).Id;
                    }
                    catch (Exception exception) when (exception is TaskCanceledException)
                    {
                        Logger.Debug(
                            "GetMovieAsync cancelled.");
                    }
                    catch (Exception exception)
                    {
                        Logger.Error(
                            $"GetMovieAsync: {exception.Message}");
                        throw;
                    }
                    finally
                    {
                        watch.Stop();
                        var elapsedMs = watch.ElapsedMilliseconds;
                        Logger.Trace(
                            $"GetMovieAsync ({imdbCode}) in {elapsedMs} milliseconds.");
                    }

                    return movie;
                }, ct);
            }
            catch (Exception ex)
            {
                Logger.Error(ex);
                throw;
            }
        }

        /// <summary>
        /// Get light movie by its Imdb code
        /// </summary>
        /// <param name="imdbCode">Movie's Imdb code</param>
        /// <param name="ct">Cancellation</param>
        /// <returns>The movie</returns>
        public async Task<MovieLightJson> GetMovieLightAsync(string imdbCode, CancellationToken ct)
        {
            var timeoutPolicy =
                Policy.TimeoutAsync(Constants.DefaultRequestTimeoutInSecond, TimeoutStrategy.Optimistic);
            try
            {
                return await timeoutPolicy.ExecuteAsync(async cancellation =>
                {
                    var watch = Stopwatch.StartNew();

                    var optionsBuilder = new DbContextOptionsBuilder<PopcornContext>();
                    optionsBuilder.UseSqlServer(
                        (ConfigurationManager.GetSection("settings") as NameValueCollection)["SQLConnectionString"],
                        builder =>
                        {
                            builder.CommandTimeout(Convert.ToInt32(TimeSpan.FromSeconds(60).TotalSeconds));
                            builder.EnableRetryOnFailure();
                        });
                    await using var context = new PopcornContext(optionsBuilder.Options);
                    var movie = new MovieLightJson();

                    try
                    {
                        var imdbParameter = new SqlParameter("@imdbCode", imdbCode);
                        var query = @"
                    SELECT 
                        Movie.Title, Movie.Year, Movie.Rating, Movie.PosterImage, Movie.ImdbCode, Movie.GenreNames
                    FROM 
                        MovieSet AS Movie
                    WHERE
                        Movie.ImdbCode = @imdbCode";

                        await using var command = context.Database.GetDbConnection().CreateCommand();
                        command.CommandText = query;
                        command.CommandType = CommandType.Text;
                        command.Parameters.Add(imdbParameter);
                        await context.Database.OpenConnectionAsync(cancellation);
                        await using var reader = await command.ExecuteReaderAsync(cancellation);
                        while (await reader.ReadAsync(cancellation))
                        {
                            movie.Title = !await reader.IsDBNullAsync(0, cancellation)
                                ? reader.GetString(0)
                                : string.Empty;
                            movie.Year = !await reader.IsDBNullAsync(1, cancellation) ? reader.GetInt32(1) : 0;
                            movie.Rating = !await reader.IsDBNullAsync(2, cancellation) ? reader.GetDouble(2) : 0d;
                            movie.PosterImage = !await reader.IsDBNullAsync(3, cancellation)
                                ? reader.GetString(3)
                                : string.Empty;
                            movie.ImdbId = !await reader.IsDBNullAsync(4, cancellation)
                                ? reader.GetString(4)
                                : string.Empty;
                            movie.Genres = !await reader.IsDBNullAsync(5, cancellation)
                                ? reader.GetString(5)
                                : string.Empty;
                        }

                        movie.TranslationLanguage = (await _tmdbService.GetClient).DefaultLanguage;
                    }
                    catch (Exception exception) when (exception is TaskCanceledException)
                    {
                        Logger.Debug(
                            "GetMovieLightAsync cancelled.");
                    }
                    catch (Exception exception)
                    {
                        Logger.Error(
                            $"GetMovieLightAsync: {exception.Message}");
                        throw;
                    }
                    finally
                    {
                        watch.Stop();
                        var elapsedMs = watch.ElapsedMilliseconds;
                        Logger.Trace(
                            $"GetMovieLightAsync ({imdbCode}) in {elapsedMs} milliseconds.");
                    }

                    return movie;
                }, ct);
            }
            catch (Exception ex)
            {
                Logger.Error(ex);
                throw;
            }
        }

        /// <summary>
        /// Get movies similar async
        /// </summary>
        /// <param name="movie">Movie</param>
        /// <param name="ct">Cancellation</param>
        /// <returns>Movies</returns>
        public async Task<IEnumerable<MovieLightJson>> GetMoviesSimilarAsync(MovieJson movie, CancellationToken ct)
        {
            var timeoutPolicy =
                Policy.TimeoutAsync(Constants.DefaultRequestTimeoutInSecond, TimeoutStrategy.Optimistic);
            try
            {
                return await timeoutPolicy.ExecuteAsync(async cancellation =>
                {
                    var watch = Stopwatch.StartNew();
                    (IEnumerable<MovieLightJson> movies, int nbMovies) similarMovies = (new List<MovieLightJson>(), 0);
                    try
                    {
                        if (movie.Similars != null && movie.Similars.Any())
                        {
                            similarMovies = await GetMoviesByIds(movie.Similars,
                                CancellationToken.None);
                        }
                    }
                    catch (Exception exception)
                    {
                        Logger.Error(
                            $"GetMoviesSimilarAsync: {exception.Message}");
                        throw;
                    }
                    finally
                    {
                        watch.Stop();
                        var elapsedMs = watch.ElapsedMilliseconds;
                        Logger.Trace(
                            $"GetMoviesSimilarAsync in {elapsedMs} milliseconds.");
                    }

                    return similarMovies.movies.Where(
                        a => a.ImdbId != movie.ImdbId);
                }, ct);
            }
            catch (Exception ex)
            {
                Logger.Error(ex);
                return new List<MovieLightJson>();
            }
        }

        /// <summary>
        /// Get movies by page
        /// </summary>
        /// <param name="page">Page to return</param>
        /// <param name="limit">The maximum number of movies to return</param>
        /// <param name="ct">Cancellation token</param>
        /// <param name="genre">The genre to filter</param>
        /// <param name="sortBy">The sort</param>
        /// <param name="ratingFilter">Used to filter by rating</param>
        /// <returns>Popular movies and the number of movies found</returns>
        public async Task<(IEnumerable<MovieLightJson> movies, int nbMovies)> GetMoviesAsync(int page,
            int limit,
            double ratingFilter,
            string sortBy,
            CancellationToken ct,
            GenreJson genre = null)
        {
            return await SearchMoviesAsync(string.Empty,
                page,
                limit,
                genre,
                ratingFilter, 
                sortBy,
                ct);
        }

        /// <summary>
        /// Process translations for a list of movies
        /// </summary>
        /// <param name="movies"></param>
        /// <returns></returns>
        private async Task ProcessTranslations(IEnumerable<IMovie> movies)
        {
            foreach (var movie in movies)
            {
                await TranslateMovie(movie);
            }
        }

        /// <summary>
        /// Get movies by ids
        /// </summary>
        /// <param name="imdbIds">The imdbIds of the movies, split by comma</param>
        /// <param name="ct">Cancellation token</param>
        /// <returns>Similar movies</returns>
        public async Task<(IEnumerable<MovieLightJson> movies, int nbMovies)> GetMoviesByIds(
            IList<string> imdbIds,
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
                        Movie.Title, Movie.Year, Movie.Rating, Movie.PosterImage, Movie.ImdbCode, Movie.GenreNames, COUNT(*) OVER () as TotalCount
                    FROM 
                        MovieSet AS Movie
                    WHERE
                        Movie.ImdbCode IN ({0})
                    ORDER BY Movie.Rating DESC";

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
                    var movies = new List<MovieLightJson>();
                    try
                    {
                        await context.Database.OpenConnectionAsync(cancellation);
                        await using var reader = await command.ExecuteReaderAsync(cancellation);
                        while (await reader.ReadAsync(cancellation))
                        {
                            var movie = new MovieLightJson
                            {
                                Title = !await reader.IsDBNullAsync(0, cancellation)
                                    ? reader.GetString(0)
                                    : string.Empty,
                                Year = !await reader.IsDBNullAsync(1, cancellation) ? reader.GetInt32(1) : 0,
                                Rating = !await reader.IsDBNullAsync(2, cancellation) ? reader.GetDouble(2) : 0d,
                                PosterImage = !await reader.IsDBNullAsync(3, cancellation)
                                    ? reader.GetString(3)
                                    : string.Empty,
                                ImdbId = !await reader.IsDBNullAsync(4, cancellation)
                                    ? reader.GetString(4)
                                    : string.Empty,
                                Genres = !await reader.IsDBNullAsync(5, cancellation)
                                    ? reader.GetString(5)
                                    : string.Empty,
                                TranslationLanguage = (await _tmdbService.GetClient).DefaultLanguage
                            };
                            movies.Add(movie);
                            count = !await reader.IsDBNullAsync(6, cancellation) ? reader.GetInt32(6) : 0;
                        }
                    }
                    catch (Exception exception) when (exception is TaskCanceledException)
                    {
                        Logger.Debug(
                            "GetMoviesByIds cancelled.");
                    }
                    catch (Exception exception)
                    {
                        Logger.Error(
                            $"GetMoviesByIds: {exception.Message}");
                        throw;
                    }
                    finally
                    {
                        watch.Stop();
                        var elapsedMs = watch.ElapsedMilliseconds;
                        Logger.Trace(
                            $"GetMoviesByIds ({string.Join(",", imdbIds)}) in {elapsedMs} milliseconds.");
                    }

                    await ProcessTranslations(movies);
                    return (movies, count);
                }, ct);
            }
            catch (Exception ex)
            {
                Logger.Error(ex);
                return (new List<MovieLightJson>(), 0);
            }
        }

        /// <summary>
        /// Get similar movies
        /// </summary>
        /// <param name="page">Page</param>
        /// <param name="limit">Limit</param>
        /// <param name="ratingFilter">Rating</param>
        /// <param name="sortBy">SortBy</param>
        /// <param name="imdbIds">The imdbIds of the movies, split by comma</param>
        /// <param name="ct">Cancellation token</param>
        /// <param name="genre">Genre</param>
        /// <returns>Similar movies</returns>
        public async Task<(IEnumerable<MovieLightJson> movies, int nbMovies)> GetSimilar(int page,
            int limit,
            double ratingFilter,
            string sortBy,
            IList<string> imdbIds,
            CancellationToken ct,
            GenreJson genre = null)
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
                        limit = Constants.MaxMoviesPerPage;

                    if (page < 1)
                        page = 1;

                    var skipParameter = new SqlParameter("@skip", (page - 1) * limit);
                    var takeParameter = new SqlParameter("@take", limit);
                    var ratingParameter = new SqlParameter("@rating", Convert.ToInt32(ratingFilter).ToString());
                    var queryParameter = new SqlParameter("@Keywords", string.Format(@"""{0}""", string.Empty));
                    var genreParameter = new SqlParameter("@genre", genre != null ? genre.EnglishName : string.Empty);
                    var query = @"
                    SELECT DISTINCT
                        Movie.Title, Movie.Year, Movie.Rating, Movie.PosterImage, Movie.ImdbCode, Movie.GenreNames, COUNT(*) OVER () as TotalCount
                    FROM 
                        MovieSet AS Movie
                    WHERE
                        Movie.ImdbCode IN (SELECT 
                            Similar.TmdbId                      
                        FROM 
                            Similar AS Similar
                        INNER JOIN
					    (
						    SELECT Movie.ID
						    FROM 
							    MovieSet AS Movie
						    WHERE 
							    Movie.ImdbCode IN ({0})
						) Movie
					ON Similar.MovieId = Movie.Id)
                    AND 1 = 1";

                    if (ratingFilter > 0 && ratingFilter < 10)
                    {
                        query += @" AND
                        Rating >= @rating";
                    }

                    if (!string.IsNullOrWhiteSpace(string.Empty))
                    {
                        query += @" AND
                        (CONTAINS(Movie.Title, @Keywords) OR CONTAINS(Movie.ImdbCode, @Keywords))";
                    }

                    if (!string.IsNullOrWhiteSpace(genre != null ? genre.EnglishName : string.Empty))
                    {
                        query += @" AND
                        CONTAINS(Movie.GenreNames, @genre)";
                    }

                    query += " ORDER BY Movie.Rating DESC";
                    query += @" OFFSET @skip ROWS 
                    FETCH NEXT @take ROWS ONLY";

                    await using var command = context.Database.GetDbConnection().CreateCommand();
                    command.Parameters.Add(skipParameter);
                    command.Parameters.Add(takeParameter);
                    command.Parameters.Add(ratingParameter);
                    command.Parameters.Add(queryParameter);
                    command.Parameters.Add(genreParameter);
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
                    var movies = new List<MovieLightJson>();

                    try
                    {
                        await command.Connection.OpenAsync(cancellation);
                        await using var reader = await command.ExecuteReaderAsync(cancellation);

                        while (await reader.ReadAsync(cancellation))
                        {
                            var movie = new MovieLightJson
                            {
                                Title = !await reader.IsDBNullAsync(0, cancellation)
                                    ? reader.GetString(0)
                                    : string.Empty,
                                Year = !await reader.IsDBNullAsync(1, cancellation) ? reader.GetInt32(1) : 0,
                                Rating = !await reader.IsDBNullAsync(2, cancellation) ? reader.GetDouble(2) : 0d,
                                PosterImage = !await reader.IsDBNullAsync(3, cancellation)
                                    ? reader.GetString(3)
                                    : string.Empty,
                                ImdbId = !await reader.IsDBNullAsync(4, cancellation)
                                    ? reader.GetString(4)
                                    : string.Empty,
                                Genres = !await reader.IsDBNullAsync(5, cancellation)
                                    ? reader.GetString(5)
                                    : string.Empty,
                                TranslationLanguage = (await _tmdbService.GetClient).DefaultLanguage
                            };
                            movies.Add(movie);
                            count = !await reader.IsDBNullAsync(6, cancellation) ? reader.GetInt32(6) : 0;
                        }
                    }
                    catch (Exception exception) when (exception is TaskCanceledException)
                    {
                        Logger.Debug(
                            "GetMoviesByIds cancelled.");
                    }
                    catch (Exception exception)
                    {
                        Logger.Error(
                            $"GetMoviesByIds: {exception.Message}");
                        throw;
                    }
                    finally
                    {
                        watch.Stop();
                        var elapsedMs = watch.ElapsedMilliseconds;
                        Logger.Trace(
                            $"GetMoviesByIds ({string.Join(",", imdbIds)}) in {elapsedMs} milliseconds.");
                    }

                    await ProcessTranslations(movies);
                    return (movies, count);
                }, ct);
            }
            catch (Exception ex)
            {
                Logger.Error(ex);
                return (new List<MovieLightJson>(), 0);
            }
        }

        /// <summary>
        /// Search movies by criteria
        /// </summary>
        /// <param name="criteria">Criteria used for search</param>
        /// <param name="page">Page to return</param>
        /// <param name="limit">The maximum number of movies to return</param>
        /// <param name="genre">The genre to filter</param>
        /// <param name="ratingFilter">Used to filter by rating</param>
        /// <param name="sortBy">Sort by</param>
        /// <param name="ct">Cancellation token</param>
        /// <returns>Searched movies and the number of movies found</returns>
        public async Task<(IEnumerable<MovieLightJson> movies, int nbMovies)> SearchMoviesAsync(string criteria,
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
                        limit = Constants.MaxMoviesPerPage;

                    if (page < 1)
                        page = 1;

                    var skipParameter = new SqlParameter("@skip", (page - 1) * limit);
                    var takeParameter = new SqlParameter("@take", limit);
                    var ratingParameter = new SqlParameter("@rating", ratingFilter);
                    var queryParameter = new SqlParameter("@Keywords", string.Format(@"""{0}""", criteria));
                    var genreParameter = new SqlParameter("@genre", genre != null ? genre.EnglishName : string.Empty);
                    var query = @"
                    SELECT DISTINCT
                        Movie.Title, Movie.Year, Movie.Rating, Movie.PosterImage, Movie.ImdbCode, Movie.GenreNames, Torrent.Peers, Torrent.Seeds, COUNT(*) OVER () as TotalCount, Movie.DateUploadedUnix, Movie.Id, Movie.DownloadCount, Movie.LikeCount
                    FROM 
                        MovieSet AS Movie
                    CROSS APPLY
					(
						SELECT TOP 1 Torrent.MovieId, Torrent.Peers, Torrent.Seeds FROM TorrentMovieSet AS Torrent
						WHERE Torrent.MovieId = Movie.Id  AND Torrent.Url <> '' AND Torrent.Url IS NOT NULL
					) Torrent

                    INNER JOIN
                        CastSet AS Cast
                    ON Cast.MovieId = Movie.Id
                    WHERE 1 = 1";

                    if (ratingFilter >= 0 && ratingFilter <= 10)
                    {
                        query += @" AND
                        Movie.Rating >= @rating";
                    }

                    if (!string.IsNullOrWhiteSpace(criteria))
                    {
                        query += @" AND
                        (CONTAINS(Movie.Title, @Keywords) OR CONTAINS(Cast.Name, @Keywords) OR CONTAINS(Movie.ImdbCode, @Keywords) OR CONTAINS(Cast.ImdbCode, @Keywords))";
                    }

                    if (!string.IsNullOrWhiteSpace(genre != null ? genre.EnglishName : string.Empty))
                    {
                        query += @" AND
                        CONTAINS(Movie.GenreNames, @genre)";
                    }

                    query +=
                        " GROUP BY Movie.Id, Movie.Title, Movie.Year, Movie.Rating, Movie.PosterImage, Movie.ImdbCode, Movie.GenreNames, Torrent.Peers, Torrent.Seeds, Movie.DateUploadedUnix, Movie.Id, Movie.DownloadCount, Movie.LikeCount";

                    if (!string.IsNullOrWhiteSpace(sortBy))
                    {
                        switch (sortBy)
                        {
                            case "title":
                                query += " ORDER BY Movie.Title ASC";
                                break;
                            case "year":
                                query += " ORDER BY Movie.Year DESC";
                                break;
                            case "rating":
                                query += " ORDER BY Movie.Rating DESC";
                                break;
                            case "peers":
                                query += " ORDER BY Torrent.Peers DESC";
                                break;
                            case "seeds":
                                query += " ORDER BY Torrent.Seeds DESC";
                                break;
                            case "download_count":
                                query += " ORDER BY Movie.DownloadCount DESC";
                                break;
                            case "like_count":
                                query += " ORDER BY Movie.LikeCount DESC";
                                break;
                            case "date_added":
                                query += " ORDER BY Movie.DateUploadedUnix DESC";
                                break;
                            default:
                                query += " ORDER BY Movie.DateUploadedUnix DESC";
                                break;
                        }
                    }
                    else
                    {
                        query += " ORDER BY Movie.DateUploadedUnix DESC";
                    }
                    query += @" OFFSET @skip ROWS 
                    FETCH NEXT @take ROWS ONLY";

                    var count = 0;
                    var movies = new List<MovieLightJson>();
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
                            var movie = new MovieLightJson
                            {
                                Title = !await reader.IsDBNullAsync(0, cancellation)
                                    ? reader.GetString(0)
                                    : string.Empty,
                                Year = !await reader.IsDBNullAsync(1, cancellation) ? reader.GetInt32(1) : 0,
                                Rating = !await reader.IsDBNullAsync(2, cancellation) ? reader.GetDouble(2) : 0d,
                                PosterImage = !await reader.IsDBNullAsync(3, cancellation)
                                    ? reader.GetString(3)
                                    : string.Empty,
                                ImdbId = !await reader.IsDBNullAsync(4, cancellation)
                                    ? reader.GetString(4)
                                    : string.Empty,
                                Genres = !await reader.IsDBNullAsync(5, cancellation)
                                    ? reader.GetString(5)
                                    : string.Empty,
                                TranslationLanguage = (await _tmdbService.GetClient).DefaultLanguage
                            };
                            movies.Add(movie);
                            count = !await reader.IsDBNullAsync(8, cancellation) ? reader.GetInt32(8) : 0;
                        }
                    }
                    catch (Exception exception) when (exception is TaskCanceledException)
                    {
                        Logger.Debug(
                            "SearchMoviesAsync cancelled.");
                    }
                    catch (Exception exception)
                    {
                        Logger.Error(
                            $"SearchMoviesAsync: {exception.Message}");
                        throw;
                    }
                    finally
                    {
                        watch.Stop();
                        var elapsedMs = watch.ElapsedMilliseconds;
                        Logger.Trace(
                            $"SearchMoviesAsync ({criteria}, {page}, {limit}) in {elapsedMs} milliseconds.");
                    }

                    await ProcessTranslations(movies);
                    return (movies, count);
                }, ct);
            }
            catch (Exception ex)
            {
                Logger.Error(ex);
                throw;
            }
        }

        /// <summary>
        /// Translate movie informations (title, description, ...)
        /// </summary>
        /// <param name="movieToTranslate">Movie to translate</param>
        /// <returns>Task</returns>
        public async Task TranslateMovie(IMovie movieToTranslate)
        {
            if (movieToTranslate.TranslationLanguage == null ||
                (await _tmdbService.GetClient).DefaultLanguage == "en" &&
                movieToTranslate.TranslationLanguage == (await _tmdbService.GetClient).DefaultLanguage) return;
            _moviesToTranslateObservable.OnNext(movieToTranslate);
        }

        /// <summary>
        /// Get the link to the youtube trailer of a movie
        /// </summary>
        /// <param name="movie">The movie</param>
        /// <param name="ct">Used to cancel loading trailer</param>
        /// <returns>Video trailer</returns>
        public async Task<string> GetMovieTrailerAsync(MovieJson movie, CancellationToken ct)
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
                        var tmdbMovie =
                            await (await _tmdbService.GetClient).GetMovieAsync(movie.ImdbId, MovieMethods.Videos, cancellation);
                        var trailers = tmdbMovie?.Videos;
                        if (trailers != null && trailers.Results.Any())
                        {
                            var trailer = trailers.Results
                                .First()
                                .Key;
                            var video = await GetVideoFromYtVideoId(trailer);
                            uri = await video.GetUriAsync();
                        }
                        else
                        {
                            throw new PopcornException("No trailer found.");
                        }
                    }
                    catch (Exception exception) when (exception is TaskCanceledException)
                    {
                        Logger.Debug(
                            "GetMovieTrailerAsync cancelled.");
                    }
                    catch (Exception exception)
                    {
                        Logger.Error(
                            $"GetMovieTrailerAsync: {exception.Message}");
                        throw;
                    }
                    finally
                    {
                        watch.Stop();
                        var elapsedMs = watch.ElapsedMilliseconds;
                        Logger.Trace(
                            $"GetMovieTrailerAsync ({movie.ImdbId}) in {elapsedMs} milliseconds.");
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

        public async Task<YouTubeVideo> GetVideoFromYtVideoId(string ytVideoId)
        {
            using (var service = Client.For(YouTube.Default))
            {
                var videos =
                    (await service.GetAllVideosAsync($"https://youtube.com/watch?v={ytVideoId}"))
                    .ToList();
                if (videos.Any())
                {
                    var settings = SimpleIoc.Default.GetInstance<ApplicationSettingsViewModel>();
                    var maxRes = settings.DefaultHdQuality ? 1080 : 720;
                    return
                        videos.Where(a => !a.Is3D && a.Resolution <= maxRes &&
                                          a.Format == VideoFormat.Mp4 && a.AudioBitrate > 0)
                            .Aggregate((i1, i2) => i1.Resolution > i2.Resolution ? i1 : i2);
                }

                return null;
            }
        }

        /// <summary>
        /// Get cast
        /// </summary>
        /// <param name="imdbCode">Tmdb cast Id</param>
        /// <returns><see cref="Person"/></returns>
        public async Task<Person> GetCast(string imdbCode)
        {
            try
            {
                var search = await (await _tmdbService.GetClient).FindAsync(FindExternalSource.Imdb, $"nm{imdbCode}");
                return await (await _tmdbService.GetClient).GetPersonAsync(search.PersonResults.First().Id,
                    PersonMethods.Images | PersonMethods.TaggedImages);
            }
            catch (Exception ex)
            {
                Logger.Error(ex);
                return null;
            }
        }

        /// <summary>
        /// Get movies for a cast by its ImdbId
        /// </summary>
        /// <param name="imdbCode"></param>
        /// <param name="ct"></param>
        /// <returns></returns>
        public async Task<IEnumerable<MovieLightJson>> GetMovieFromCast(string imdbCode, CancellationToken ct)
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
                    var imdbParameter = new SqlParameter("@imdbCode", imdbCode);
                    var query = @"
                    SELECT 
                        Movie.Title, Movie.Year, Movie.Rating, Movie.PosterImage, Movie.ImdbCode, Movie.GenreNames
                    FROM 
                        MovieSet AS Movie
                    INNER JOIN
                        CastSet AS Cast
                    ON 
                        Cast.MovieId = Movie.Id
                    WHERE
                        Cast.ImdbCode = @imdbCode";

                    var movies = new List<MovieLightJson>();

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
                            var movie = new MovieLightJson
                            {
                                Title = !await reader.IsDBNullAsync(0, cancellation)
                                    ? reader.GetString(0)
                                    : string.Empty,
                                Year = !await reader.IsDBNullAsync(1, cancellation) ? reader.GetInt32(1) : 0,
                                Rating = !await reader.IsDBNullAsync(2, cancellation) ? reader.GetDouble(2) : 0d,
                                PosterImage = !await reader.IsDBNullAsync(3, cancellation)
                                    ? reader.GetString(3)
                                    : string.Empty,
                                ImdbId = !await reader.IsDBNullAsync(4, cancellation)
                                    ? reader.GetString(4)
                                    : string.Empty,
                                Genres = !await reader.IsDBNullAsync(5, cancellation)
                                    ? reader.GetString(5)
                                    : string.Empty,
                                TranslationLanguage = (await _tmdbService.GetClient).DefaultLanguage
                            };
                            movies.Add(movie);
                        }
                    }
                    catch (Exception exception) when (exception is TaskCanceledException)
                    {
                        Logger.Debug(
                            "GetMovieFromCast cancelled.");
                    }
                    catch (Exception exception)
                    {
                        Logger.Error(
                            $"GetMovieFromCast: {exception.Message}");
                        throw;
                    }
                    finally
                    {
                        watch.Stop();
                        var elapsedMs = watch.ElapsedMilliseconds;
                        Logger.Trace(
                            $"GetMovieFromCast ({imdbCode}) in {elapsedMs} milliseconds.");
                    }

                    await ProcessTranslations(movies);
                    return movies;
                }, ct);
            }
            catch (Exception ex)
            {
                Logger.Error(ex);
                return new List<MovieLightJson>();
            }
        }

        /// <summary>
        /// Retrieve an image url from Tmdb
        /// </summary>
        /// <param name="url">Image to retrieve</param>
        /// <returns>Image url</returns>
        public async Task<string> GetImagePathFromTmdb(string url)
        {
            return (await _tmdbService.GetClient).GetImageUrl("original", url, true).AbsoluteUri;
        }

        /// <summary>
        /// Convert a <see cref="Movie"/> to a <see cref="MovieJson"/>
        /// </summary>
        /// <param name="movie"></param>
        /// <returns></returns>
        private MovieJson ConvertMovieToJson(Database.Movie movie)
        {
            return new MovieJson
            {
                Rating = movie.Rating,
                Torrents = movie.Torrents.Select(torrent => new TorrentJson
                {
                    DateUploadedUnix = torrent.DateUploadedUnix,
                    Peers = torrent.Peers,
                    Seeds = torrent.Seeds,
                    Quality = torrent.Quality,
                    Url = torrent.Url,
                    DateUploaded = torrent.DateUploaded,
                    Hash = torrent.Hash,
                    Size = torrent.Size,
                    SizeBytes = torrent.SizeBytes
                }).ToList(),
                Title = movie.Title,
                DateUploadedUnix = movie.DateUploadedUnix,
                Genres = movie.Genres.Select(genre => genre.Name).ToList(),
                Cast = movie.Cast.Select(cast => new CastJson
                {
                    CharacterName = cast.CharacterName,
                    Name = cast.Name,
                    ImdbCode = cast.ImdbCode,
                    SmallImage = cast.SmallImage
                }).ToList(),
                Runtime = movie.Runtime,
                Url = movie.Url,
                Year = movie.Year,
                Slug = movie.Slug,
                LikeCount = movie.LikeCount,
                DownloadCount = movie.DownloadCount,
                ImdbId = movie.ImdbCode,
                DateUploaded = movie.DateUploaded,
                BackgroundImage = movie.BackgroundImage,
                DescriptionFull = movie.DescriptionFull,
                DescriptionIntro = movie.DescriptionIntro,
                Language = movie.Language,
                MpaRating = movie.MpaRating,
                PosterImage = movie.PosterImage,
                TitleLong = movie.TitleLong,
                YtTrailerCode = movie.YtTrailerCode,
                Similars = movie.Similars.Select(a => a.TmdbId).ToList()
            };
        }
    }
}