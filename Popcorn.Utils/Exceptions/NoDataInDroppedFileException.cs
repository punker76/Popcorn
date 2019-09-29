using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Popcorn.Utils.Exceptions
{
    [Serializable]
    public class NoDataInDroppedFileException : Exception
    {
        /// <summary>
        /// Constructor
        /// </summary>
        public NoDataInDroppedFileException()
        {
        }

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="message">Message</param>
        public NoDataInDroppedFileException(string message) : base(message)
        {
        }
    }
}
