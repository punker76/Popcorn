using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Popcorn.Utils.Exceptions
{
    [Serializable]
    public class TrailerNotAvailableException : Exception
    {
        /// <summary>
        /// Constructor
        /// </summary>
        public TrailerNotAvailableException()
        {
        }

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="message">Message</param>
        public TrailerNotAvailableException(string message) : base(message)
        {
        }
    }
}
