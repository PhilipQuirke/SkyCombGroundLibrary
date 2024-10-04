using SkyCombGround.CommonSpace;
using SkyCombGround.GroundLogic;
using System.Text;



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


        public static bool SaveGridOptimized(
            BaseDataStore? dataStore,
            GroundModel.GroundModel? grid,
            string tabName)
        {
            try
            {
                if (dataStore == null || grid == null || grid.NumElevationsStored <= 0)
                    return false;

                (var newTab, var ws) = dataStore.SelectOrAddWorksheet(tabName);
                if (ws == null)
                    return false;

                dataStore.ClearWorksheet();

                for (int row = 1; row <= grid.NumRows; row++)
                {
                    var rowData = new StringBuilder();
                    for (int col = 1; col <= grid.NumCols; col++)
                    {
                        float elevation = grid.GetElevationMByGridIndex(row, col);
                        // Convert to integer (multiply by ScaleFactor to preserve 0.25 intervals)
                        int compressedValue = (int)(elevation * GroundScaleFactor);
                        rowData.Append(compressedValue.ToString("X4")); // Use 4-digit hex to support higher values
                    }

                    // Split the row data into multiple cells if necessary
                    string rowString = rowData.ToString();
                    for (int i = 0; i < rowString.Length; i += GroundValuesPerCell * 4)
                    {
                        int cellIndex = i / (GroundValuesPerCell * 4) + 1;
                        int length = Math.Min(GroundValuesPerCell * 4, rowString.Length - i);
                        ws.Cells[row, cellIndex].Value = rowString.Substring(i, length);
                    }
                }

                return true;
            }
            catch (Exception ex)
            {
                throw new Exception("OptimizedGridProcedures.SaveGridOptimized", ex);
            }
        }


        // Save ground data (if any) to the DataStore 
        public static void Save(BaseDataStore? dataStore, GroundData? groundData)
        {
            if ((dataStore == null) || (groundData == null))
                return;

            (var _, var ws) = dataStore.SelectOrAddWorksheet(GroundTabName);
            if (ws == null)
                return;

            dataStore.SetTitles("Ground");

            dataStore.SetTitleAndDataListColumn(GroundInputTitle, Chapter1TitleRow, LhsColOffset, groundData.GetSettings());
            dataStore.SetColumnWidth(LhsColOffset, 25);
            dataStore.SetColumnWidth(LhsColOffset + LabelToValueCellOffset, 25);

            dataStore.SetTitleAndDataListColumn(DsmInputTitle, Chapter1TitleRow, MidColOffset, groundData.GetDsmSettings());
            dataStore.SetColumnWidth(MidColOffset, 25);
            dataStore.SetColumnWidth(MidColOffset + LabelToValueCellOffset, 10);

            dataStore.SetTitleAndDataListColumn(DemInputTitle, Chapter1TitleRow, RhsColOffset, groundData.GetDemSettings());
            dataStore.SetColumnWidth(RhsColOffset, 25);
            dataStore.SetColumnWidth(RhsColOffset + LabelToValueCellOffset, 10);

            if (SaveGridOptimized(dataStore, groundData.DsmModel, DsmTabName))
                dataStore.SetLastUpdateDateTime(DsmTabName);

            if (SaveGridOptimized(dataStore, groundData.DemModel, DemTabName))
                dataStore.SetLastUpdateDateTime(DemTabName);

            if (SaveGridOptimized(dataStore, groundData.SwatheModel, SwatheTabName))
                dataStore.SetLastUpdateDateTime(SwatheTabName);

            dataStore.SelectWorksheet(GroundTabName);

            dataStore.HideWorksheet(DemTabName);
            dataStore.HideWorksheet(DsmTabName);
            dataStore.HideWorksheet(SwatheTabName);

            dataStore.SetLastUpdateDateTime(GroundTabName);
        }
    }
}