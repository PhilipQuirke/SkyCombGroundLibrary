using SkyCombGround.CommonSpace;
using SkyCombGround.GroundModel;
using SkyCombGround.GroundLogic;



namespace SkyCombGround.PersistModel
{
    // Save meta-data about a drone flight, the videos taken, the flight log, and ground DEM and DSM elevations to a datastore, including graphs
    public class GroundSave : BaseConstants
    {
        // Save the ground-elevation/surface-elevation/swathe data
        private static bool SaveGrid(
            BaseDataStore? dataStore,
            GroundModel.GroundModel? grid,
            string tabName)
        {
            int row = 0;
            int col = 0;
            try
            {
                if ((dataStore == null) || (grid == null) || (grid.NumElevationsStored <= 0))
                    return false;

                if (dataStore.SelectWorksheet(tabName))
                    dataStore.ClearWorksheet();

                (var newTab, var ws) = dataStore.SelectOrAddWorksheet(tabName);
                if (ws == null)
                    return false;

                for (row = 1; row < grid.NumRows + 1; row++)
                    for (col = 1; col < grid.NumCols + 1; col++)
                        ws.Cells[row, col].Value = grid.GetElevationMByGridIndex(row, col);
            }
            catch (Exception ex)
            {
                throw ThrowException("GroundSave.SaveData: Row=" + row + " Col=" + col, ex);
            }

            return true;
        }


        // Save ground data (if any) to the DataStore 
        public static void Save(BaseDataStore dataStore, GroundData groundData, bool full)
        {
            if ((dataStore == null) || (groundData == null))
                return;

            (var _, var ws) = dataStore.SelectOrAddWorksheet(GroundTabName);
            if (ws == null)
                return;

            if (full)
            {
                dataStore.SetTitles("Ground");
                dataStore.SetTitleAndDataListColumn(GroundInputTitle, Chapter1TitleRow, 1, groundData.GetSettings());
                dataStore.SetColumnWidth(LhsColOffset, 30);
                dataStore.SetColumnWidth(LhsColOffset + LabelToValueCellOffset, 25);

                if(SaveGrid(dataStore, groundData.DemModel, DemTabName))
                    dataStore.SetLastUpdateDateTime(DemTabName);

                if(SaveGrid(dataStore, groundData.DsmModel, DsmTabName))
                    dataStore.SetLastUpdateDateTime(DsmTabName);

                if(SaveGrid(dataStore, groundData.SwatheModel, SwatheTabName))
                    dataStore.SetLastUpdateDateTime(SwatheTabName);

                dataStore.SelectWorksheet(GroundTabName);

                dataStore.SetLastUpdateDateTime(GroundTabName);
            }
        }
    }
}