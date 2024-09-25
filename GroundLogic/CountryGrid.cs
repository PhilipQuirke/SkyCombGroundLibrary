// Copyright SkyComb Limited 2024. All rights reserved. 
using BitMiracle.LibTiff.Classic;
using SkyCombGround.CommonSpace;
using SkyCombGround.GroundModel;
using System.Drawing;


// Handles GeoTiff file with suffix ".tif"
namespace SkyCombGround.GroundLogic
{

    public class CountryGrid : GroundModel.GroundModel
    {
        public string GroundDirectory = "";


        public CountryGrid(string groundDirectory, bool isDem, RelativeLocation minCountryLocnM, RelativeLocation maxCountryLocnM)
         : base(isDem, minCountryLocnM, maxCountryLocnM)
        {
            GroundDirectory = groundDirectory.TrimEnd('\\'); // Remove any trailing backslash
            ElevationAccuracyM = 0.2f;
        }


        public void GetDatumsInLocationCoordinates(TileModelList usefulBooks)
        {
            if (usefulBooks.Count > 0)
            {
                foreach (var book in usefulBooks)
                    if (book.Value.IsDem == this.IsDem)
                        GetDatums(book.Value);

                if (NumDatums > 0)
                    SetGapsToMinimum();
            }
        }


        // Load datums from the book that are inside LocalArea
        protected void GetDatums(TileModel book)
        {
            if (Source == "")
                Source = book.GeoGcs;
            else if (Source != book.GeoGcs)
                return;

            try
            {
                var tiffFileName = Path.Combine(GroundDirectory, book.FolderName, book.FileName);
                if (!File.Exists(tiffFileName))
                    return;

                using (var tiff = Tiff.Open(tiffFileName, "r"))
                {
                    int width = tiff.GetField(TiffTag.IMAGEWIDTH)[0].ToInt();
                    int height = tiff.GetField(TiffTag.IMAGELENGTH)[0].ToInt();

                    var modelTiePointTag = tiff.GetField(TiffTag.GEOTIFF_MODELTIEPOINTTAG);
                    var modelTransformation = modelTiePointTag[1].GetBytes();
                    double originX = BitConverter.ToDouble(modelTransformation, 24);
                    double originY = BitConverter.ToDouble(modelTransformation, 32);

                    var modelPixelScaleTag = tiff.GetField(TiffTag.GEOTIFF_MODELPIXELSCALETAG);
                    var modelPixelScale = modelPixelScaleTag[1].GetBytes();
                    double pixelSizeX = BitConverter.ToDouble(modelPixelScale, 0);
                    double pixelSizeY = BitConverter.ToDouble(modelPixelScale, 8);

                    int tileWidth = tiff.GetField(TiffTag.TILEWIDTH)[0].ToInt();
                    int tileHeight = tiff.GetField(TiffTag.TILELENGTH)[0].ToInt();
                    var tileSize = tiff.TileSize();
                    var numTiles = tiff.NumberOfTiles();

                    var buffer = new byte[tileSize];
                    var elevations = new float[tileWidth * tileHeight];

                    double startY = originY + (pixelSizeY / 2.0);
                    double startX = originX + (pixelSizeX / 2.0);

                    for (int tileY = 0; tileY < height; tileY += tileHeight)
                    {
                        for (int tileX = 0; tileX < width; tileX += tileWidth)
                        {
                            int tileIndex = tiff.ComputeTile(tileX, tileY, 0, 0);
                            tiff.ReadTile(buffer, 0, tileX, tileY, 0, 0);
                            Buffer.BlockCopy(buffer, 0, elevations, 0, buffer.Length);

                            for (int y = 0; y < tileHeight && tileY + y < height; y++)
                            {
                                double yLocation = startY - (tileY + y) * pixelSizeY;
                                if (yLocation > MaxCountryNorthingM || yLocation < MinCountryNorthingM)
                                    continue;

                                for (int x = 0; x < tileWidth && tileX + x < width; x++)
                                {
                                    double xLocation = startX + (tileX + x) * pixelSizeX;
                                    if (xLocation < MinCountryEastingM || xLocation > MaxCountryEastingM)
                                        continue;

                                    float elevationM = elevations[y * tileWidth + x];
                                    AddCountryDatum(new RelativeLocation((float)yLocation, (float)xLocation), elevationM);
                                }
                            }
                        }
                    }
                }

                if (NumDatums > 0)
                    SetGapsToMinimum();
            }
            catch (Exception ex)
            {
                throw ThrowException("CountryGrid.GetDatums", ex);
            }
        }
    }


    // Derived class specific to New Zealand and the GCS_NZGD_2000 data format.
    // #ExtendGroundSpace: For data sources from other countries, clone and modify this class's source code
    internal class NzGrid : CountryGrid
    {
        public NzGrid(string groundDirectory, bool isDem, RelativeLocation minCountryLocnM, RelativeLocation maxCountryLocnM)
            : base(groundDirectory, isDem, minCountryLocnM, maxCountryLocnM)
        {
            NztmProjection.AssertGood();
        }
    }


    // Calculate ground DEM and DSM using TIFF files on disk for New Zealand locations
    // Refer https://github.com/PhilipQuirke/SkyCombAnalystHelp/Ground.md for more detail.
    // #ExtendGroundSpace: For data sources from other countries, clone and modify this class's source code
    public class GroundTiffNZ : BaseConstants
    {

        // PQR: Assumes there is only one applicable (DEM or DSM) index. In rare cases, there may be 2 applicable indexes
        // PQR: Assumes that each index has either DEM or DSM data but not both. This is a reasonable assumption.
        public static TileIndex FindExistingIndex(string groundSubDirectory, RectangleF targetCountryAreaM, bool isDem)
        {
            // Open an index of the ground DEM/DSM books found in groundDirectory (or subdirectory)
            TileIndex groundIndex = new(groundSubDirectory, targetCountryAreaM, TileIndex.NzGeoGcs, false, isDem);
            if ((groundIndex.Tiles.Count > 0) && (groundIndex.Tiles.Values[0].IsDem == isDem))
                return groundIndex;


            // Recursively handle subfolders
            string[] subfolders = Directory.GetDirectories(groundSubDirectory);
            foreach (string subfolder in subfolders)
            {
                groundIndex = FindExistingIndex(subfolder, targetCountryAreaM, isDem);
                if (groundIndex != null)
                    return groundIndex;
            }

            return null;
        }


        // Using TIFF files in subfolders of the groundDirectory folder,
        // return a list of unsorted DEM and DSM elevations inside the min/max location range.
        public static (CountryGrid? demDatums, CountryGrid? dsmDatums)
            CalcElevations(GroundData groundData, string groundDirectory)
        {
            try
            {
                groundDirectory = groundDirectory.Trim('\\');

                if (groundDirectory != "")
                {
                    var minCountryM = NztmProjection.WgsToNztm(groundData.MinGlobalLocation);
                    var maxCountryM = NztmProjection.WgsToNztm(groundData.MaxGlobalLocation);

                    var demDatums = new NzGrid(groundDirectory, true, minCountryM, maxCountryM);
                    // Open an index of the ground DEM books found in groundDirectory (or subdirectory)
                    TileIndex groundIndex = FindExistingIndex(groundDirectory, demDatums.TargetCountryAreaM(), true);
                    // Find the books that can provide ground data for the drone flight area.
                    if ((groundIndex != null) && (groundIndex.Tiles.Count > 0))
                    {
                        demDatums.GetDatumsInLocationCoordinates(groundIndex.Tiles);
                        if ((demDatums.NumDatums == 0) || (demDatums.NumElevationsStored == 0))
                            demDatums = null;
                    }

                    var dsmDatums = new NzGrid(groundDirectory, false, minCountryM, maxCountryM);
                    // Open an index of the ground DSM books found in groundDirectory (or subdirectory)
                    groundIndex = FindExistingIndex(groundDirectory, demDatums.TargetCountryAreaM(), false);
                    // Find the books that can provide ground data for the drone flight area.
                    if ((groundIndex != null) && (groundIndex.Tiles.Count > 0))
                    {
                        dsmDatums.GetDatumsInLocationCoordinates(groundIndex.Tiles);
                        if ((dsmDatums.NumDatums == 0) || (dsmDatums.NumElevationsStored == 0))
                            dsmDatums = null;
                    }

                    return (demDatums, dsmDatums);
                }
            }
            catch (Exception ex)
            {
                throw ThrowException("GroundTiffNZ.CalcElevations", ex);
            }

            return (null, null);
        }


        // Scan groundDirectory folder and sub-folders, building TIF indexes.
        // For each folder or sub-folder containing TIFs, build a new index (spreadheet)
        public static void RebuildIndexes(string folderPath)
        {
            // Maybe create an index for the current folder
            new TileIndex(folderPath);

            // Recursively handle subfolders
            string[] subfolders = Directory.GetDirectories(folderPath);
            foreach (string subfolder in subfolders)
                RebuildIndexes(subfolder);
        }

    }
}
