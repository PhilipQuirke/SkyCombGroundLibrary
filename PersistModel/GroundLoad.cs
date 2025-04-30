using SkyCombGround.CommonSpace;
using SkyCombGround.GroundLogic;
using System.Text;


namespace SkyCombGround.PersistModel
{
    // Load meta-data about the ground DEM and DSM elevations from a datastore
    public class GroundLoad : BaseConstants
    {
        // Load Ground elevation settings
        private static (List<string>? groundSettings, List<string>? demSettings, List<string>? dsmSettings)
            LoadSettings(BaseDataStore droneDataStore)
        {
            return (
                droneDataStore.GetColumnSettingsIfAvailable(
                    GroundReportTabName, GroundInputTitle, Chapter1TitleRow, LhsColOffset),
                droneDataStore.GetColumnSettingsIfAvailable(
                    GroundReportTabName, DsmInputTitle, Chapter1TitleRow, MidColOffset),
                droneDataStore.GetColumnSettingsIfAvailable(
                    GroundReportTabName, DemInputTitle, Chapter1TitleRow, RhsColOffset));
        }


        public static void LoadGridOptimized(
           BaseDataStore? dataStore,
           GroundModel.GroundModel? grid,
           string tabName)
        {
            try
            {
                if (grid == null || dataStore == null || !dataStore.SelectWorksheet(tabName))
                    return;

                grid.NumElevationsStored = 0;

                const int charsPerValue = 4;
                int valuesPerRow = grid.NumCols;
                int charsPerRow = valuesPerRow * charsPerValue;
                int numCellsPerRow = (int)Math.Ceiling(valuesPerRow / (double)GroundValuesPerCell);

                for (int row = 1; row <= grid.NumRows; row++)
                {
                    var rowData = new StringBuilder();

                    // Read only expected number of cells
                    for (int col = 1; col <= numCellsPerRow; col++)
                    {
                        var cell = dataStore.Worksheet.Cells[row, col];
                        if (cell?.Value != null)
                            rowData.Append(cell.Value.ToString());
                    }

                    string rowString = rowData.ToString();

                    // Optional check (debugging aid)
                    if (rowString.Length < charsPerRow)
                        throw new Exception($"Row {row} data too short: expected {charsPerRow} characters, got {rowString.Length}");

                    for (int col = 0; col < grid.NumCols; col++)
                    {
                        int startIndex = col * charsPerValue;
                        if (startIndex + charsPerValue <= rowString.Length)
                        {
                            string hexValue = rowString.Substring(startIndex, charsPerValue);
                            float elevation = 0; // Sea-level
                            if (hexValue != "0000")
                            {
                                int compressedValue = Convert.ToInt32(hexValue, 16);
                                elevation = compressedValue / (float)GroundScaleFactor;
                            }
                            grid.AddSettingDatum(row, col + 1, elevation);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                throw new Exception("OptimizedGridProcedures.LoadGridOptimized", ex);
            }
        }




        // Load ground data (if any) from the DataStore 
        public static GroundData? Load(BaseDataStore droneDataStore, bool fullLoad)
        {
            GroundData? groundData = null;

            try
            {
                if (droneDataStore.SelectWorksheet(GroundReportTabName))
                {
                    // Load the summary (settings) data 
                    (var groundSettings, var dsmSettings, var demSettings) = LoadSettings(droneDataStore);
                    groundData = GroundDataFactory.Create(groundSettings, dsmSettings, demSettings);


                    // Load ground (DEM) elevations (if any)
                    if (fullLoad && droneDataStore.SelectWorksheet(DemDataTabName))
                    {
                        LoadGridOptimized(droneDataStore, groundData.DemModel, DemDataTabName);
                        groundData.DemModel.AssertListGood();
                    }


                    // Load surface (DSM) elevations (if any)
                    if (fullLoad && droneDataStore.SelectWorksheet(DsmDataTabName))
                    {
                        LoadGridOptimized(droneDataStore, groundData.DsmModel, DsmDataTabName);
                        groundData.DsmModel.AssertListGood();
                    }


                    // Load surface seen (Swathe) area (if any)
                    if (fullLoad && droneDataStore.SelectWorksheet(SwatheDataTabName))
                    {
                        LoadGridOptimized(droneDataStore, groundData.SwatheModel, SwatheDataTabName);
                        groundData.SwatheModel.AssertListGood();
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("Suppressed GroundLoad.Load failure: " + ex.ToString());
                groundData = null;
            }

            return groundData;
        }
    }
}