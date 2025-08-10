// Copyright SkyComb Limited 2025. All rights reserved.
using SkyCombGround.CommonSpace;
using SkyCombGround.Interfaces;
using SkyCombGround.Services;

namespace SkyCombGround.Examples
{
    /// <summary>
    /// Examples showing how to use the SkyCombGroundLibrary for common scenarios
    /// </summary>
    public static class BasicUsageExamples
    {
        /// <summary>
        /// Basic usage example - get elevation data for an area around Auckland, New Zealand
        /// </summary>
        public static async Task BasicElevationExample()
        {
            Console.WriteLine("=== Basic Elevation Data Example ===");

            // Define the area you want elevation data for (Auckland area)
            var bounds = new GeographicalBounds(
                southwest: new GlobalLocation(-36.8485, 174.7633), // Auckland SW
                northeast: new GlobalLocation(-36.8439, 174.7688)  // Auckland NE
            );

            Console.WriteLine($"Loading elevation data for bounds: {bounds.Southwest} to {bounds.Northeast}");

            // Create the service
            var groundService = GroundDataService.Create();

            try
            {
                // Get elevation data (requires local GeoTIFF files)
                // Note: You need to have elevation data files in the specified directory
                using var groundData = await groundService.GetElevationDataAsync(bounds, @"C:\ElevationData");

                Console.WriteLine($"Elevation data loaded successfully!");
                Console.WriteLine($"  DEM data available: {groundData.HasDemData}");
                Console.WriteLine($"  DSM data available: {groundData.HasDsmData}");

                // Query elevation at a specific point
                if (groundData.HasDemData)
                {
                    var location = new GlobalLocation(-36.8462, 174.7660); // Auckland CBD
                    var demElevation = groundData.GetDemElevation(location);
                    var dsmElevation = groundData.GetDsmElevation(location);

                    Console.WriteLine($"  Point {location}:");
                    Console.WriteLine($"    Ground elevation (DEM): {demElevation:F1}m");
                    
                    if (!float.IsNaN(dsmElevation))
                    {
                        Console.WriteLine($"    Surface elevation (DSM): {dsmElevation:F1}m");
                        Console.WriteLine($"    Vegetation/structure height: {dsmElevation - demElevation:F1}m");
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error loading elevation data: {ex.Message}");
                Console.WriteLine("Note: This example requires GeoTIFF elevation files in the specified directory");
                Console.WriteLine("For New Zealand, you can download these from LINZ: https://data.linz.govt.nz/");
                Console.WriteLine("IMPORTANT: After adding new TIF files, run GroundDataService.RebuildElevationIndexes()");
            }
        }

        /// <summary>
        /// Example showing how to rebuild elevation data indexes after adding new GeoTIFF files
        /// </summary>
        public static async Task RebuildIndexExample()
        {
            Console.WriteLine("\n=== Rebuild Elevation Index Example ===");
            
            string dataDirectory = @"C:\ElevationData";
            Console.WriteLine($"Rebuilding elevation indexes for: {dataDirectory}");
            Console.WriteLine("This may take several minutes for large datasets...");

            try
            {
                // Rebuild indexes after adding new GeoTIFF files
                // This scans all subdirectories and creates Excel-based index files
                GroundDataService.RebuildElevationIndexes(dataDirectory);
                
                Console.WriteLine("? Elevation indexes rebuilt successfully!");
                Console.WriteLine("New GeoTIFF files are now available for elevation queries.");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"? Error rebuilding indexes: {ex.Message}");
            }
        }

        /// <summary>
        /// Example showing how to work with drone flight paths
        /// </summary>
        public static async Task DroneFlightPathExample()
        {
            Console.WriteLine("\n=== Drone Flight Path Example ===");

            // Define a drone flight path (example coordinates in Wellington, NZ)
            var flightPath = new List<GlobalLocation>
            {
                new(-41.2865, 174.7762), // Wellington Harbor start
                new(-41.2845, 174.7782), // Flight waypoint 1
                new(-41.2825, 174.7802), // Flight waypoint 2
                new(-41.2805, 174.7822), // Flight endpoint
            };

            Console.WriteLine($"Analyzing flight path with {flightPath.Count} waypoints");

            try
            {
                // Calculate bounding box for the flight path
                var bounds = GeographicalBounds.FromPoints(flightPath);
                Console.WriteLine($"Flight bounds: {bounds.Southwest} to {bounds.Northeast}");

                var groundService = GroundDataService.Create();
                using var groundData = await groundService.GetElevationDataAsync(bounds, @"C:\ElevationData");

                // Analyze elevation profile along flight path
                Console.WriteLine("\nElevation profile:");
                for (int i = 0; i < flightPath.Count; i++)
                {
                    var point = flightPath[i];
                    var demElevation = groundData.GetDemElevation(point);
                    var dsmElevation = groundData.GetDsmElevation(point);

                    Console.WriteLine($"  Waypoint {i + 1}: {point}");
                    if (!float.IsNaN(demElevation))
                    {
                        Console.WriteLine($"    Ground: {demElevation:F1}m");
                        if (!float.IsNaN(dsmElevation))
                        {
                            var treeHeight = dsmElevation - demElevation;
                            Console.WriteLine($"    Surface: {dsmElevation:F1}m (vegetation: {treeHeight:F1}m)");
                        }
                    }
                    else
                    {
                        Console.WriteLine("    No elevation data available");
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error analyzing flight path: {ex.Message}");
            }
        }

        /// <summary>
        /// Example showing how to get elevation for a single point
        /// </summary>
        public static async Task SinglePointElevationExample()
        {
            Console.WriteLine("\n=== Single Point Elevation Example ===");

            // Example location: Mount Cook, New Zealand (highest peak)
            var mountCook = new GlobalLocation(-43.5950, 170.1418);
            Console.WriteLine($"Getting elevation for Mount Cook: {mountCook}");

            try
            {
                var groundService = GroundDataService.Create();

                // Get DEM elevation for this single point
                var demElevation = await groundService.GetElevationAtAsync(
                    mountCook, 
                    ElevationType.DEM, 
                    @"C:\ElevationData");

                // Get DSM elevation for this single point
                var dsmElevation = await groundService.GetElevationAtAsync(
                    mountCook, 
                    ElevationType.DSM, 
                    @"C:\ElevationData");

                if (!float.IsNaN(demElevation))
                {
                    Console.WriteLine($"  Ground elevation: {demElevation:F1}m");
                    if (!float.IsNaN(dsmElevation))
                    {
                        Console.WriteLine($"  Surface elevation: {dsmElevation:F1}m");
                    }
                }
                else
                {
                    Console.WriteLine("  No elevation data available for this location");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error getting single point elevation: {ex.Message}");
            }
        }

        /// <summary>
        /// Example showing error handling for unsupported locations
        /// </summary>
        public static async Task ErrorHandlingExample()
        {
            Console.WriteLine("\n=== Error Handling Example ===");

            // Try to get elevation data for a location outside New Zealand (e.g., Sydney, Australia)
            var sydney = new GlobalLocation(-33.8688, 151.2093);
            Console.WriteLine($"Attempting to get elevation for Sydney (outside NZ): {sydney}");

            var groundService = GroundDataService.Create();

            try
            {
                var bounds = new GeographicalBounds(
                    new GlobalLocation(-33.9000, 151.1500),
                    new GlobalLocation(-33.8500, 151.2500)
                );

                using var groundData = await groundService.GetElevationDataAsync(bounds, @"C:\ElevationData");
                Console.WriteLine("  Unexpected success - this should have failed!");
            }
            catch (Exceptions.UnsupportedLocationException ex)
            {
                Console.WriteLine($"  Expected error: {ex.Message}");
                Console.WriteLine($"  Unsupported location: {ex.Location}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"  Other error: {ex.Message}");
            }
        }

        /// <summary>
        /// Runs all the examples
        /// </summary>
        public static async Task RunAllExamples()
        {
            Console.WriteLine("SkyCombGroundLibrary Usage Examples");
            Console.WriteLine("==================================");
            
            await RebuildIndexExample(); // Show this first as it's often needed
            await BasicElevationExample();
            await DroneFlightPathExample();
            await SinglePointElevationExample();
            await ErrorHandlingExample();

            Console.WriteLine("\n=== Examples Complete ===");
            Console.WriteLine("Note: To run these examples successfully, you need:");
            Console.WriteLine("1. GeoTIFF elevation files in C:\\ElevationData directory");
            Console.WriteLine("2. Data covering the New Zealand coordinates used in examples");
            Console.WriteLine("3. For New Zealand data, visit: https://data.linz.govt.nz/");
            Console.WriteLine("4. IMPORTANT: Run GroundDataService.RebuildElevationIndexes() after adding new TIF files!");
        }
    }

    /// <summary>
    /// Program entry point for running the examples
    /// </summary>
    internal class Program
    {
        static async Task Main(string[] args)
        {
            await BasicUsageExamples.RunAllExamples();
            
            Console.WriteLine("\nPress any key to exit...");
            Console.ReadKey();
        }
    }
}