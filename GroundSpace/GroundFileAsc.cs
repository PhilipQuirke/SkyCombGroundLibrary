using SkyCombGround.CommonSpace;
using SkyCombGround.PersistModel;
using System.Drawing;


// Handles ASC text files
// Refer https://en.wikipedia.org/wiki/Esri_grid for more detail on ASC file structure.
// Importantly the origin correponds to the FIRST value on the LAST row of data.
// So as we read line by line down the text file, the yLocation decreases towards zero.


namespace SkyCombGround.GroundSpace
{
    // Read-only books containing location and the ground area covered
    public class BookCatalogAsc : BookCatalog
    {
        public const string NzGeoGcs = "GCS_NZGD_2000";

        public BookCatalogAsc(string groundDirectory) :
            base(groundDirectory, groundDirectory + "\\SkyCombIndexAsc.xlsx")
        {
        }


        // By scanning all folders in groundDirectory, find and return all PRJ files 
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
                        // Read the names of the projection (prj) files in the directory, containing a GEOGCS value
                        string[] prjFileNames = Directory.GetFiles(subdir, "*.prj");
                        foreach (var prjFileName in prjFileNames)
                        {
                            try
                            {
                                fileProcessed++;

                                var shortFileName = prjFileName.Substring(subdir.Length + 1);
                                var shortSubdir = subdir.Substring(GroundDirectory.Length + 1);

                                var isDem =
                                    prjFileName.ToLower().Contains("dem_") ||
                                    prjFileName.ToLower().Contains("_dem");


                                System.IO.StreamReader prjFile = new(prjFileName);

                                // Load the GEOGCS value
                                string line = prjFile.ReadLine().ToUpper();
                                int pos = line.IndexOf("GEOGCS[");
                                if (pos < 0)
                                    continue;
                                line = line.Substring(pos + 8);
                                pos = line.IndexOf("\"");
                                if (pos < 0)
                                    continue;
                                string geoGcs = line.Substring(0, pos);

                                prjFile.Close();


                                // Open the associated ASC to load the xllcorner & yllcorner values.
                                var ascFileName = BaseDataStore.SwapFileNameExtension(prjFileName, ".asc");

                                if (System.IO.File.Exists(ascFileName))
                                {
                                    System.IO.StreamReader ascFile = new(ascFileName);

                                    // ASC file starts with:
                                    // ncols        480
                                    // nrows        720
                                    // xllcorner    1730080.000000000000
                                    // yllcorner    5987040.000000000000
                                    // cellsize     1.000000000000
                                    // NODATA_value -999.000000

                                    line = ascFile.ReadLine();
                                    int numCols = int.Parse(line.Substring(12).Trim());

                                    line = ascFile.ReadLine();
                                    int numRows = int.Parse(line.Substring(12).Trim());

                                    line = ascFile.ReadLine();
                                    double xllCorner = double.Parse(line.Substring(12).Trim());

                                    line = ascFile.ReadLine();
                                    double yllCorner = double.Parse(line.Substring(12).Trim());

                                    line = ascFile.ReadLine();
                                    double cellSize = double.Parse(line.Substring(12).Trim());

                                    line = ascFile.ReadLine();
                                    int noDataValue = BaseConstants.UnknownValue;
                                    if (line.Substring(0, 6).ToLower() == "NoData") // Sometimes this line is missing.
                                        noDataValue = (int)double.Parse(line.Substring(12).Trim());

                                    if ((numCols > 0) && (numRows > 0))
                                        BookNames.Add(new(shortSubdir, shortFileName, isDem, geoGcs, numCols, numRows, xllCorner, yllCorner, cellSize, noDataValue));

                                    ascFile.Close();
                                }
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

    internal abstract class GroundDatumsAsc : GroundDatums
    {
        public GroundDatumsAsc(string groundDirectory, bool isDem, RelativeLocation minLocationM, RelativeLocation maxCountryLocnM) 
            : base(groundDirectory, isDem, minLocationM, maxCountryLocnM) 
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
                // Open the associated ASC to load the xllcorner & yllcorner values.
                var fullAscFileName = GroundDirectory + "\\" +
                    book.FolderName + "\\" + BaseDataStore.SwapFileNameExtension(book.FileName, ".asc");
                if (File.Exists(fullAscFileName))
                {
                    StreamReader ascFile = new(fullAscFileName);

                    // ASC file starts with:
                    // ncols        480
                    // nrows        720
                    // xllcorner    1730080.000000000000
                    // yllcorner    5987040.000000000000
                    // cellsize     1.000000000000
                    // NODATA_value -999.000000
                    ascFile.ReadLine();
                    ascFile.ReadLine();
                    ascFile.ReadLine();
                    ascFile.ReadLine();
                    ascFile.ReadLine();
                    ascFile.ReadLine();

                    // Refer https://en.wikipedia.org/wiki/Esri_grid for more detail.
                    // Importantly origin is the FIRST value on the LAST row of data.
                    // As we read down the file, the yLocation decreases.
                    for (northing = book.NumRows; northing > 0; northing--)
                    {
                        string line = ascFile.ReadLine();
                        if (line == "")
                            continue;

                        double yLocation = book.YllCorner + northing * book.CellSize;
                        if (yLocation <= MaxCountryNorthingM)
                        {
                            if (yLocation >= MinCountryNorthingM)
                            {
                                easting = 0;
                                var lastSpacePos = 0;
                                var spacePos = line.IndexOf(' ', lastSpacePos + 1);
                                while (spacePos > 0)
                                {
                                    // As we read across the line, the xLocation increases.
                                    double xLocation = book.XllCorner + easting * book.CellSize;
                                    if (xLocation >= MinCountryEastingM)
                                    {
                                        if (xLocation <= MaxCountryEastingM)
                                        {
                                            var elevationM = float.Parse(line.Substring(lastSpacePos, spacePos - lastSpacePos));

                                            AddCountryDatum(new RelativeLocation((float)yLocation, (float)xLocation), elevationM);
                                        }
                                        else
                                            // Have traversed right past desired range.
                                            break;
                                    }

                                    easting++;
                                    lastSpacePos = spacePos;
                                    spacePos = line.IndexOf(' ', lastSpacePos + 1);
                                }
                            }
                            else
                                // Have traversed down past desired range.
                                break;
                        }
                    }

                    ascFile.Close();
                }
            }
            catch (Exception ex)
            {
                throw ThrowException("GroundDatumsAsc.GetDatums: Row=" + northing + " Col=" + easting, ex);
            }
        }
    }


    // Derived class specific to New Zealand and the GCS_NZGD_2000 data format.
    // #ExtendGroundSpace: For data sources from other countries, clone and modfiy this class's source ccode
    internal class GroundDatumsAscNz : GroundDatumsAsc
    {
        public GroundDatumsAscNz(string groundDirectory, bool isDem, RelativeLocation minCountryLocnM, RelativeLocation maxCountryLocnM)
            : base(groundDirectory, isDem, minCountryLocnM, maxCountryLocnM)
        {
            NztmProjection.AssertGood();
        }
    }


    // Calculate ground DEM and DSM using ASC files on disk for New Zealand locations
    // Refer https://github.com/PhilipQuirke/SkyCombAnalystHelp/Ground.md for more detail.
    // #ExtendGroundSpace: For data sources from other countries, clone and modfiy this class's source ccode
    internal class GroundAscNZ : BaseConstants
    {
        // Using PRJ and ASC files in subfolders of the groundDirectory folder,
        // return a list of unsorted DEM and DSM elevations inside the min/max location range.
        public static (GroundDatumsAsc? demDatums, GroundDatumsAsc? dsmDatums)
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

                    GroundDatumsAsc? demDatums = new GroundDatumsAscNz(groundDirectory, true, minCountryM, maxCountryM);
                    GroundDatumsAsc? dsmDatums = new GroundDatumsAscNz(groundDirectory, false, minCountryM, maxCountryM);

                    // Create (slow) or open (fast) an index of the ground DEM/DSM books found in groundDirectory
                    BookCatalogAsc catalog = new(groundDirectory);

                    // Find the books that can provide ground data for the drone flight area.
                    var neededBooks = catalog.OverlapsTargetArea(demDatums.TargetCountryAreaM(), BookCatalogAsc.NzGeoGcs, true);
                    if (neededBooks.Count > 0)
                    {
                        demDatums.GetDatumsInLocationCoordinates(neededBooks);
                        dsmDatums.GetDatumsInLocationCoordinates(neededBooks);

                        if((demDatums.NumDatums == 0) || (demDatums.NumElevationsStored == 0))
                            demDatums = null;

                        if((dsmDatums.NumDatums == 0) || (dsmDatums.NumElevationsStored == 0))
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