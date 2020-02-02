namespace Popcorn.Database
{
    using System;
    using System.Collections.Generic;
    
    public partial class EpisodeShow
    {
        public int Id { get; set; }
        public long FirstAired { get; set; }
        public bool DateBased { get; set; }
        public string Overview { get; set; }
        public string Title { get; set; }
        public int EpisodeNumber { get; set; }
        public int Season { get; set; }
        public Nullable<int> TvdbId { get; set; }
    
        public virtual TorrentNode Torrents { get; set; }
    }
}
