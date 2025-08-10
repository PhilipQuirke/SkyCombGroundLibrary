// Copyright SkyComb Limited 2025. All rights reserved.

namespace SkyCombGround.Configuration
{
    /// <summary>
    /// Configuration options for ground data processing
    /// </summary>
    public class GroundDataOptions
    {
        /// <summary>
        /// Gets or sets the default data directory containing elevation files
        /// </summary>
        public string DataDirectory { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets a value indicating whether to enable caching of processed elevation data
        /// </summary>
        public bool EnableCaching { get; set; } = true;

        /// <summary>
        /// Gets or sets the cache expiration time for processed elevation data
        /// </summary>
        public TimeSpan CacheExpiration { get; set; } = TimeSpan.FromHours(24);

        /// <summary>
        /// Gets or sets the maximum number of concurrent operations when processing elevation data
        /// </summary>
        public int MaxConcurrentOperations { get; set; } = Environment.ProcessorCount;

        /// <summary>
        /// Gets or sets the buffer distance in meters to add around the requested area
        /// </summary>
        public int BufferDistanceM { get; set; } = 50;

        /// <summary>
        /// Gets or sets the timeout for individual GeoTIFF file processing operations
        /// </summary>
        public TimeSpan FileProcessingTimeout { get; set; } = TimeSpan.FromMinutes(5);
    }

}