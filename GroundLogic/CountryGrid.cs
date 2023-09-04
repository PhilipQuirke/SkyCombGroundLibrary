// Copyright SkyComb Limited 2023. All rights reserved. 
using BitMiracle.LibTiff.Classic;
using SkyCombGround.CommonSpace;
using SkyCombGround.GroundModel;


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
                // If we have data in multiple encoding formats for this area, 
                // we only collect data for one encoding format.
                return;

            int northing = 0;
            int easting = 0;

            try
            {
                // Open the Tiff file
                var tiffFileName = GroundDirectory + "\\" +
                    book.FolderName + "\\" + book.FileName;
                if (File.Exists(tiffFileName))
                {
                    var tiff = Tiff.Open(tiffFileName, "r");

                    // Refer https://bitmiracle.github.io/libtiff.net/help/api/BitMiracle.LibTiff.Classic.TiffTag.html 
                    // for more detail on the following tags. Content includes:
                    // GEOTIFF_MODELPIXELSCALETAG
                    //      This tag is defining exact affine transformations between raster and model space.
                    //      Used in interchangeable GeoTIFF files.
                    // GEOTIFF_MODELTIEPOINTTAG
                    //      This tag stores raster->model tiepoint pairs. Used in interchangeable GeoTIFF files.
                    // GEOTIFF_MODELTRANSFORMATIONTAG
                    //      This tag is optionally provided for defining exact affine transformations between raster and model space.
                    //      Used in interchangeable GeoTIFF files.

                    int width = tiff.GetField(TiffTag.IMAGEWIDTH)[0].ToInt();
                    int height = tiff.GetField(TiffTag.IMAGELENGTH)[0].ToInt();

                    int samplesPerPixel = tiff.GetField(TiffTag.SAMPLESPERPIXEL)[0].ToInt();
                    int bitsPerSample = tiff.GetField(TiffTag.BITSPERSAMPLE)[0].ToInt();
                    int bytesPerSample = bitsPerSample / 8;

                    FieldValue[] modelTiePointTag = tiff.GetField(TiffTag.GEOTIFF_MODELTIEPOINTTAG);
                    byte[] modelTransformation = modelTiePointTag[1].GetBytes();
                    double originX = BitConverter.ToDouble(modelTransformation, 24); // e.g. 1924960
                    double originY = BitConverter.ToDouble(modelTransformation, 32); // e.g. 5803440

                    FieldValue[] modelPixelScaleTag = tiff.GetField(TiffTag.GEOTIFF_MODELPIXELSCALETAG);
                    byte[] modelPixelScale = modelPixelScaleTag[1].GetBytes();
                    double pixelSizeX = BitConverter.ToDouble(modelPixelScale, 0);
                    double pixelSizeY = BitConverter.ToDouble(modelPixelScale, 8);

                    int tileWidth = tiff.GetField(TiffTag.TILEWIDTH)[0].ToInt();
                    int tileHeight = tiff.GetField(TiffTag.TILELENGTH)[0].ToInt();
                    var tileSize = tiff.TileSize();
                    var numTiles = tiff.NumberOfTiles();
                    int tilesPerRow = (width + tileWidth - 1) / tileWidth; // Number of tiles in each row
                    var tileRoom = tileSize * numTiles;

                    var bufferSize = Math.Max(width * height * sizeof(float), tileRoom);
                    var buffer = new byte[bufferSize];
                    var offset = 0;


                    // Read the full Tiff into memory. NZ Tiffs are ~1Mb. 
                    for (var tileIndex = 0; tileIndex < numTiles; tileIndex++)
                    {
                        tiff.ReadEncodedTile(tileIndex, buffer, offset, -1);
                        offset += tileSize;
                    }


                    // Traverse Tiff and for area overlapping CountryTargetAreaBuffered call AddDatum
                    double startY = originY + (pixelSizeY / 2.0);
                    double startX = originX + (pixelSizeX / 2.0);

                    for (northing = 0; northing < height; northing++)
                    {
                        double yLocation = startY - northing * pixelSizeY;
                        if ((yLocation <= MaxCountryNorthingM) &&
                            (yLocation >= MinCountryNorthingM))
                        {
                            for (easting = 0; easting < width; easting++)
                            {
                                double xLocation = startX + easting * pixelSizeX;
                                if ((xLocation >= MinCountryEastingM) &&
                                    (xLocation <= MaxCountryEastingM))
                                {
                                    int tileIndexX = easting / tileWidth;
                                    int tileIndexY = northing / tileHeight;
                                    int tileIndex = tileIndexY * tilesPerRow + tileIndexX;

                                    int tileOffsetX = easting % tileWidth;
                                    int tileOffsetY = northing % tileHeight;
                                    int tileOffset = (tileOffsetY * tileWidth + tileOffsetX) * sizeof(float);

                                    float elevationM = BitConverter.ToSingle(buffer, tileOffset + tileIndex * tileSize);

                                    AddCountryDatum(new RelativeLocation((float)yLocation, (float)xLocation), elevationM);
                                }
                            }
                        }
                    }

                    tiff.Close();
                }
            }
            catch (Exception ex)
            {
                throw ThrowException("GroundDatumsTiff.GetDatums: northing=" + northing + " easting=" + easting, ex);
            }
        }
    }


    // Derived class specific to New Zealand and the GCS_NZGD_2000 data format.
    // #ExtendGroundSpace: For data sources from other countries, clone and modfiy this class's source ccode
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
    // #ExtendGroundSpace: For data sources from other countries, clone and modfiy this class's source ccode
    public class GroundTiffNZ : BaseConstants
    {
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
                    var dsmDatums = new NzGrid(groundDirectory, false, minCountryM, maxCountryM);

                    // Create (slow) or open (fast) an index of the ground DEM/DSM books found in groundDirectory
                    TileIndex groundIndex = new(groundDirectory, demDatums.TargetCountryAreaM(), TileIndex.NzGeoGcs, false); 

                    // Find the books that can provide ground data for the drone flight area.
                    if (groundIndex.Tiles.Count > 0)
                    {
                        demDatums.GetDatumsInLocationCoordinates(groundIndex.Tiles);
                        dsmDatums.GetDatumsInLocationCoordinates(groundIndex.Tiles);

                        if ((demDatums.NumDatums == 0) || (demDatums.NumElevationsStored == 0))
                            demDatums = null;

                        if ((dsmDatums.NumDatums == 0) || (dsmDatums.NumElevationsStored == 0))
                            dsmDatums = null;
                    }

                    return (demDatums, dsmDatums);
                }
            }
            catch
            {
            }

            return (null, null);
        }


        public static void RebuildIndex(string groundDirectory)
        {
            groundDirectory = groundDirectory.Trim('\\');
            TileIndex index = new(groundDirectory,false);
        }

    }
}
