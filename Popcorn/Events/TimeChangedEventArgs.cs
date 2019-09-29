using System;

namespace Popcorn.Events
{
    public class TimeChangedEventArgs : EventArgs
    {
        /// <summary>
        /// The time in seconds
        /// </summary>
        public readonly double Seconds;

        /// <summary>
        /// Initialize a new instance of TimeChangedEventArgs
        /// </summary>
        /// <param name="seconds">Time in seconds</param>
        public TimeChangedEventArgs(double seconds)
        {
            Seconds = seconds;
        }
    }
}
