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
                    GroundTabName, GroundInputTitle, Chapter1TitleRow, LhsColOffset),
                droneDataStore.GetColumnSettingsIfAvailable(
                    GroundTabName, DsmInputTitle, Chapter1TitleRow, MidColOffset),
                droneDataStore.GetColumnSettingsIfAvailable(
                    GroundTabName, DemInputTitle, Chapter1TitleRow, RhsColOffset));
        }


        // Load all Ground (DEM) or Surface (DSM) data from a XLS file 
        private static void LoadGrid(
            BaseDataStore? droneDataStore,
            GroundModel.GroundModel? grid,
            string tabName)
        {
            int row = 0;
            int col = 0;
            try
            {
                if ((grid != null) && (droneDataStore != null) && droneDataStore.SelectWorksheet(tabName))
                {
                    grid.NumElevationsStored = 0;
                    for (row = 1; row < grid.NumRows + 1; row++)
                    {
                        for (col = 1; col < grid.NumCols + 1; col++)
                        {
                            var cell = droneDataStore.Worksheet.Cells[row, col];
                            if (cell != null && cell.Value != null)
                            {
                                var elevationStr = cell.Value.ToString();
                                if ((elevationStr != null) && (elevationStr != ""))
                                {
                                    float elevationM = float.Parse(elevationStr);
                                    grid.AddSettingDatum(row, col, elevationM);
                                }
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                throw ThrowException("GroundLoad.LoadGrid: Row=" + row + " Col=" + col, ex);
            }
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

                for (int row = 1; row <= grid.NumRows; row++)
                {
                    var rowData = new StringBuilder();
                    for (int col = 1; ; col++)
                    {
                        var cell = dataStore.Worksheet.Cells[row, col];
                        if (cell?.Value == null)
                            break;
                        rowData.Append(cell.Value.ToString());
                    }

                    string rowString = rowData.ToString();
                    for (int col = 0; col < grid.NumCols; col++)
                    {
                        if (col * 4 + 4 <= rowString.Length)
                        {
                            string hexValue = rowString.Substring(col * 4, 4);
                            int compressedValue = Convert.ToInt32(hexValue, 16);
                            float elevation = compressedValue / (float)GroundScaleFactor;
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
                if (droneDataStore.SelectWorksheet(GroundTabName))
                {
                    // Load the summary (settings) data 
                    (var groundSettings, var dsmSettings, var demSettings) = LoadSettings(droneDataStore);
                    groundData = GroundDataFactory.Create(groundSettings, dsmSettings, demSettings);


                    // Load ground (DEM) elevations (if any)
                    if (fullLoad && droneDataStore.SelectWorksheet(DemTabName))
                    {
                        LoadGridOptimized(droneDataStore, groundData.DemModel, DemTabName);
                        groundData.DemModel.AssertListGood();
                    }


                    // Load surface (DSM) elevations (if any)
                    if (fullLoad && droneDataStore.SelectWorksheet(DsmTabName))
                    {
                        LoadGridOptimized(droneDataStore, groundData.DsmModel, DsmTabName);
                        groundData.DsmModel.AssertListGood();
                    }


                    // Load surface seen (Swathe) area (if any)
                    if (fullLoad && droneDataStore.SelectWorksheet(SwatheTabName))
                    {
                        LoadGridOptimized(droneDataStore, groundData.SwatheModel, SwatheTabName);
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