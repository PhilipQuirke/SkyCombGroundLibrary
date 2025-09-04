// Copyright SkyComb Limited 2025. All rights reserved.
using SkyCombGround.CommonSpace;

namespace SkyCombGround.Interfaces
{
    /// <summary>
    /// Defines the types of elevation data available
    /// </summary>
    public enum ElevationType
    {
        /// <summary>
        /// Digital Elevation Model - represents ground/earth surface elevation
        /// </summary>
        DEM,
        
        /// <summary>
        /// Digital Surface Model - represents tree-top/surface elevation including vegetation and structures
        /// </summary>
        DSM
    }

    /// <summary>
    /// Represents geographical boundaries defined by southwest and northeast corners
    /// </summary>
    public class GeographicalBounds
    {
        /// <summary>
        /// Gets the southwest corner of the bounds
        /// </summary>
        public GlobalLocation Southwest { get; }
        
        /// <summary>
        /// Gets the northeast corner of the bounds
        /// </summary>
        public GlobalLocation Northeast { get; }

        /// <summary>
        /// Initializes a new geographical bounds instance
        /// </summary>
        /// <param name="southwest">The southwest corner</param>
        /// <param name="northeast">The northeast corner</param>
        /// <exception cref="ArgumentNullException">Thrown when southwest or northeast is null</exception>
        /// <exception cref="ArgumentException">Thrown when bounds are invalid (northeast not actually northeast of southwest)</exception>
        public GeographicalBounds(GlobalLocation southwest, GlobalLocation northeast)
        {
            Southwest = southwest ?? throw new ArgumentNullException(nameof(southwest));
            Northeast = northeast ?? throw new ArgumentNullException(nameof(northeast));

            if (northeast.Latitude <= southwest.Latitude || northeast.Longitude <= southwest.Longitude)
            {
                throw new ArgumentException("Northeast point must be northeast of southwest point");
            }
        }

        /// <summary>
        /// Creates geographical bounds from a collection of points
        /// </summary>
        /// <param name="points">Collection of geographical points</param>
        /// <returns>Bounds that encompass all points</returns>
        /// <exception cref="ArgumentException">Thrown when points collection is null or empty</exception>
        public static GeographicalBounds FromPoints(IEnumerable<GlobalLocation> points)
        {
            if (points == null || !points.Any())
                throw new ArgumentException("Points collection cannot be null or empty", nameof(points));

            var pointsList = points.ToList();
            var minLat = pointsList.Min(p => p.Latitude);
            var maxLat = pointsList.Max(p => p.Latitude);
            var minLon = pointsList.Min(p => p.Longitude);
            var maxLon = pointsList.Max(p => p.Longitude);

            return new GeographicalBounds(
                new GlobalLocation(minLat, minLon),
                new GlobalLocation(maxLat, maxLon)
            );
        }

        /// <summary>
        /// Checks if a point is contained within these bounds
        /// </summary>
        /// <param name="location">The location to check</param>
        /// <returns>True if the location is within the bounds</returns>
        public bool Contains(GlobalLocation location)
        {
            if (location == null) return false;

            return location.Latitude >= Southwest.Latitude &&
                   location.Latitude <= Northeast.Latitude &&
                   location.Longitude >= Southwest.Longitude &&
                   location.Longitude <= Northeast.Longitude;
        }
    }

    /// <summary>
    /// Provides access to ground elevation data (DEM and DSM)
    /// </summary>
    public interface IGroundData : IDisposable
    {
        /// <summary>
        /// Gets a value indicating whether DEM (Digital Elevation Model) data is available
        /// </summary>
        bool HasDemData { get; }

        /// <summary>
        /// Gets a value indicating whether DSM (Digital Surface Model) data is available
        /// </summary>
        bool HasDsmData { get; }

        /// <summary>
        /// Gets the geographical bounds of the loaded elevation data
        /// </summary>
        GeographicalBounds Bounds { get; }

        /// <summary>
        /// Gets the DEM (ground) elevation at the specified location
        /// </summary>
        /// <param name="location">The geographical location</param>
        /// <returns>Elevation in meters above sea level, or NaN if no data available</returns>
        float GetDemElevation(GlobalLocation location);

        /// <summary>
        /// Gets the DSM (surface) elevation at the specified location
        /// </summary>
        /// <param name="location">The geographical location</param>
        /// <returns>Elevation in meters above sea level, or NaN if no data available</returns>
        float GetDsmElevation(GlobalLocation location);

        /// <summary>
        /// Gets elevation at the specified location for the given elevation type
        /// </summary>
        /// <param name="location">The geographical location</param>
        /// <param name="elevationType">Type of elevation data to retrieve</param>
        /// <returns>Elevation in meters above sea level, or NaN if no data available</returns>
        float GetElevation(GlobalLocation location, ElevationType elevationType);
    }

    /// <summary>
    /// Service for loading and processing ground elevation data
    /// </summary>
    public interface IGroundDataService
    {
        /// <summary>
        /// Gets ground elevation data (DEM and DSM) for the specified geographical area
        /// </summary>
        /// <param name="bounds">The geographical bounds to get elevation data for</param>
        /// <param name="dataDirectory">Directory containing elevation data files</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Ground elevation data including DEM and DSM models</returns>
        /// <exception cref="ArgumentNullException">Thrown when bounds or dataDirectory is null</exception>
        /// <exception cref="DirectoryNotFoundException">Thrown when dataDirectory does not exist</exception>
        /// <exception cref="GroundDataNotFoundException">Thrown when no elevation data files are found</exception>
        /// <exception cref="UnsupportedLocationException">Thrown when the location is not supported</exception>
        Task<IGroundData> GetGroundDataAsync(GeographicalBounds bounds, string dataDirectory, CancellationToken cancellationToken = default);

        /// <summary>
        /// Gets elevation at a specific geographical point
        /// </summary>
        /// <param name="location">The geographical location</param>
        /// <param name="elevationType">Type of elevation (DEM or DSM)</param>
        /// <param name="dataDirectory">Directory containing elevation data files</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Elevation in meters above sea level, or NaN if no data available</returns>
        Task<float> GetElevationAtAsync(GlobalLocation location, ElevationType elevationType, string dataDirectory, CancellationToken cancellationToken = default);
    }
}