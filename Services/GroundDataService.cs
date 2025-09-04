// Copyright SkyComb Limited 2025. All rights reserved.
using Microsoft.Extensions.Logging;
using SkyCombGround.CommonSpace;
using SkyCombGround.Configuration;
using SkyCombGround.Exceptions;
using SkyCombGround.GroundLogic;
using SkyCombGround.Interfaces;

namespace SkyCombGround.Services
{
    /// <summary>
    /// Main service for accessing ground elevation data with a simplified public API
    /// </summary>
    public class GroundDataService : IGroundDataService
    {
        private readonly ILogger<GroundDataService> _logger;
        private readonly GroundDataOptions _options;

        /// <summary>
        /// Initializes a new instance of the GroundDataService class
        /// </summary>
        /// <param name="logger">Optional logger for diagnostic information</param>
        /// <param name="options">Optional configuration options</param>
        public GroundDataService(ILogger<GroundDataService>? logger = null, GroundDataOptions? options = null)
        {
            _logger = logger ?? Microsoft.Extensions.Logging.Abstractions.NullLogger<GroundDataService>.Instance;
            _options = options ?? new GroundDataOptions();
        }

        /// <summary>
        /// Creates a new instance of GroundDataService with default configuration
        /// </summary>
        /// <returns>A new GroundDataService instance</returns>
        public static GroundDataService Create() => new();

        /// <summary>
        /// Creates a new instance of GroundDataService with custom options
        /// </summary>
        /// <param name="options">Configuration options</param>
        /// <returns>A new GroundDataService instance</returns>
        public static GroundDataService Create(GroundDataOptions options) => new(null, options);

        /// <summary>
        /// Rebuilds elevation data indexes after adding new GeoTIFF files
        /// </summary>
        /// <param name="dataDirectory">Root directory containing elevation data files</param>
        /// <remarks>
        /// This method must be called after adding new GeoTIFF files to make them available to the library.
        /// It scans all subdirectories and creates Excel-based indexes that catalog available elevation files.
        /// This is a one-time operation after adding new data and may take several minutes for large datasets.
        /// </remarks>
        public static void RebuildElevationIndexes(string dataDirectory)
        {
            if (string.IsNullOrWhiteSpace(dataDirectory))
                throw new ArgumentNullException(nameof(dataDirectory));

            if (!Directory.Exists(dataDirectory))
                throw new DirectoryNotFoundException($"Data directory not found: {dataDirectory}");

            GroundTiffNZ.RebuildIndexes(dataDirectory);
        }

        /// <inheritdoc />
        public async Task<IGroundData> GetGroundDataAsync(GeographicalBounds bounds, string dataDirectory, CancellationToken cancellationToken = default)
        {
            if (bounds == null)
                throw new ArgumentNullException(nameof(bounds));
            
            if (string.IsNullOrWhiteSpace(dataDirectory))
                throw new ArgumentNullException(nameof(dataDirectory));

            if (!Directory.Exists(dataDirectory))
                throw new DirectoryNotFoundException($"Data directory not found: {dataDirectory}");

            _logger.LogDebug("Loading elevation data for bounds {Southwest} to {Northeast}", bounds.Southwest, bounds.Northeast);

            try
            {
                // Validate location is in New Zealand (only supported region)
                ValidateNewZealandLocation(bounds);

                // Create internal GroundData instance
                var groundData = GroundDataFactory.Create();

                // Calculate elevations using the internal API
                await Task.Run(() =>
                {
                    groundData.GlobalCalculateElevations(bounds.Southwest, bounds.Northeast, dataDirectory);
                }, cancellationToken);

                // Wrap in our public interface
                var result = new GroundDataWrapper(groundData, bounds, _logger);
                
                _logger.LogInformation("Successfully loaded elevation data. DEM: {HasDem}, DSM: {HasDsm}", 
                    result.HasDemData, result.HasDsmData);

                return result;
            }
            catch (Exception ex) when (!(ex is GroundDataException))
            {
                _logger.LogError(ex, "Failed to load elevation data for bounds {Southwest} to {Northeast}", 
                    bounds.Southwest, bounds.Northeast);
                throw new GroundDataException("Failed to load elevation data", ex);
            }
        }

        /// <inheritdoc />
        public async Task<float> GetElevationAtAsync(GlobalLocation location, ElevationType elevationType, string dataDirectory, CancellationToken cancellationToken = default)
        {
            if (location == null)
                throw new ArgumentNullException(nameof(location));

            // Create a small bounds around the point
            const double buffer = 0.001; // About 100 meters
            var bounds = new GeographicalBounds(
                new GlobalLocation(location.Latitude - buffer, location.Longitude - buffer),
                new GlobalLocation(location.Latitude + buffer, location.Longitude + buffer)
            );

            using var groundData = await GetGroundDataAsync(bounds, dataDirectory, cancellationToken);
            return groundData.GetElevation(location, elevationType);
        }

        /// <summary>
        /// Validates that the given bounds are within New Zealand (the only supported region)
        /// </summary>
        /// <param name="bounds">Geographical bounds to validate</param>
        /// <exception cref="UnsupportedLocationException">Thrown when location is not in New Zealand</exception>
        private void ValidateNewZealandLocation(GeographicalBounds bounds)
        {
            // Check if location is in New Zealand bounds
            bool isInNZ = IsInNewZealand(bounds.Southwest) && IsInNewZealand(bounds.Northeast);

            if (!isInNZ)
            {
                throw new UnsupportedLocationException(bounds.Southwest);
            }
        }

        /// <summary>
        /// Checks if a location is within New Zealand bounds
        /// </summary>
        /// <param name="location">Location to check</param>
        /// <returns>True if location is in New Zealand</returns>
        private static bool IsInNewZealand(GlobalLocation location)
        {
            return location.Latitude >= -50 && location.Latitude <= -34 &&
                   location.Longitude >= 165 && location.Longitude <= 179;
        }
    }

    /// <summary>
    /// Internal wrapper that adapts the existing GroundData to the public IGroundData interface
    /// </summary>
    internal class GroundDataWrapper : IGroundData
    {
        private readonly GroundData _groundData;
        private readonly GeographicalBounds _bounds;
        private readonly ILogger _logger;
        private bool _disposed = false;

        public GroundDataWrapper(GroundData groundData, GeographicalBounds bounds, ILogger logger)
        {
            _groundData = groundData ?? throw new ArgumentNullException(nameof(groundData));
            _bounds = bounds ?? throw new ArgumentNullException(nameof(bounds));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public bool HasDemData => _groundData.HasDemModel;
        public bool HasDsmData => _groundData.HasDsmModel;
        public GeographicalBounds Bounds => _bounds;

        public float GetDemElevation(GlobalLocation location)
        {
            if (_disposed)
                throw new ObjectDisposedException(nameof(GroundDataWrapper));

            if (location == null)
                return float.NaN;

            try
            {
                if (_groundData.DemModel == null)
                    return float.NaN;

                return _groundData.DemModel.GetElevationByGlobalLocation(location);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to get DEM elevation for location {Location}", location);
                return float.NaN;
            }
        }

        public float GetDsmElevation(GlobalLocation location)
        {
            if (_disposed)
                throw new ObjectDisposedException(nameof(GroundDataWrapper));

            if (location == null)
                return float.NaN;

            try
            {
                if (_groundData.DsmModel == null)
                    return float.NaN;

                return _groundData.DsmModel.GetElevationByGlobalLocation(location);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to get DSM elevation for location {Location}", location);
                return float.NaN;
            }
        }

        public float GetElevation(GlobalLocation location, ElevationType elevationType)
        {
            return elevationType switch
            {
                ElevationType.DEM => GetDemElevation(location),
                ElevationType.DSM => GetDsmElevation(location),
                _ => throw new ArgumentOutOfRangeException(nameof(elevationType))
            };
        }

        public void Dispose()
        {
            if (!_disposed)
            {
                _groundData?.Dispose();
                _disposed = true;
            }
        }
    }
}