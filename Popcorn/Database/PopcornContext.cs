using Microsoft.EntityFrameworkCore;

namespace Popcorn.Database
{
    public class PopcornContext : DbContext
    {
        public PopcornContext(DbContextOptions<PopcornContext> options)
            : base(options)
        {
        }

        public virtual DbSet<Movie> MovieSet { get; set; }
        public virtual DbSet<Genre> GenreSet { get; set; }
        public virtual DbSet<Cast> CastSet { get; set; }
        public virtual DbSet<TorrentMovie> TorrentMovieSet { get; set; }
        public virtual DbSet<Show> ShowSet { get; set; }
        public virtual DbSet<EpisodeShow> EpisodeShowSet { get; set; }
        public virtual DbSet<TorrentNode> TorrentNodeSet { get; set; }
        public virtual DbSet<Torrent> TorrentSet { get; set; }
        public virtual DbSet<ImageShow> ImageShowSet { get; set; }
        public virtual DbSet<Rating> RatingSet { get; set; }
    }
}