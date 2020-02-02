namespace Popcorn.Database
{
    using System;
    using System.Collections.Generic;
    
    public partial class Genre
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public Nullable<int> ShowId { get; set; }
    }
}
