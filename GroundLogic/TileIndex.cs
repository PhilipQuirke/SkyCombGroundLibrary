// Copyright SkyComb Limited 2023. All rights reserved. 
using BitMiracle.LibTiff.Classic;
using SkyCombGround.CommonSpace;
using SkyCombGround.GroundModel;
using SkyCombGround.PersistModel;
using System.Drawing;


// Read & index ground & surface elevation data from files on disk 
// Refer https://github.com/PhilipQuirke/SkyCombAnalystHelp/Ground.md for more background
namespace SkyCombGround.GroundLogic
{
    // Build and save a list (index) to the ground contour TIFFs in a datastore
    public class TileIndex : BaseConstants
    {
        public const string NzGeoGcs = "NZGD2000";
        public const string IndexSuffix = "SkyCombIndexTiff.xlsx";


        // Ground directory e.g. D:\SkyComb\Ground_Data
        public string GroundDirectory = "";
        public string IndexFileName { get { return GroundDirectory + "\\" + IndexSuffix; } }


        public TileModelList Tiles;


        // Normal constructor. Creates index if needed.
        public TileIndex(string groundDirectory, RectangleF targetArea, string theGeoGcs, bool yAxisPositive)
        {
            GroundDirectory = groundDirectory.TrimEnd('\\'); // Remove any trailing backslash
            Tiles = new();


            // If we can't find the index, create one.
            if (! IndexLoadSave.Exists(IndexFileName))
                CreateTileIndex();


            // Load the list of tiles from the datastore that intersects the target area
            IndexLoadSave indexStore = new(IndexFileName);
            indexStore.Load(Tiles, targetArea, theGeoGcs, yAxisPositive);
        }


        // Rebuild constructor. Rarely used. Updates index if needed. Slow.
        public TileIndex(string groundDirectory, bool yAxisPositive)
        {
            GroundDirectory = groundDirectory.TrimEnd('\\'); // Remove any trailing backslash
            Tiles = new();

            RebuildTileIndex(yAxisPositive);
        }


        // Create an index of all the tiles in the ground directory. Very slow. Say 20mins
        public void CreateTileIndex()
        {
            // Find all useful tiles so we can create a SkyCombIndexTiff.xlsx
            AddTiles();

            SaveTiles();
        }


        private void SaveTiles()
        { 
            // If we have tiles, create a tile index
            // If groundDirectory is not specified or is invalid then we may have not books.
            if (Tiles.Count > 0)
            {
                string fullfileName = IndexFileName; // GroundDirectory + "\\" + IndexSuffix;
                IndexLoadSave indexStore = new (fullfileName);
                indexStore.Save(Tiles);
            }

            Tiles.Clear();
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


        // By scanning all folders in groundDirectory, find and return all TIF files 
        public void AddTiles()
        {
            int fileProcessed = 0;

            try
            {
                if (GroundDirectory != "")
                {
                    // Read the names of the subdirectories
                    string[] subdirs = Directory.GetDirectories(GroundDirectory);
                    foreach (var subdir in subdirs)
                    {
                        // Read the names of the tiff files in the directory
                        string[] tiffFileNames = Directory.GetFiles(subdir, "*.tif");
                        foreach (var tiffFileName in tiffFileNames)
                        {
                            try
                            {
                                fileProcessed++;

                                var shortFileName = tiffFileName.Substring(subdir.Length + 1);
                                var shortSubdir = subdir.Substring(GroundDirectory.Length + 1);

                                AddTile(tiffFileName, shortSubdir, shortFileName);
                            }
                            catch
                            {
                                fileProcessed--;
                                // We ignore files we can't parse 
                            }
                        }
                    }
                }
            }
            catch
            {
                Tiles.Clear();
            }
        }


        // Usual case is that the user has obtained additional TIFFs and
        // wants to add them to the index. This is a slow process.
        // Also check for any duplicates in the index.
        private bool RebuildTileIndex(bool yAxisPositive)
        {
            if (GroundDirectory == "")
                return false;

            // If we can't find the index, create one using standard method.
            if (!IndexLoadSave.Exists(IndexFileName))
            {
                CreateTileIndex();
                return true;
            }


            // Load all the books from the datastore. Takes ~9 seconds.
            IndexLoadSave bookStore = new(IndexFileName);
            int lastDataStoreRow = bookStore.Load(Tiles, new(), "", yAxisPositive);


            // Check that index is good
            bool indexBad = false;
            foreach(var outerBook in Tiles)
            {
                foreach (var innerBook in Tiles)
                {
                    if( string.Compare(outerBook.Value.FileName, innerBook.Value.FileName) <= 0 )
                        continue;

                    if ((outerBook.Value.XllCorner == innerBook.Value.XllCorner) &&
                       (outerBook.Value.YllCorner == innerBook.Value.YllCorner) &&
                       (outerBook.Value.IsDem == innerBook.Value.IsDem))
                        indexBad = true;
                }
            }
            if(indexBad)
            {
                // Delete the existing bad index file
                File.Delete(IndexFileName);

                // Create a clean index
                CreateTileIndex();
                return true;
            }


            // Scan the folders and for each book found
            // If the book is not in the index, add it.
            try
            {
                // Read the names of the subdirectories
                string[] subdirs = Directory.GetDirectories(GroundDirectory);
                foreach (var subdir in subdirs)
                {
                    // Read the names of the tiff files in the directory
                    string[] tiffFileNames = Directory.GetFiles(subdir, "*.tif");
                    foreach (var tiffFileName in tiffFileNames)
                    {
                        try
                        {
                            var shortFileName = tiffFileName.Substring(subdir.Length + 1);
                            var shortSubdir = subdir.Substring(GroundDirectory.Length + 1);

                            if (Tiles.TryGetValue(shortFileName, out var orgBook))
                                continue;

                            AddTile(tiffFileName, shortSubdir, shortFileName);
                        }
                        catch
                        {
                            // We ignore files we can't parse 
                        }
                    }
                }

                SaveTiles();

                return true;
            }
            catch
            {
                Tiles.Clear();
                return false;
            }   
        }
    }
}
