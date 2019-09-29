using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;
using RestSharp.Deserializers;

namespace Popcorn.Models.Trailer
{
    public class TrailerResponse
    {
        [DataMember(Name = "trailer_url")]
        public string TrailerUrl { get; set; }
    }
}
