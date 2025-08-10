// Copyright SkyComb Limited 2025. All rights reserved.
using SkyCombGround.CommonSpace;

namespace SkyCombGround.Exceptions
{
    /// <summary>
    /// Base exception for all ground data related errors
    /// </summary>
    public class GroundDataException : Exception
    {
        /// <summary>
        /// Initializes a new instance of the GroundDataException class
        /// </summary>
        /// <param name="message">The error message</param>
        public GroundDataException(string message) : base(message) { }

        /// <summary>
        /// Initializes a new instance of the GroundDataException class
        /// </summary>
        /// <param name="message">The error message</param>
        /// <param name="innerException">The inner exception</param>
        public GroundDataException(string message, Exception innerException) : base(message, innerException) { }
    }

    /// <summary>
    /// Exception thrown when a geographical location is not supported by the library
    /// </summary>
    public class UnsupportedLocationException : GroundDataException
    {
        /// <summary>
        /// Gets the unsupported location
        /// </summary>
        public GlobalLocation Location { get; }

        /// <summary>
        /// Initializes a new instance of the UnsupportedLocationException class
        /// </summary>
        /// <param name="location">The unsupported geographical location</param>
        public UnsupportedLocationException(GlobalLocation location) 
            : base($"Location {location} is not supported by this library. Currently only New Zealand is supported.")
        {
            Location = location;
        }

        /// <summary>
        /// Initializes a new instance of the UnsupportedLocationException class
        /// </summary>
        /// <param name="location">The unsupported geographical location</param>
        /// <param name="innerException">The inner exception</param>
        public UnsupportedLocationException(GlobalLocation location, Exception innerException) 
            : base($"Location {location} is not supported by this library", innerException)
        {
            Location = location;
        }
    }

}