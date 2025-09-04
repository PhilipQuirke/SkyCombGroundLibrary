# SkyComb Ground Library

A .NET library for processing geographical elevation data from GeoTIFF files, 
specifically designed for drone flight planning and geospatial analysis.

You can obtain NZ GeoTIFF files covering large areas from the government LINZ department for free.
Stored the GeoTiffs in folder(s). Add folders over time as you explore new physical locations.

The service allows you to define a small area (e.g. the area under a drone flight )
and persist the GeoTIFF information (both DEM and DSM) for that area in a spreadsheet (i.e. an xls) called a DataStore.

## Features

- 🗺️ **Load and process DEM** (Digital Elevation Model) data for ground elevation
- 🌳 **Load and process DSM** (Digital Surface Model) data for surface elevation including vegetation and structures
- 📍 **Point elevation queries** - Get elevation data for specific geographical coordinates
- 🌍 **Coordinate system support** - Handles conversion between global (WGS84) and local coordinate systems
- 📊 **Export capabilities** - Save elevation data to spreadsheets for further analysis
- ⚡ **High precision** - Works with 1m grid resolution and ±0.2m accuracy (for LIDAR data)
- 🔧 **Extensible design** - Built to support multiple countries and data sources

## Quick Start

### Installation

```bash
dotnet add package SkyCombGroundLibrary
```

### Basic Usage

```csharp
using SkyCombGround.Services;
using SkyCombGround.CommonSpace;

// Define your area of interest
var bounds = new GeographicalBounds(
    southwest: new GlobalLocation(-36.8485, 174.7633), // Auckland SW
    northeast: new GlobalLocation(-36.8439, 174.7688)  // Auckland NE
);

// Get elevation data
var groundService = GroundDataService.Create();
using var groundData = await groundService.GetGroundDataAsync(bounds, @"C:\GroundData");

// Query elevation at a point
var location = new GlobalLocation(-36.8462, 174.7660); // Auckland CBD
var elevation = groundData.GetDemElevation(location);
Console.WriteLine($"Ground elevation: {elevation:F1}m");
```

### Drone Flight Path Analysis

```csharp
// Define drone flight path
var flightPath = new List<GlobalLocation>
{
    new(-36.8485, 174.7633),
    new(-36.8475, 174.7643),
    new(-36.8465, 174.7653),
};

// Calculate bounding box and get elevation data
var bounds = GeographicalBounds.FromPoints(flightPath);
using var groundData = await groundService.GetGroundDataAsync(bounds, @"C:\GroundData");

// Analyze elevation profile
foreach (var point in flightPath)
{
    var demElevation = groundData.GetDemElevation(point);
    var dsmElevation = groundData.GetDsmElevation(point);
    var treeHeight = dsmElevation - demElevation;

    Console.WriteLine($"Point {point}: Ground={demElevation:F1}m, Surface={dsmElevation:F1}m, Vegetation={treeHeight:F1}m");
}
```

## Data Requirements

This library works with GeoTIFF elevation files. Currently supports **New Zealand** data sources.

### For New Zealand Data:

1. **Download elevation files** from [LINZ Data Service](https://data.linz.govt.nz/)
   - Search for "LiDAR" datasets
   - Download both DEM (Digital Elevation Model) and DSM (Digital Surface Model) files
   - Files should be in GeoTIFF (.tif) format

2. **Organize files** in the following directory structure:

   ```
   GroundData/
   ├── DEM/
   │   ├── tile_001.tif
   │   ├── tile_002.tif
   │   └── ...
   ├── DSM/
   │   ├── tile_001.tif
   │   ├── tile_002.tif
   │   └── ...
   └── (or organized by region/survey)
   ```

3. **⚠️ IMPORTANT: Rebuild indexes after adding new files**
   
   After adding new GeoTIFF files, you **must** rebuild the elevation data indexes:
   
   ```csharp
   using SkyCombGround.GroundLogic;
   
   // Rebuild indexes for all TIF files in directory and subdirectories
   GroundTiffNZ.RebuildIndexes(@"C:\GroundData");
   ```
   
   **Why this is required:**
   - The library uses Excel-based indexes to quickly locate relevant elevation files
   - These indexes are stored as `.xls` files in each directory containing `.tif` files
   - New elevation files won't be accessible until indexes are rebuilt
   - This is a **one-time operation** after adding new data

4. **Data specifications**:
   - **Format**: GeoTIFF (.tif)
   - **Coordinate System**: NZGD2000 / New Zealand Transverse Mercator 2000 (EPSG:2193)
   - **Resolution**: 1m x 1m grid preferred
   - **Accuracy**: ±0.2m for LiDAR data

### Common Issues

**"No elevation data found" errors:**
- Ensure you've run `GroundTiffNZ.RebuildIndexes()` after adding new TIF files
- Check that TIF files are in the expected coordinate system (NZGD2000)
- Verify directory permissions allow creating `.xls` index files

## Troubleshooting

### "No elevation data found" Errors

This is the most common issue and usually indicates that elevation data indexes need to be rebuilt:

1. **After adding new GeoTIFF files**, always run:
   ```csharp
   GroundDataService.RebuildElevationIndexes(@"C:\GroundData");
   ```

2. **Check file organization**: Ensure TIF files are organized correctly:
   ```
   GroundData/
   ├── region1/
   │   ├── file1.tif
   │   ├── file2.tif  
   │   └── SkyCombIndex.xls  ← Created by RebuildElevationIndexes()
   └── region2/
       ├── file3.tif
       └── SkyCombIndex.xls  ← Created by RebuildElevationIndexes()
   ```

3. **Verify coordinate system**: Files must be in NZGD2000 coordinate system

4. **Check permissions**: Ensure the application can write `.xls` index files to data directories

### Performance Issues

- **Slow loading**: Run `RebuildElevationIndexes()` to optimize file access
- **Memory usage**: Process smaller geographical areas for better performance
- **File access**: Keep elevation data on fast storage (SSD recommended)

## API Reference

### Core Classes

- **`GroundDataService`** - Main service for loading elevation data
- **`GlobalLocation`** - Represents latitude/longitude coordinates
- **`GeographicalBounds`** - Defines rectangular areas using SW/NE corners
- **`ElevationType`** - Enum for DEM vs DSM data types

### Key Methods

```csharp
// Load elevation data for an area
Task<IGroundData> GetGroundDataAsync(GeographicalBounds bounds, string dataDirectory);

// Get elevation at a specific point  
Task<float> GetElevationAtAsync(GlobalLocation location, ElevationType type, string dataDirectory);

// Query loaded elevation data
float GetDemElevation(GlobalLocation location);
float GetDsmElevation(GlobalLocation location);

// ⚠️ IMPORTANT: Rebuild indexes after adding new GeoTIFF files
GroundDataService.RebuildElevationIndexes(string dataDirectory);
```

## Supported Regions
This library currently supports only New Zealand. Support for other regions would require additional development and appropriate data sources.

## Examples

See the [Examples](Examples/) directory for comprehensive usage examples:

- `BasicUsageExamples.cs` - Getting started with elevation queries
- Error handling
- Single point elevation lookup

## Configuration Options

```csharp
var options = new GroundDataOptions
{
    EnableCaching = true,
    CacheExpiration = TimeSpan.FromHours(24),
    MaxConcurrentOperations = Environment.ProcessorCount,
    BufferDistanceM = 50
};

var service = GroundDataService.Create(options);
```

## Error Handling

The library provides specific exception types:

- **`GroundDataNotFoundException`** - No elevation files found
- **`UnsupportedLocationException`** - Location outside supported regions
- **`GeoTiffProcessingException`** - Issues processing GeoTIFF files
- **`InvalidGeographicalBoundsException`** - Invalid coordinate bounds

## Performance Considerations

- **Memory Usage**: Large areas require more memory (1M elevation points ≈ 4MB)
- **Loading Time**: Initial load is slower, subsequent queries are fast
- **File Access**: Keep elevation files on fast storage (SSD preferred)
- **Caching**: Enable caching for repeated access to same areas

## Contributing

We welcome contributions! See [CONTRIBUTING.md](CONTRIBUTING.md) for guidelines.

### Extending the Library

To add support for new countries or regions:

1. Create a new coordinate conversion class (similar to `NztmProjection`)
2. Implement country-specific data loading (similar to `GroundTiffNZ`)  
3. Add country detection logic in `GroundDataService`
4. Update documentation and tests

## Related Projects

This library is part of the SkyComb ecosystem:

- **[SkyComb Analyst](https://github.com/PhilipQuirke/SkyCombAnalyst/)** - Drone thermal video analysis
- **[SkyComb Flights](https://github.com/PhilipQuirke/SkyCombFlights/)** - Drone flight planning
- **[SkyComb Image Library](https://github.com/PhilipQuirke/SkyCombImageLibrary/)** - Drone image processing
- **[SkyComb Drone Library](https://github.com/PhilipQuirke/SkyCombDroneLibrary/)** - Drone flight data processing

## License

MIT License - see [LICENSE](LICENSE) for details.

## Support

- 🐛 **Issues**: [GitHub Issues](https://github.com/PhilipQuirke/SkyCombGroundLibrary/issues)
- 📧 **Email**: Contact through GitHub

---

**Note**: This library requires elevation data files to function. For New Zealand users, free high-quality LiDAR data is available from LINZ. Other regions require appropriate GeoTIFF elevation files.