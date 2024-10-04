# [SkyComb Ground Library](https://github.com/PhilipQuirke/SkyCombGroundLibrary/) 

SkyComb Ground Library is a library that:
- takes as input a rectangular drone-flight physical area in global coordinates (longitude/latitude)
- reads from a local folder tree of ground contour elevations (DEM & DSM) data files in GeoTIFF format 
- finds the DEM (surface) & DSM (tree-top) elevation data under and around the drone flight area, and 
- saves the DEM and DSM data in a spreadsheet   

This "ground data" library is incorporated into the tools / libraries:
- [SkyComb Analyst](https://github.com/PhilipQuirke/SkyCombAnalyst/) 
- [SkyComb Flights](https://github.com/PhilipQuirke/SkyCombFlights/)
- [SkyComb Library](https://github.com/PhilipQuirke/SkyDroneLibrary/)

The code folders are:
- **CommonSpace:** Constants and generic code shared by SkyCombGroundLibrary, SkyCombDroneLibrary, SkyCombFlights & SkyCombAnalyst
- **GroundModel:** In-memory structures for the ground data
- **GroundLogic:** From drone-flight-area, calculate and return the elevations (DEM & DSM) 
- **PersistModel:** Save/load drone-flight-area ground data from/to the datastore (xls)

This tools is currently aimed at New Zealand (NZ) drone flights. But is easily extensible to other countries.

In NZ, the GeoTIFFs can be downloaded for free from LINZ.
The [SkyCombAnalystHelp-Ground](https://github.com/PhilipQuirke/SkyCombAnalystHelp/blob/main/Ground.md) page describes
this process and how to do the one-off process of setting up the local folder tree of DEM & DSM data files.

The spreadsheet DEM, DSM and Swathe tabs are hidden as they are not human-readable.