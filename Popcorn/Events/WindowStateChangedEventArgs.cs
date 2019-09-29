using System;

namespace Popcorn.Events
{
    /// <summary>
    /// Used to transmit a window state change
    /// </summary>
    public class WindowStateChangedEventArgs : EventArgs
    {
        /// <summary>
        /// Is movie playing
        /// </summary>
        public readonly bool IsMoviePlaying;

        /// <summary>
        /// Initialize a new instance of WindowStateChangedEventArgs
        /// </summary>
        /// <param name="isMoviePlaying">Is movie playing</param>
        public WindowStateChangedEventArgs(bool isMoviePlaying)
        {
            IsMoviePlaying = isMoviePlaying;
        }
    }
}