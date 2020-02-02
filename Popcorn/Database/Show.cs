namespace Popcorn.Database
{
    using System;
    using System.Collections.Generic;
    
    public partial class Show
    {
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA2214:DoNotCallOverridableMethodsInConstructors")]
        public Show()
        {
            this.Episodes = new HashSet<EpisodeShow>();
            this.Genres = new HashSet<Genre>();
            this.Similars = new HashSet<Similar>();
        }

        public int Id { get; set; }
        public string ImdbId { get; set; }
        public string TvdbId { get; set; }
        public string Title { get; set; }
        public int Year { get; set; }
        public string GenreNames { get; set; }
        public string Slug { get; set; }
        public string Synopsis { get; set; }
        public string Runtime { get; set; }
        public string Country { get; set; }
        public string Network { get; set; }
        public string AirDay { get; set; }
        public string AirTime { get; set; }
        public string Status { get; set; }
        public int NumSeasons { get; set; }
        public long LastUpdated { get; set; }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA2227:CollectionPropertiesShouldBeReadOnly")]
        public virtual ICollection<Similar> Similars { get; set; }
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA2227:CollectionPropertiesShouldBeReadOnly")]
        public virtual ICollection<EpisodeShow> Episodes { get; set; }
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA2227:CollectionPropertiesShouldBeReadOnly")]
        public virtual ICollection<Genre> Genres { get; set; }
        public virtual ImageShow Images { get; set; }
        public virtual Rating Rating { get; set; }
    }
}
