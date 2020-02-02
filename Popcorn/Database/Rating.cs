namespace Popcorn.Database
{
    using System;
    using System.Collections.Generic;
    
    public partial class Rating
    {
        public int Id { get; set; }
        public int Percentage { get; set; }
        public int Watching { get; set; }
        public int Votes { get; set; }
        public int Loved { get; set; }
        public int Hated { get; set; }
    }
}
