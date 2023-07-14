// Copyright SkyComb Limited 2023. All rights reserved.
using OfficeOpenXml;
using OfficeOpenXml.Style;
using SkyCombGround.CommonSpace;
using System.Drawing;


namespace SkyCombGround.PersistModel
{

    public class GenericDataStore : ConfigBase
    {
        // Tab Names
        public const string IndexTabName = "Index";
        public const string FilesTabName = "Files";


        // Column offset
        public const int LabelToValueCellOffset = 1;


        // The name of the spreadsheet created as the DataStore
        public string DataStoreFileName { get; set; } = "";
        // A spreadsheet is used as the physical instantiation of the DataStore
        public ExcelPackage? Store { get; set; } = null;
        public ExcelWorksheet? Worksheet { get; set; } = null;


        public bool IsOpen => Store != null;


        // Open an existing DataStore 
        public GenericDataStore(ExcelPackage store, string fileName)
        {
            DataStoreFileName = fileName;
            Store = store;
            Worksheet = null;
        }


        // Create a DataStore on disk & store the Files settings.
        public GenericDataStore(string fileName)
        {
            ExcelPackage.LicenseContext = LicenseContext.NonCommercial;

            DataStoreFileName = fileName;
            Store = new ExcelPackage();
            Worksheet = null;
        }


        // Ensure we are not holding a file handle (or similar resources) open
        public void Close()
        {
            if (IsOpen)
                Store.Dispose();
            Store = null;
            Worksheet = null;
        }


        // Save (and close) the existing datastore to disk.
        public void SaveAndClose()
        {
            Store.Save();
            Close();
        }


        // Open the existing datastore 
        public virtual void Open()
        {
            Store = new(DataStoreFileName);
        }


        // Add a new worksheet
        public void AddWorksheet(string worksheetName)
        {
            Worksheet = Store.Workbook.Worksheets.Add(worksheetName);
        }


        // Return a pointer to an existing worksheet by name
        public ExcelWorksheet ReferWorksheet(string worksheetName)
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
        public (bool, ExcelWorksheet) SelectOrAddWorksheet(string worksheetName)
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
        public static string GetColumnName(int index)
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


        public static string RemoveFileNameSuffix(string filename)
        {
            if (filename.Length < 4)
                return filename;

            return filename.Substring(0, filename.Length - 4);
        }


        public static string AddFileNameSuffix(string filename, string suffix)
        {
            return RemoveFileNameSuffix(filename) + suffix;
        }


        // Swap file extension
        public static string SwapExtension(string fileName, string newExtension)
        {
            if (fileName == "")
                return fileName;

            return fileName.Substring(0, fileName.Length - 4) + newExtension;
        }

    }
}
