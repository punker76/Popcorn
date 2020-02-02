namespace Popcorn.Database
{
    using System;
    using System.Collections.Generic;
    
    public partial class Cast
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string CharacterName { get; set; }
        public string SmallImage { get; set; }
        public string ImdbCode { get; set; }
    }
}
