using SkyCombGround.GroundModel;
using System.Drawing;


// Read & index ground & surface elevation data from files on disk 
// Refer https://github.com/PhilipQuirke/SkyCombAnalystHelp/Ground.md for more background
namespace SkyCombGround.PersistModel
{
    // Persist the list of book names in a datastore SkyCombIndex.xlsx
    public class IndexLoadSave : BaseDataStore
    {
        public IndexLoadSave(string fullfileName) : base(fullfileName)
        {
        }


        // Does the data store exist on disk?
        public static bool Exists(string fullfileName)
        {
            return System.IO.File.Exists(fullfileName);
        }


        public void Save(TileModelList bookNames)
        {
            Open();

            SelectOrAddWorksheet(FilesTabName);
            ClearWorksheet();

            int theRow = 0;
            foreach (var bookName in bookNames)
                SetDataListRowKeysAndValues(ref theRow, bookName.Value.GetSettings());

            SaveAndFreeResources();
        }


        // Count the number of books in the datastore
        public int Count()
        {
            int answer = 0;
            try
            {
                Open();

                if (SelectWorksheet(FilesTabName))
                    if ((Worksheet != null) && (Worksheet.Dimension != null) && (Worksheet.Dimension.End != null))
                        answer = Worksheet.Dimension.End.Row - 1;

                FreeResources();
            }
            catch (Exception ex)
            {
                throw ThrowException("IndexLoadSave.Count:", ex);
            }

            return answer;
        }


        // Load the list of books from the datastore that intersects the target area
        public void Load(TileModelList bookNames, RectangleF targetArea, string theGeoGcs, bool yAxisPositive)
        {
            int row = 2;
            try
            {
                Open();

                if (SelectWorksheet(FilesTabName))
                {
                    var cell = Worksheet.Cells[row, 1];
                    while (cell != null && cell.Value != null && cell.Value.ToString() != "")
                    {
                        var directoryString = cell.Value.ToString();
                        if (directoryString == "")
                            break;

                        // Load the non-blank cells in this row into a BookName object
                        var book = new TileModel(GetRowSettings(row, 1));

                        // If we have a GeoGcs check it matches
                        if((theGeoGcs != "") && (book.GeoGcs != theGeoGcs))
                            continue;

                        RectangleF bookRect = book.GetCountryArea(yAxisPositive);

                        // If we have a target area check it intersects
                        if (!targetArea.IsEmpty)
                            bookRect.Intersect(targetArea);

                        if ((bookRect.Width > 0) && (bookRect.Height > 0))
                            bookNames.Add(book);

                        row++;
                        cell = Worksheet.Cells[row, 1];
                    }
                }

                FreeResources();
            }
            catch (Exception ex)
            {
                throw ThrowException("IndexLoadSave.Load: Row=" + row, ex);
            }
        }
    }
}
