namespace Popcorn.Database
{
    using System;
    using System.Collections.Generic;
    
    public partial class TorrentMovie
    {
        public int Id { get; set; }
        public string Url { get; set; }
        public string Hash { get; set; }
        public string Quality { get; set; }
        public int Seeds { get; set; }
        public int Peers { get; set; }
        public string Size { get; set; }
        public Nullable<long> SizeBytes { get; set; }
        public string DateUploaded { get; set; }
        public int DateUploadedUnix { get; set; }
        public int MovieId { get; set; }
    }
}
