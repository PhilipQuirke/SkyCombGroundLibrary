using BitMiracle.LibTiff.Classic;
using SkyCombGround.CommonSpace;


// Handles GeoTiff file with suffix ".tif"


namespace SkyCombGround.GroundSpace
{
    // Read-only books containing location and the ground area covered
    public class BookCatalogTiff : BookCatalog
    {
        public const string NzGeoGcs = "NZGD2000";


        public BookCatalogTiff(string groundDirectory) :
            base(groundDirectory, groundDirectory + "\\SkyCombIndexTiff.xlsx")
        {
        }


        // By scanning all folders in groundDirectory, find and return all TIF files 
        public override void AddBookNames()
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

                                var isDem =
                                    tiffFileName.ToLower().Contains("dem_") ||
                                    tiffFileName.ToLower().Contains("_dem");

                                var tiff = Tiff.Open(tiffFileName, "r");

                                int width = tiff.GetField(TiffTag.IMAGEWIDTH)[0].ToInt();
                                int height = tiff.GetField(TiffTag.IMAGELENGTH)[0].ToInt();

                                FieldValue[] modelTiePointTag = tiff.GetField(TiffTag.GEOTIFF_MODELTIEPOINTTAG);
                                byte[] modelTransformation = modelTiePointTag[1].GetBytes();
                                double originX = BitConverter.ToDouble(modelTransformation, 24); // e.g. 1924960
                                double originY = BitConverter.ToDouble(modelTransformation, 32); // e.g. 5803440

                                FieldValue[] geoScaleTag = tiff.GetField(TiffTag.GEOTIFF_GEOASCIIPARAMSTAG);
                                if ((geoScaleTag == null) || (geoScaleTag.Length < 2))
                                    continue;
                                var geoGcs = geoScaleTag[1].ToString();
                                if (!geoGcs.Contains(NzGeoGcs))
                                    continue;
                                geoGcs = NzGeoGcs;

                                BookNames.Add(new(shortSubdir, shortFileName, isDem, geoGcs, width, height, originX, originY, 1, 0));

                                tiff.Close();
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
                BookNames.Clear();
            }
        }
    }


    internal abstract class GroundDatumsTiff : GroundDatums
    {
        public GroundDatumsTiff(string groundDirectory, bool isDem, RelativeLocation minCountryLocnM, RelativeLocation maxCountryLocnM)
            : base(groundDirectory, isDem, minCountryLocnM, maxCountryLocnM)
        {
        }


        // Load datums from the book that are inside LocalArea
        protected override void GetDatums(BookName book)
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
    internal class GroundDatumsTiffNz : GroundDatumsTiff
    {
        public GroundDatumsTiffNz(string groundDirectory, bool isDem, RelativeLocation minCountryLocnM, RelativeLocation maxCountryLocnM)
            : base(groundDirectory, isDem, minCountryLocnM, maxCountryLocnM)
        {
            NztmProjection.AssertGood();
        }
    }


    // Calculate ground DEM and DSM using TIFF files on disk for New Zealand locations
    // Refer https://github.com/PhilipQuirke/SkyCombAnalystHelp/Ground.md for more detail.
    // #ExtendGroundSpace: For data sources from other countries, clone and modfiy this class's source ccode
    internal class GroundTiffNZ : BaseConstants
    {
        // Using TIFF files in subfolders of the groundDirectory folder,
        // return a list of unsorted DEM and DSM elevations inside the min/max location range.
        public static (GroundDatumsTiff? demDatums, GroundDatumsTiff? dsmDatums)
            CalcElevations(GroundData groundData, string groundDirectory)
        {
            try
            {
                groundDirectory = groundDirectory.Trim('\\');

                if (groundDirectory != "")
                {
                    (double minCountryNorthingM, double minCountryEastingM) =
                        NztmProjection.WgsToNztm(groundData.MinGlobalLocation.Latitude, groundData.MinGlobalLocation.Longitude);
                    (double maxCountryNorthingM, double maxCountryEastingM) =
                        NztmProjection.WgsToNztm(groundData.MaxGlobalLocation.Latitude, groundData.MaxGlobalLocation.Longitude);

                    var minCountryM = new RelativeLocation((float)minCountryNorthingM, (float)minCountryEastingM);
                    var maxCountryM = new RelativeLocation((float)maxCountryNorthingM, (float)maxCountryEastingM);

                    GroundDatumsTiff demDatums = new GroundDatumsTiffNz(groundDirectory, true, minCountryM, maxCountryM);
                    GroundDatumsTiff dsmDatums = new GroundDatumsTiffNz(groundDirectory, false, minCountryM, maxCountryM);

                    // Create (slow) or open (fast) an index of the ground DEM/DSM books found in groundDirectory
                    BookCatalogTiff catalog = new(groundDirectory);

                    // Find the books that can provide ground data for the drone flight area.
                    var neededBooks = catalog.OverlapsTargetArea(demDatums.TargetCountryAreaM(), BookCatalogTiff.NzGeoGcs, false);
                    if (neededBooks.Count > 0)
                    {
                        demDatums.GetDatumsInLocationCoordinates(neededBooks);
                        dsmDatums.GetDatumsInLocationCoordinates(neededBooks);

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
    }
}
