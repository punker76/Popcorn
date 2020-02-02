namespace Popcorn.Database
{
    using System;
    using System.Collections.Generic;
    
    public partial class Torrent
    {
        public int Id { get; set; }
        public string Provider { get; set; }
        public Nullable<int> Peers { get; set; }
        public Nullable<int> Seeds { get; set; }
        public string Url { get; set; }
    }
}
