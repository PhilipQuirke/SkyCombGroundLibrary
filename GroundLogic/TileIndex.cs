// Copyright SkyComb Limited 2024. All rights reserved. 
using BitMiracle.LibTiff.Classic;
using SkyCombGround.CommonSpace;
using SkyCombGround.GroundModel;
using SkyCombGround.PersistModel;
using System.Drawing;


// Read & index ground & surface elevation data from files on disk 
// Refer https://github.com/PhilipQuirke/SkyCombAnalystHelp/Ground.md for more background
namespace SkyCombGround.GroundLogic
{

    // Build and save a list (index) to the ground contour TIFFs in a single directory into a datastore (xlsx)
    public class TileIndex : BaseConstants
    {
        public const string NzGeoGcs = "NZGD2000";
        public const string DemIndexSuffix = "SkyCombDemIndex.xlsx";
        public const string DsmIndexSuffix = "SkyCombDsmIndex.xlsx";

         
        // Ground directory e.g. D:\SkyComb\Data_Ground\lds-canterbury-lidar-1m-dsm-2020-2023-GTiff\
        public string GroundSubDirectory = "";
        public string DemIndexFileName { get { return GroundSubDirectory + "\\" + DemIndexSuffix; } }
        public string DsmIndexFileName { get { return GroundSubDirectory + "\\" + DsmIndexSuffix; } }


        public TileModelList Tiles;


        // Normal constructor. Loads index
        public TileIndex(string groundSubDirectory, RectangleF targetArea, string theGeoGcs, bool yAxisPositive, bool isDem)
        {
            GroundSubDirectory = groundSubDirectory.TrimEnd('\\'); // Remove any trailing backslash
            Tiles = new();

            string fileName = isDem ? DemIndexFileName : DsmIndexFileName;

            if (IndexLoadSave.Exists(fileName))
            {
                // Load the list of tiles from the datastore that intersects the target area
                IndexLoadSave indexStore = new(fileName);
                indexStore.Load(Tiles, targetArea, theGeoGcs, yAxisPositive);
            }
        }


        // Rebuild constructor. Rarely used. Creates / updates index if needed. Slow.
        public TileIndex(string groundSubDirectory)
        {
            GroundSubDirectory = groundSubDirectory.TrimEnd('\\'); // Remove any trailing backslash
            Tiles = new();

            ValidateOrCreateTileIndex();
        }


        private void AddTile(string tiffFileName, string shortSubdir, string shortFileName)
        {
            var tiff = Tiff.Open(tiffFileName, "r");

            var isDem =
                shortFileName.ToLower().Contains("dem_") ||
                shortFileName.ToLower().Contains("_dem");

            int width = tiff.GetField(TiffTag.IMAGEWIDTH)[0].ToInt();
            int height = tiff.GetField(TiffTag.IMAGELENGTH)[0].ToInt();

            FieldValue[] modelTiePointTag = tiff.GetField(TiffTag.GEOTIFF_MODELTIEPOINTTAG);
            byte[] modelTransformation = modelTiePointTag[1].GetBytes();
            double originX = BitConverter.ToDouble(modelTransformation, 24); // e.g. 1924960
            double originY = BitConverter.ToDouble(modelTransformation, 32); // e.g. 5803440

            FieldValue[] geoScaleTag = tiff.GetField(TiffTag.GEOTIFF_GEOASCIIPARAMSTAG);
            if ((geoScaleTag == null) || (geoScaleTag.Length < 2))
                return;

            var geoGcs = geoScaleTag[1].ToString();
            if (!geoGcs.Contains(NzGeoGcs))
                return;

            geoGcs = NzGeoGcs;

            Tiles.Add(new(shortSubdir, shortFileName, isDem, geoGcs, width, height, originX, originY, 1, 0));

            tiff.Close();
        }


        // Load all tiles and create the index
        public bool CreateTileIndex(string subdir)
        {
            int fileProcessed = 0;

            try
            {
                // Read the names of the tiff files in the directory
                string[] tiffFileNames = Directory.GetFiles(subdir, "*.tif");
                foreach (var tiffFileName in tiffFileNames)
                {
                    try
                    {
                        fileProcessed++;

                        var shortFileName = tiffFileName.Substring(subdir.Length + 1);
                        var shortSubdir = new DirectoryInfo(subdir).Name;

                        AddTile(tiffFileName, shortSubdir, shortFileName);
                    }
                    catch
                    {
                        fileProcessed--;
                        // We ignore files we can't parse 
                    }
                }

                // If we have tiles, create a tile index
                bool success = (Tiles.Count > 0);
                if (success)
                {
                    bool isDem = Tiles.Values[0].IsDem;
                    string fullfileName = isDem ? DemIndexFileName : DsmIndexFileName;
                    IndexLoadSave indexStore = new(fullfileName);
                    indexStore.Save(Tiles);
                    Tiles.Clear();
                }

                return success;
            }
            catch
            {
                Tiles.Clear();
                return false;
            }
        }


        // Usual case is that the user has obtained additional TIFFs and
        // wants to add them to the index. This is a slow process.
        private bool ValidateOrCreateTileIndex()
        {
            if (GroundSubDirectory == "")
                return false;

            // If there are no TIFs in the directory, we dont need an index
            string[] tiffFileNames = Directory.GetFiles(GroundSubDirectory, "*.tif");
            if (tiffFileNames.Length == 0)
            {
                File.Delete(DsmIndexFileName);
                File.Delete(DemIndexFileName);
                return true;
            }

            // If an index exists and is up to date, we dont need to do anything
            for( int i = 0; i < 2; i++)
            { 
                string fileName = i == 0 ? DemIndexFileName : DsmIndexFileName;
                if (IndexLoadSave.Exists(fileName))
                {
                    IndexLoadSave bookStore = new(fileName);
                    int numDataStoreRows = bookStore.Count();

                    if (numDataStoreRows == tiffFileNames.Length)
                        return true; // Index is up to date 

                    // Delete the existing index file as it is out of date
                    File.Delete(fileName);
                }
            }

            // Create the SkyCombIndexTiff.xlsx containing the list of tiles
            return CreateTileIndex(GroundSubDirectory);
        }
    }
}
