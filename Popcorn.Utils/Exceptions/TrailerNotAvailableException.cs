﻿using System;

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
