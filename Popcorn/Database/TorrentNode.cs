namespace Popcorn.Database
{
    using System;
    using System.Collections.Generic;
    
    public partial class TorrentNode
    {
        public int Id { get; set; }
    
        public virtual Torrent Torrent0 { get; set; }
        public virtual Torrent Torrent480p { get; set; }
        public virtual Torrent Torrent720p { get; set; }
        public virtual Torrent Torrent1080p { get; set; }
    }
}
