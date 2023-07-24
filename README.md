# [SkyComb Ground Library](https://github.com/PhilipQuirke/SkyCombGroundLibrary/) 

SkyComb Ground Library is a library that:
- takes as input a rectangular area drone-flight-area in global coordinates (longitude/latitude)
- reads from a library of ground contour elevations (DEM & DSM) in GeoTIFF format, and 
- generates a drone-flight-area DEM & DSM data set in local coordinates (NZTM)   

This "ground data" library is used by the tools / libraries:
- [SkyComb Analyst](https://github.com/PhilipQuirke/SkyCombAnalyst/) 
- [SkyComb Flights](https://github.com/PhilipQuirke/SkyCombFlights/)
- [SkyComb Library](https://github.com/PhilipQuirke/SkyDroneLibrary/)

The folders are:
- CommonSpace: Constants and generic code shared by SkyCombGroundLibrary, SkyCombDroneLibrary, SkyCombFlights & SkyCombAnalyst
- GroundSpace: From drone-flight-area, calculate and return the elevations (DEM & DSM) 
- PersistModel: Save/load drone-flight-area ground data from/to the datastore (xls)

In NZ, the GeoTIFFs can be obtained for free from LINZ as described in [SkyCombAnalystHelp-Groubnd](https://github.com/PhilipQuirke/SkyCombAnalystHelp/blob/main/Ground.md)
