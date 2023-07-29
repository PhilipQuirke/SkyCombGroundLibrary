using SkyCombGround.CommonSpace;
using SkyCombGround.GroundSpace;


namespace SkyCombGround.PersistModel
{
    // Load meta-data about the ground DEM and DSM elevations from a datastore
    public class GroundLoad : BaseConstants
    {
        // Load Ground elevation settings
        private static List<string> LoadSettings(BaseDataStore dataStore)
        {
            return dataStore.GetColumnSettingsIfAvailable(
                GroundTabName, GroundInputTitle, 
                Chapter1TitleRow, LhsColOffset);
        }


        // Load all Ground (DEM) or Surface (DSM) data from a XLS file 
        private static void LoadGrid(
            BaseDataStore? dataStore, 
            GroundGrid? grid, 
            string tabName)
        {
            int row = 0;
            int col = 0;
            try
            {
                if ((grid != null) && (dataStore != null) && dataStore.SelectWorksheet(tabName))
                {
                    for( row = 1; row < grid.NumRows + 1; row++ )
                    {
                        for (col = 1; col < grid.NumCols + 1; col++)
                        {
                            var cell = dataStore.Worksheet.Cells[row, col];
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
                throw ThrowException("GroundLoad.LoadData: Row=" + row + " Col=" + col, ex);
            }
        }


        // Load ground data (if any) from the DataStore 
        public static GroundData? Load(BaseDataStore dataStore)
        {
            GroundData? groundData = null;

            try
            {
                if (dataStore.SelectWorksheet(GroundTabName))
                {
                    // Load the summary (settings) data 
                    groundData = GroundDataFactory.Create(LoadSettings(dataStore));


                    // Load ground (DEM) elevations (if any)
                    if (dataStore.SelectWorksheet(DemTabName))
                    {
                        LoadGrid(dataStore, groundData.DemGrid, DemTabName);
                        groundData.DemGrid.AssertGood();
                    }


                    // Load surface (DSM) elevations (if any)
                    if (dataStore.SelectWorksheet(DsmTabName))
                    {
                        LoadGrid(dataStore, groundData.DsmGrid, DsmTabName);
                        groundData.DsmGrid.AssertGood();
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