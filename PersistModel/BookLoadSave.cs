using SkyCombGround.GroundModel;


// Read & index ground & surface elevation data from files on disk 
// Refer https://github.com/PhilipQuirke/SkyCombAnalystHelp/Ground.md for more background
namespace SkyCombGround.PersistModel
{
    // Persist the list of book names in a datastore SkyCombIndex.xlsx
    public class BookLoadSave : BaseDataStore
    {
        public BookLoadSave(string fullfileName) : base(fullfileName)
        {
        }


        // Does the data store exist on disk?
        public static bool Exists(string fullfileName)
        {
            return System.IO.File.Exists(fullfileName);
        }


        public void Save(BookModelList bookNames)
        {
            Open();

            SelectOrAddWorksheet(FilesTabName);
            ClearWorksheet();

            int theRow = 0;
            foreach (var bookName in bookNames)
                SetDataListRowKeysAndValues(ref theRow, bookName.GetSettings());

            SaveAndClose();
        }


        public void Load(BookModelList bookNames)
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
                        var book = new BookModel(GetRowSettings(row, 1));
                        bookNames.Add(book);

                        row++;
                        cell = Worksheet.Cells[row, 1];
                    }
                }

                Close();
            }
            catch (Exception ex)
            {
                throw ThrowException("BookDataStore.Load: Row=" + row, ex);
            }
        }
    }
}
