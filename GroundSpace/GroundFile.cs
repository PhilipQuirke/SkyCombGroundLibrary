using SkyCombGround.CommonSpace;
using SkyCombGround.PersistModel;
using System.Drawing;


// Read & index ground & surface elevation data from files on disk 
// Refer https://github.com/PhilipQuirke/SkyCombAnalystHelp/Ground.md for more background
namespace SkyCombGround.GroundSpace
{
    // Read-only book containing book location and the ground area it covers
    public class BookName : BaseConstants
    {
        // Folder name the file is in e.g. lds-auckland-north-lidar-1m-dem-2016-2018-AAIGrid
        public string FolderName { get; set; }
        // Short file name e.g. DEM_AY30_2016_1000_4949.xml or .tif
        public string FileName { get; set; }
        // Is file DEM data (else is DSM data).
        public bool IsDem { get; set; }
        // Coordinate system e.g. GCS_NZGD_2000
        public string GeoGcs { get; set; }


        // Refer https://en.wikipedia.org/wiki/Esri_grid for more detail. 
        // Number of columns of data in body of file
        public int NumCols { get; set; }
        // Number of rows of data in body of file
        public int NumRows { get; set; }
        // XllCorner and YllCorner are the western (left) x-coordinate and southern (bottom) y-coordinates, such as easting and northing
        public double XllCorner { get; set; }
        public double YllCorner { get; set; }
        // CellSize is the length of one side of a square cell
        public double CellSize { get; set; } = 1; // 1 metre
        // NoDataValue is the value that is regarded as "missing" or "not applicable" 
        public int NoDataValue { get; set; } = UnknownValue; // -999 is the most common "no value"


        public BookName(string folderName, string fileName, bool isDem, string geoGcs,
            int numCols, int numRows, double xllCorner, double yllCorner, double cellSize, int noDataValue)
        {
            FolderName = folderName;
            FileName = fileName;
            IsDem = isDem;
            GeoGcs = geoGcs.ToUpper();
            NumCols = numCols;
            NumRows = numRows;
            XllCorner = xllCorner;
            YllCorner = yllCorner;
            CellSize = cellSize;
            NoDataValue = noDataValue;

            AssertGood();
        }
        public BookName(List<string> settings)
        {
            FolderName = "";
            FileName = "";
            GeoGcs = "";

            LoadSettings(settings);
            AssertGood();
        }


        // Area covered by this book in country coordinate system
        public RectangleF GetCountryArea(bool yAxisPostive)
        {
            return new RectangleF(
                (float)XllCorner,
                (float)YllCorner + (yAxisPostive ? 0 : -NumRows),
                NumCols,
                NumRows);
        }


        public void AssertGood()
        {
            Assert(NumCols > 0, "BookName: NumCols Bad");
            Assert(NumRows > 0, "BookName: NumRows Bad");
            Assert(CellSize > 0, "BookName: CellSize Bad");
        }


        // Get the class's settings as datapairs (e.g. for saving to the datastore)
        public DataPairList GetSettings()
        {
            return new DataPairList
            {
                { "FolderName", FolderName },
                { "FileName", FileName },
                { "IsDem", IsDem },
                { "GeoGcs", GeoGcs },
                { "NumCols", NumCols },
                { "NumRows", NumRows },
                { "XllCorner", XllCorner, 0 },
                { "YllCorner", YllCorner, 0 },
                { "CellSize", CellSize, 0 },
                { "NoDataValue", NoDataValue },
            };
        }


        // Load this object's settings from strings (loaded from a datastore)
        // This function must align to the above GetSettings function.
        public void LoadSettings(List<string> settings)
        {
            int i = 0;
            FolderName = settings[i++];
            FileName = settings[i++];
            IsDem = ConfigBase.StringToBool(settings[i++]);
            GeoGcs = settings[i++].ToUpper();
            NumCols = ConfigBase.StringToInt(settings[i++]);
            NumRows = ConfigBase.StringToInt(settings[i++]);
            XllCorner = ConfigBase.StringToDouble(settings[i++]);
            YllCorner = ConfigBase.StringToDouble(settings[i++]);
            CellSize = ConfigBase.StringToDouble(settings[i++]);
            NoDataValue = ConfigBase.StringToInt(settings[i++]);
        }
    }


    // Read-only books containing location and the ground area covered
    public class BookNameList : List<BookName>
    {

    }


    // Persist the list of book names in a datastore SkyCombIndex.xlsx
    public class BookDataStore : BaseDataStore
    {
        public BookDataStore(string fullfileName) : base(fullfileName)
        {
        }


        // Does the data store exist on disk?
        public static bool Exists(string fullfileName)
        {
            return System.IO.File.Exists(fullfileName);
        }


        public void Save(BookNameList bookNames)
        {
            SelectOrAddWorksheet(FilesTabName);
            ClearWorksheet();

            int theRow = 0;
            foreach (var bookName in bookNames)
                SetDataListRowKeysAndValues(ref theRow, bookName.GetSettings());
        }


        public void Load(BookNameList bookNames)
        {
            int row = 2;
            try
            {
                if (SelectWorksheet(FilesTabName))
                {
                    var cell = Worksheet.Cells[row, 1];
                    while (cell != null && cell.Value != null && cell.Value.ToString() != "")
                    {
                        var directoryString = cell.Value.ToString();
                        if (directoryString == "")
                            break;

                        // Load the non-blank cells in this row into a BookName object
                        var book = new BookName(GetRowSettings(row, 1));
                        bookNames.Add(book);

                        row++;
                        cell = Worksheet.Cells[row, 1];
                    }
                }
            }
            catch (Exception ex)
            {
                throw BaseConstants.ThrowException("BookDataStore.Load: Row=" + row, ex);
            }
        }
    }


    // Build and save a list of the books in a datastore
    public abstract class BookCatalog : BaseConstants
    {
        public BookNameList BookNames = null;

        public BookDataStore BookStore = null;

        // Ground directory e.g. D:\SkyComb\Ground_Data
        public string GroundDirectory = "";


        // BookNames.AddBookNames(GroundDirectory);
        public abstract void AddBookNames();


        public BookCatalog(string groundDirectory, string fullfileName)
        {
            GroundDirectory = groundDirectory.TrimEnd('\\'); // Remove any trailing backslash

            BookNames = new();
            if (GroundDirectory != "")
            {
                if (BookDataStore.Exists(fullfileName))
                {
                    // Open & read the existing book index
                    BookStore = new(fullfileName);
                    BookStore.Open();
                    BookStore.Load(BookNames);
                    BookStore.Close();
                }
                else
                {
                    // Find all useful books
                    AddBookNames();

                    // If we have books, create a book index
                    // If groundDirectory is not specified or is invalid then we may have not books.
                    if (BookNames.Count > 0)
                    {
                        BookStore = new(fullfileName);
                        BookStore.Open();
                        BookStore.Save(BookNames);
                        BookStore.SaveAndClose();
                    }
                }
            }
        }


        // Return all BookNames that interest the specified target area, and use the specified GeoGcs
        public BookNameList OverlapsTargetArea(RectangleF targetArea, string theGeoGcs, bool yAxisPostive)
        {
            var answer = new BookNameList();

            foreach (var book in BookNames)
            {
                if (book.GeoGcs != theGeoGcs)
                    continue;

                RectangleF bookRect = book.GetCountryArea(yAxisPostive);

                bookRect.Intersect(targetArea);
                if ((bookRect.Width > 0) && (bookRect.Height > 0))
                    answer.Add(book);
            }

            return answer;
        }
    }


    public abstract class GroundDatums : GroundGrid
    {
        public string GroundDirectory = "";


        public GroundDatums(string groundDirectory, bool isDem, RelativeLocation minCountryLocnM, RelativeLocation maxCountryLocnM)
         : base(isDem, minCountryLocnM, maxCountryLocnM)
        {
            GroundDirectory = groundDirectory.TrimEnd('\\'); // Remove any trailing backslash
            ElevationAccuracyM = 0.2f;
        }


        // Load datums from the book that are inside CountryArea
        protected abstract void GetDatums(BookName book);


        public void GetDatumsInLocationCoordinates(BookNameList usefulBooks)
        {
            if (usefulBooks.Count > 0)
            {
                foreach (var book in usefulBooks)
                    if (book.IsDem == this.IsDem)
                        GetDatums(book);

                if (NumDatums > 0)
                    SetGapsToMinimum();
            }
        }
    }
}
