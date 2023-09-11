using SkyCombGround.CommonSpace;
using SkyCombGround.GroundLogic;


namespace SkyCombGround.PersistModel
{
    // Load meta-data about the ground DEM and DSM elevations from a datastore
    public class GroundLoad : BaseConstants
    {
        // Load Ground elevation settings
        private static (List<string>? groundSettings, List<string>? demSettings, List<string>? dsmSettings ) 
            LoadSettings(BaseDataStore droneDataStore)
        {
            return (
                droneDataStore.GetColumnSettingsIfAvailable(
                    GroundTabName, GroundInputTitle, Chapter1TitleRow, LhsColOffset),
                droneDataStore.GetColumnSettingsIfAvailable(
                    GroundTabName, DemInputTitle, Chapter1TitleRow, MidColOffset),
                droneDataStore.GetColumnSettingsIfAvailable(
                    GroundTabName, DsmInputTitle, Chapter1TitleRow, RhsColOffset));
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
                    for( row = 1; row < grid.NumRows + 1; row++ )
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


        // Load ground data (if any) from the DataStore 
        public static GroundData? Load(BaseDataStore droneDataStore)
        {
            GroundData? groundData = null;

            try
            {
                if (droneDataStore.SelectWorksheet(GroundTabName))
                {
                    // Load the summary (settings) data 
                    (var groundSettings, var demSettings, var dsmSettings) = LoadSettings(droneDataStore);
                    groundData = GroundDataFactory.Create(groundSettings, demSettings, dsmSettings);


                    // Load ground (DEM) elevations (if any)
                    if (droneDataStore.SelectWorksheet(DemTabName))
                    {
                        LoadGrid(droneDataStore, groundData.DemModel, DemTabName);
                        groundData.DemModel.AssertListGood();
                    }


                    // Load surface (DSM) elevations (if any)
                    if (droneDataStore.SelectWorksheet(DsmTabName))
                    {
                        LoadGrid(droneDataStore, groundData.DsmModel, DsmTabName);
                        groundData.DsmModel.AssertListGood();
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