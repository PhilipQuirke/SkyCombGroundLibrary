using SkyCombGround.CommonSpace;
using SkyCombGround.GroundModel;
using SkyCombGround.PersistModel;
using System.Drawing;


// Read & index ground & surface elevation data from files on disk 
// Refer https://github.com/PhilipQuirke/SkyCombAnalystHelp/Ground.md for more background
namespace SkyCombGround.GroundLogic
{
    // Build and save a list of the books in a datastore
    public abstract class BookCatalog : BaseConstants
    {
        public BookModelList? BookNames = null;

        public BookLoadSave? BookStore = null;

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
                if (BookLoadSave.Exists(fullfileName))
                {
                    // Open & read the existing book index
                    BookStore = new(fullfileName);
                    BookStore.Load(BookNames);
                }
                else
                {
                    // Find all useful books so we can create a SkyCombIndexTiff.xlsx
                    AddBookNames();

                    // If we have books, create a book index
                    // If groundDirectory is not specified or is invalid then we may have not books.
                    if (BookNames.Count > 0)
                    {
                        BookStore = new(fullfileName);
                        BookStore.Save(BookNames);
                    }
                }
            }
        }


        // Return all BookNames that interest the specified target area, and use the specified GeoGcs
        public BookModelList OverlapsTargetArea(RectangleF targetArea, string theGeoGcs, bool yAxisPostive)
        {
            var answer = new BookModelList();

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
        protected abstract void GetDatums(BookModel book);


        public void GetDatumsInLocationCoordinates(BookModelList usefulBooks)
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
