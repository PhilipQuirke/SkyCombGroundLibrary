// Copyright SkyComb Limited 2023. All rights reserved.
using OfficeOpenXml;
using OfficeOpenXml.Drawing.Chart;
using SkyCombGround.CommonSpace;
using System.Drawing;


namespace SkyCombGround.PersistModel
{
    public class BaseDataStore : ConfigBase
    {
        // If we can't save a datastore, it is possible the file is open in Excel.
        // Ask the user to close it. If they do, return true so we can retry.
        public delegate bool CantAccessDataStore_InformUser_ReturnRetry();

        public static CantAccessDataStore_InformUser_ReturnRetry? CantAccessDataStore_InformUser_ReturnRetry_delegate = null;


        // The name of the spreadsheet created as the DataStore
        public string DataStoreFileName { get; set; } = "";
        // A spreadsheet is used as the physical instantiation of the DataStore
        public ExcelPackage? Store { get; set; } = null;
        public ExcelWorksheet? Worksheet { get; set; } = null;


        // Open an existing DataStore 
        public BaseDataStore(ExcelPackage store, string fileName)
        {
            DataStoreFileName = fileName;
            Store = store;
            Worksheet = null;
        }


        // Create a DataStore on disk & store the Files settings.
        public BaseDataStore(string fileName)
        {
            ExcelPackage.LicenseContext = LicenseContext.NonCommercial;

            DataStoreFileName = fileName;
            Store = new();
            Worksheet = null;
        }


        public bool IsOpen => ((Store != null) && (Store.File != null));


        // If necessary, open the existing datastore 
        public virtual void Open()
        {
            if (IsOpen)
                return;

            Store = new(DataStoreFileName);
        }


        // Ensure we are not holding a file handle (or similar resources) open
        public void Close()
        {
            if (IsOpen)
                Store.Dispose();
            Store = null;
            Worksheet = null;
        }


        // Save the existing datastore to disk.
        public void Save()
        {
            try
            {
                Store.Save();
            }
            catch (Exception ex)
            {
                if (ex.ToString().Contains("Error saving file") &&
                    (CantAccessDataStore_InformUser_ReturnRetry_delegate != null))
                {
                    // If we can't save a datastore, it is possible the file is open in Excel.
                    // Ask the user to close it. If they do, retry the save.
                    if (CantAccessDataStore_InformUser_ReturnRetry_delegate())
                        Store.Save();
                    else
                        throw;
                }
                else
                    throw;
            }
        }


        // Save (and close) the existing datastore to disk.
        public void SaveAndClose()
        {
            Save();
            Close();
        }


        // Add a new worksheet
        public void AddWorksheet(string worksheetName)
        {
            Worksheet = Store.Workbook.Worksheets.Add(worksheetName);
        }


        // Return a pointer to an existing worksheet by name
        public ExcelWorksheet? ReferWorksheet(string worksheetName)
        {
            if (Store == null)
                return null;

            return Store.Workbook.Worksheets[worksheetName];
        }


        // Select the existing worksheet by name
        public bool SelectWorksheet(string worksheetName)
        {
            Worksheet = ReferWorksheet(worksheetName);
            return Worksheet != null;
        }


        // Ensure the named worksheet exists by either selecting or adding it. Returns true if it added a new tab.
        public (bool, ExcelWorksheet?) SelectOrAddWorksheet(string worksheetName)
        {
            var newTab = !SelectWorksheet(worksheetName);
            if (newTab)
                AddWorksheet(worksheetName);
            return (newTab, Worksheet);
        }


        // Clear any existing data in the Worksheet
        public void ClearWorksheet()
        {
            if ((Worksheet != null) && (Worksheet.Dimension != null) && (Worksheet.Dimension.End != null))
                Worksheet.Cells[1, 1, Worksheet.Dimension.End.Row, Worksheet.Dimension.End.Column].Clear();
        }


        // Hide an existing Worksheet
        public void HideWorksheet(string worksheetName, bool hide = true)
        {
            var worksheet = ReferWorksheet(worksheetName);
            if (worksheet != null)
                worksheet.Hidden = (hide ? eWorkSheetHidden.Hidden : eWorkSheetHidden.Visible);
        }


        public void TrimLastEmptyRows()
        {
            while (IsLastRowEmpty())
                Worksheet.DeleteRow(Worksheet.Dimension.End.Row);
        }


        public bool IsLastRowEmpty()
        {
            var empties = new List<bool>();

            for (int i = 1; i <= Worksheet.Dimension.End.Column; i++)
            {
                var rowEmpty = Worksheet.Cells[Worksheet.Dimension.End.Row, i].Value == null ? true : false;
                empties.Add(rowEmpty);
            }

            return empties.All(e => e);
        }


        public void SetColumnWidth(int column, int width = 50)
        {
            Worksheet.Column(column).Width = width;
        }


        // Autofit columns for all cells. Slow procedure.
        public void AutoFitColumns(int minWidth = 100)
        {
            if ((Worksheet != null) && (Worksheet.Cells != null))
                Worksheet.Cells.AutoFitColumns(minWidth);
        }
        // For lists we want a smaller column width for compactness. Slow procedure.
        public void AutoFitListColumns()
        {
            AutoFitColumns(40);
        }


        public void SetColumnColor(int column /* one-based */, int maxRow, Color color)
        {
            if (maxRow > 1)
                Worksheet.Cells[1, column, maxRow, column].Style.Font.Color.SetColor(color);
        }


        public void SetInternalHyperLink(ExcelRange cells, string destinationTabName)
        {
            cells.Hyperlink = new Uri("#'" + destinationTabName + "'!A1", UriKind.Relative);
            cells.Style.Font.UnderLine = true;
            cells.Style.Font.Color.SetColor(Color.Blue);
        }


        public void SetExternalHyperLink(ExcelRange cells, string destinationURL)
        {
            cells.Hyperlink = new Uri(destinationURL, UriKind.Absolute);
            cells.Style.Font.UnderLine = true;
            cells.Style.Font.Color.SetColor(Color.Blue);
        }


        // Convert a one-based index to a column letter
        public static char ColumnIndexToChar(int index)
        {
            return (char)('A' - 1 + index);
        }


        public void SetDataListRowKeys(int row, int startCol, DataPairList list)
        {
            if (list == null)
                return;

            int col = startCol;
            foreach (var pair in list)
            {
                var cell = Worksheet.Cells[row, col++];
                cell.Value = pair.Key;
            }

            BoldFreezeAndAutoFilterRow1(col - 1);
        }


        public void SetDataListRowValues(int row, int startCol, DataPairList list)
        {
            if (list == null)
                return;

            int col = startCol;
            foreach (var pair in list)
                SetDataPairValue(row, col++, pair);
        }


        // Get (read) a vertical column of "key/value pair" settings until hit a blank key cel, & return the settings values.
        public List<string> GetColumnSettings(int row, int keyCol, int valueCol)
        {
            List<string> settings = new();

            var keyCell = Worksheet.Cells[row, keyCol];
            var valueCell = Worksheet.Cells[row, valueCol];
            while ((keyCell != null) && (keyCell.Value != null) && (keyCell.Value.ToString().Trim() != "") &&
                    (valueCell != null) && (valueCell.Value != null))
            {
                settings.Add(valueCell.Value.ToString());

                // Advance a row
                row++;
                keyCell = Worksheet.Cells[row, keyCol];
                valueCell = Worksheet.Cells[row, valueCol];
            }

            return settings;
        }


        public void SetDataListColumn(ref int row, int col, DataPairList? list, bool showUnknown = true, int extraColOffset = 0)
        {
            if ((list == null) || (Worksheet == null))
                return;

            foreach (var pair in list)
            {
                bool isUnknown = (pair.Ndp >= 0) && (pair.Value == UnknownValue.ToString());
                if (isUnknown && !showUnknown)
                    continue;

                Worksheet.Cells[row, col].Value = pair.Key;
                if (isUnknown)
                    Worksheet.Cells[row, col + LabelToValueCellOffset + extraColOffset].Value = UnknownString;
                else
                    SetDataPairValue(row, col + LabelToValueCellOffset + extraColOffset, pair);
                row++;
            }
        }


        public void SetTitle(ref int row, int col, string title, int fontsize = MediumTitleFontSize)
        {
            var cell = Worksheet.Cells[row, col];
            cell.Value = title;
            cell.Style.Font.Bold = true;
            cell.Style.Font.Size = fontsize;
            cell.Style.Font.Color.SetColor(fontsize == MediumTitleFontSize ? Color.DarkBlue : Color.Blue);
            row++;
        }


        public void SetTitles(string title3)
        {
            int row = 1;
            SetTitle(ref row, 1, Main1Title, LargeTitleFontSize);
            row = 1;
            SetTitle(ref row, 2, Main2Title, LargeTitleFontSize);
            row = 1;
            SetTitle(ref row, 4, title3, LargeTitleFontSize);
        }


        public void SetTitleAndDataListColumn(String title, int firstRow, int col, DataPairList? list, bool showUnknown = true, int extraColOffset = 0)
        {
            int row = firstRow;
            SetTitle(ref row, col, title);
            SetDataListColumn(ref row, col, list, showUnknown, extraColOffset);
        }

        public void SetChartTitle(ExcelChart chart, string title)
        {
            chart.Title.Text = title;
            chart.Title.Font.Size = MediumTitleFontSize;
            chart.Title.Font.Bold = true;
            chart.Title.Font.Color = Color.DarkBlue; // Re obsolete hint: Suggested "Fill" actually fills in the background not title text color. 
        }


        public void SetChart(ExcelChart chart, string title, float rowOffset, int colOffset, int depth, int width = StandardChartCols)
        {
            SetChartTitle(chart, title);

            int startRow = (int)(rowOffset * StandardChartRows);
            chart.SetPosition(startRow, 0, colOffset * StandardChartCols, 0);
            chart.To.Column = colOffset * StandardChartCols + width;
            chart.To.Row = startRow + depth;
        }


        // Get (read) a horizontal row of settings until hit a blank cell 
        public List<string> GetRowSettings(int row, int col)
        {
            List<string> settings = new();

            var cell = Worksheet.Cells[row, col];
            while ((cell != null) && (cell.Value != null) && (cell.Value.ToString() != ""))
            {
                settings.Add(cell.Value.ToString());
                col++;
                cell = Worksheet.Cells[row, col];
            }

            return settings;
        }


        // Load a column of data from a XLS file 
        public List<string>? GetColumnSettingsIfAvailable(string tabName, string titleSuffix, int titleRow, int titleCol = 1)
        {
            try
            {
                // Load the ground elevation data
                if (SelectWorksheet(tabName))
                {
                    var cell = Worksheet.Cells[titleRow, titleCol];
                    if ((cell != null) && (cell.Value != null) && (cell.Value.ToString().Contains(titleSuffix)))
                        return GetColumnSettings(titleRow + 1, titleCol, titleCol + LabelToValueCellOffset);
                }
                return null;
            }
            catch (Exception ex)
            {
                throw ThrowException("DataStore.GetColumnSettingsIfAvailable", ex);
            }
        }


        // Save the values (and possibly keys) for the DataPairList to a row in this datastore
        public void SetDataListRowKeysAndValues(ref int row, DataPairList list)
        {
            if (row == 0)
            {
                // Clear any existing list data
                ClearWorksheet();

                row++;
                SetDataListRowKeys(row, 1, list);
            }

            row++;
            SetDataListRowValues(row, 1, list);
        }


        // Returns "A" to "Z", "AA" to "AZ", "BA" to "BZ", etc.
        public string GetColumnName(int index)
        {
            const string letters = "ABCDEFGHIJKLMNOPQRSTUVWXYZ";

            var value = "";

            if (index >= letters.Length)
                value += letters[index / letters.Length - 1];

            value += letters[index % letters.Length];

            return value;
        }


        // Save the values (and possibly keys) for the DataPairList to a row in this datastore
        public void SetNumberColumnNdp(int column, int ndp)
        {
            if ((Worksheet != null) && (Worksheet.Dimension != null) && (Worksheet.Dimension.End != null))
            {
                var lastRow = Worksheet.Dimension.End.Row;

                string columnName = GetColumnName(column - 1);
                string range = columnName + "2:" + columnName + lastRow;

                string format = "#,##0";
                if (ndp > 0)
                    format += ".";
                for (int i = 0; i < ndp; i++)
                    format += "0";

                Worksheet.Cells[range].Style.Numberformat.Format = format;

            }
        }


        public void SetDataPairValue(int row, int col, DataPair pair)
        {
            int ndp = pair.Ndp;
            if (ndp == 0)
            {
                int value = ConfigBase.StringToInt(pair.Value);
                Worksheet.Cells[row, col].Value = value;
            }
            else if (ndp > 0)
            {
                double value = double.Parse(pair.Value);
                var cell = Worksheet.Cells[row, col];
                cell.Value = value;
                cell.Style.Numberformat.Format = (ndp == 1 ? "0.0" : ndp == 2 ? "0.00" : ndp == 3 ? "0.000" : "0.0000000");
            }
            else
            {
                // Never store an empty string in a cell, as the load will fail.
                string value = pair.Value;
                Worksheet.Cells[row, col].Value = (value == "" ? UnknownString : value);
            }
        }

 
        public void BoldFreezeAndAutoFilterRow1(int cols)
        {
            // Prefix handles transition from "Z1" to "AA1"
            int alphabetsize = 26;
            string prefix = "";
            if (cols > alphabetsize)
            {
                prefix = "A";
                cols -= alphabetsize;
            }

            char letter = ColumnIndexToChar(cols);
            var cellRange = string.Format("A1:{0}{1}1", prefix, letter.ToString());


            Worksheet.Cells[cellRange].AutoFilter = true;
            Worksheet.Cells[cellRange].Style.Font.Bold = true;

            // Freeze the first row and the first column
            Worksheet.View.FreezePanes(2, 2);
        }


        // Split the filename into folder and filename
        public static (string folder, string file) SplitFileName(string fileName)
        {
            if (fileName.Length < 4)
                return ("", fileName);

            int pos = fileName.LastIndexOf('\\');

            if (pos < 0)
                return ("", fileName);

            return (fileName.Substring(0, pos), fileName.Substring(pos + 1));
        }


        public static string RemoveFileNameSuffix(string fileName)
        {
            if (fileName.Length < 4)
                return fileName;

            return fileName.Substring(0, fileName.Length - 4);
        }


        public static string AddFileNameSuffix(string fileName, string suffix)
        {
            return RemoveFileNameSuffix(fileName) + suffix;
        }


        public static string SwapFileNameExtension(string fileName, string newExtension)
        {
            if (fileName.Length < 4)
                return fileName;

            return fileName.Substring(0, fileName.Length - 4) + newExtension;
        }


        // Assuming that the file name is something like F:\SkyComb\Input_Data\Philip_Quirke\DJI_0027.SRT
        // return the string F:\SkyComb\Ground_Data
        public static string GuessGroundDataFolderFromFileName(string fileName)
        {
            if (fileName.Length < 15)
                return "";

            int pos = fileName.IndexOf("Input_Data");

            if (pos < 0)
                return "";

            return fileName.Substring(0, pos) + "Ground_Data";
        }


        // Assuming that the file name is something like F:\SkyComb\Input_Data\Philip_Quirke\DJI_0027.SRT
        // return the string F:\SkyComb\Output_Data
        public static string GuessOutputDataFolderFromFileName(string fileName)
        {
            if (fileName.Length < 15)
                return "";

            int pos = fileName.IndexOf("Input_Data");

            if (pos < 0)
                return "";

            return fileName.Substring(0, pos) + "Output_Data";
        }


        // Set the workbook properties for SkyCombAnalyst output
        public void SetWorkbookAnalystProperties()
        {
            Store.Workbook.Properties.Title = "SkyComb Analyst data store";
            Store.Workbook.Properties.Comments = "Produced by the SkyComb Analyst tool.";
            Store.Workbook.Properties.Application = "SkyCombAnalyst.exe";

            Store.Workbook.Properties.Author = "Philip Quirke";
            Store.Workbook.Properties.Company = "SkyComb Limited";
            Store.Workbook.Properties.AppVersion = CodeVersion;
        }


        // Set the workbook properties for SkyCombFlights output
        public void SetWorkbookFlightsProperties()
        {
            Store.Workbook.Properties.Title = "SkyComb Flights data store";
            Store.Workbook.Properties.Comments = "Produced by the SkyComb Flights tool.";
            Store.Workbook.Properties.Application = "SkyCombFlights.exe";

            Store.Workbook.Properties.Author = "Philip Quirke";
            Store.Workbook.Properties.Company = "SkyComb Limited";
            Store.Workbook.Properties.AppVersion = CodeVersion;
        }


        // Get the Index tab contents
        public DataPairList GetIndex()
        {
            return new DataPairList
                {
                    { IndexTabName, "This tab" },
                    { FilesTabName, "List of drone input files and output files created" },
                    { "", "" },
                    { GroundTabName, "Ground and Surface summary and graphs" },
                    { DemTabName, "Ground elevation data" },
                    { DsmTabName, "Surface (aka tree-top) elevation data" },
                    { SwatheTabName, "Swathe of ground seen by drone" },
                    { "", "" },
                    { DroneTabName, "Summary drone and elevation data" },
                    { Sections1TabName, "Raw drone flight log data table" },
                    { Sections2TabName, "Raw drone flight log graphs" },
                    { Steps1TabName, "Smoothed drone flight log data table" },
                    { Steps2TabName, "Smoothed drone flight log graphs" },
                    { Legs1TabName, "Drone flight legs data table" },
                    { "", "" },
                    { ProcessTabName, "Summary image processing and object data"},
                    { Blocks1TabName, "Processing blocks (aka video frames) data table - combines Step, DEM, DSM, Leg & image data" },
                    { Blocks2TabName, "Processing blocks (aka video frames) graphs" },
                    { FeaturesTabName, "Feature (cluster of hot pixels in one video frame) data table" },
                    { Objects1TabName, "Object (sequence of features across multiple video frames) data table" },
                    { Objects2TabName, "Object graphs - combines object, feature & block data" },
                    { SpanTabName, "Spans (in the blocks) data" },
                    { "", "" },
                    { ObjectCategoryTabName, "Object category (annotations) data table" },
                    { MasterCategoryTabName, "Master category data table" },
                    { PopulationTabName, "Categorised object population Graphs" },
                };
        }


        // Update the Index tab with the current date/time for the 
        // specified tabName. Also record the code version used.
        public void SetLastUpdateDateTime(string tabName)
        {
            if (SelectWorksheet(IndexTabName))
            {
                var indexData = GetIndex();
                int row = IndexContentRow;
                foreach (var index in indexData)
                {
                    if (index.Key == tabName)
                    {
                        Worksheet.Cells[row, 3].Value = DateTime.Now.ToString();

                        // Store the code version that stored the changed data. 
                        // Helps if say the video was processed with an old version of the code,
                        // but the object categories were assigned using a new version of the code.
                        Worksheet.Cells[row, 4].Value = CodeVersion;
                        break;
                    }

                    row++;
                }
            }
        }


        // Save the bitmap to the datastore
        public void SaveBitmap(Bitmap? theBitmap, string name, int row, int col = 0, int percent = 100)
        {
            if(theBitmap == null)
                return; 

            using (MemoryStream stream = new MemoryStream())
            {
                // Save the bitmap into the memory stream as PNG format
                theBitmap.Save(stream, System.Drawing.Imaging.ImageFormat.Png);

                var picture = Worksheet.Drawings.AddPicture(name, stream);
                picture.SetPosition(row, 0, col, 0);
                picture.Border.Width = 0;
                picture.SetSize(percent);
            }
        }

    }
}
