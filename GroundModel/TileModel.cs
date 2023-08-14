using SkyCombGround.CommonSpace;
using System.Drawing;


// Read & index ground & surface elevation data from files on disk 
// Refer https://github.com/PhilipQuirke/SkyCombAnalystHelp/Ground.md for more background
namespace SkyCombGround.GroundModel
{
    // Read-only TIFF containing physical location and ground coverage area
    public class TileModel : BaseConstants
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


        public TileModel(string folderName, string fileName, bool isDem, string geoGcs,
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
        public TileModel(List<string> settings)
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
            Assert(NumCols > 0, "TileModel: NumCols Bad");
            Assert(NumRows > 0, "TileModel: NumRows Bad");
            Assert(CellSize > 0, "TileModel: CellSize Bad");
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


    // Read-only tiles containing location and the ground area covered
    public class TileModelList : SortedList<string,TileModel>
    {
        public void Add( TileModel model)
        {             
            Add(model.FileName, model);
        }
    }
}
