using SkyCombGround.CommonSpace;
using SkyCombGround.GroundLogic;
using SkyCombGround.GroundModel;
using System.Text;


namespace SkyCombGround.PersistModel
{
    // Save meta-data about a drone flight, the videos taken, the flight log, and ground DEM and DSM elevations to a datastore, including graphs
    public class GroundSave : GroundConstants
    {
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

                // At the beginning of the method, add special handling for SwatheModel
                if (grid is SwatheModel)
                {
                    // For SwatheModel, store raw 0/1 values without scaling
                    for (int row = 1; row <= grid.NumRows; row++)
                    {
                        var rowData = new StringBuilder();
                        for (int col = 1; col <= grid.NumCols; col++)
                        {
                            short seenValue = ((SwatheModel)grid).GetSeenValue(row, col);
                            rowData.Append(seenValue.ToString("X4"));
                        }

                        string rowString = rowData.ToString();
                        for (int i = 0; i < rowString.Length; i += GroundValuesPerCell * 4)
                        {
                            int cellIndex = i / (GroundValuesPerCell * 4) + 1;
                            int length = Math.Min(GroundValuesPerCell * 4, rowString.Length - i);
                            ws.Cells[row, cellIndex].Value = rowString.Substring(i, length);
                        }
                    }
                }
                else
                {
                    for (int row = 1; row <= grid.NumRows; row++)
                    {
                        var rowData = new StringBuilder();
                        for (int col = 1; col <= grid.NumCols; col++)
                        {
                            float elevation = grid.GetElevationMByGridIndex(row, col);

                            // LINZ does not provide elevations over the sea, so there may be gaps in the data. Store as 0m.
                            if (elevation < 0)
                                elevation = 0.0f;

                            Assert(elevation < GroundNZMaxDEM, "Elevation is bad (too large)");

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
                }

                return true;
            }
            catch (Exception ex)
            {
                throw new Exception("OptimizedGridProcedures.SaveGridOptimized", ex);
            }
        }


        // Save ground data report and graphs
        public static void Save(BaseDataStore? dataStore, GroundData? groundData)
        {
            if ((dataStore == null) || (groundData == null))
                return;

            dataStore.SelectOrAddWorksheet(GroundReportTabName);

            dataStore.SetLargeTitle(GroundReportTitle);

            dataStore.SetTitleAndDataListColumn(GroundInputTitle, Chapter1TitleRow, LhsColOffset, groundData.GetSettings());
            dataStore.SetColumnWidth(LhsColOffset, 25);
            dataStore.SetColumnWidth(LhsColOffset + LabelToValueCellOffset, 25);

            dataStore.SetTitleAndDataListColumn(DsmInputTitle, Chapter1TitleRow, MidColOffset, groundData.GetDsmSettings());
            dataStore.SetColumnWidth(MidColOffset, 25);
            dataStore.SetColumnWidth(MidColOffset + LabelToValueCellOffset, 10);

            dataStore.SetTitleAndDataListColumn(DemInputTitle, Chapter1TitleRow, RhsColOffset, groundData.GetDemSettings());
            dataStore.SetColumnWidth(RhsColOffset, 25);
            dataStore.SetColumnWidth(RhsColOffset + LabelToValueCellOffset, 10);

            SaveGridOptimized(dataStore, groundData.DsmModel, DsmDataTabName);
            SaveGridOptimized(dataStore, groundData.DemModel, DemDataTabName);
            SaveGridOptimized(dataStore, groundData.SwatheModel, SwatheDataTabName);

            dataStore.SelectWorksheet(GroundReportTabName);

            dataStore.HideWorksheet(DemDataTabName);
            dataStore.HideWorksheet(DsmDataTabName);
            dataStore.HideWorksheet(SwatheDataTabName);
        }
    }
}