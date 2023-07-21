using OfficeOpenXml.Drawing.Chart;
using SkyCombGround.CommonSpace;
using SkyCombGround.GroundSpace;
using System.Drawing;


namespace SkyCombGround.PersistModel
{
    // Save meta-data about a drone flight, the videos taken, the flight log, and ground DEM and DSM elevations to a datastore, including graphs
    public class GroundSave : BaseConstants
    {
        // Save the ground/surface elevation data
        public static void SaveGrid(
            GenericDataStore dataStore,
            GroundGrid grid,
            string tabName)
        {
            int row = 0;
            int col = 0;
            try
            {
                if ((dataStore == null) || (grid == null) || (grid.NumDatums == 0))
                    return;

                if (dataStore.SelectWorksheet(tabName))
                    dataStore.ClearWorksheet();

                (var newTab, var ws) = dataStore.SelectOrAddWorksheet(tabName);
                if (ws == null)
                    return;

                for (row = 1; row < grid.NumRows + 1; row++)
                {
                    for (col = 1; col < grid.NumCols + 1; col++)
                    {
                        var cell = ws.Cells[row, col];
                        cell.Value = grid.GetElevationMByGridIndex(row, col);
                        //cell.Style.Numberformat.Format = (ndp == 1 ? "0.0" : ndp == 2 ? "0.00" : ndp == 3 ? "0.000" : "0.0000000");
                    }
                }
            }
            catch (Exception ex)
            {
                throw ThrowException("GroundSave.SaveData: Row=" + row + " Col=" + col, ex);
            }
        }


        // Save the ground/surface elevation surface chart
        public static void SaveSurface(
            GenericDataStore dataStore,
            GroundGrid grid,
            string tabName,
            string chartName3D, string chartTitle3D, 
            Color lowColor, Color highColor, int rowOffset)
        {
/* Come back to this later
            try
            {
                if ((dataStore == null) || (grid == null) || (grid.NumDatums == 0))
                    return;

                dataStore.SelectWorksheet(GroundTabName);
                var groundWS = dataStore.Worksheet;

                if (groundWS.Drawings[chartName3D] != null)
                    return;

                dataStore.SelectWorksheet(tabName);
                var dataWS = dataStore.Worksheet;

                // Example =DEM!$A$1:$BD$55
                //string dataRangeAddress = "A1:" + dataStore.GetColumnName(grid.NumCols) + (grid.NumRows + 1);
                string dataArea = "$A$1:$" + dataStore.GetColumnName(grid.NumCols) + "$" + (grid.NumRows + 1);
                string xArea = "$A$1:$A$" + (grid.NumRows + 1);
                string yArea = "$A$1:$" + dataStore.GetColumnName(grid.NumCols) + "$1";

                (float minAltitude, float maxAltitude) = grid.GetMinMaxElevationM();

                // Create 3D surface graph of the elevation data pivot
                // Refer sample https://github.com/EPPlusSoftware/EPPlus.Sample.NetFramework/blob/master/18-PivotTables/PivotTablesSample.cs 
                var chart = groundWS.Drawings.AddSurfaceChart(chartName3D, eSurfaceChartType.Surface);
                dataStore.SetChart(chart, chartTitle3D, 0, 1, 0);

                // Set the data range for the chart
                //var series = chart.Series.Add(dataArea);
                //var series = chart.Series.Add((dataWS.Cells["A2:A36"],
                //                              dataWS.Cells["A1:Z1"].Offset(0, 1));
                var series = chart.Series.Add(dataWS.Cells[yArea],
                                              dataWS.Cells[xArea]);

                chart.SetPosition(rowOffset, 0, 3, 0);
                chart.To.Column = 6 + StandardChartCols;
                chart.To.Row = 1 + LargeChartRows;
                chart.Legend.Remove();

                // Y axis is vertical and shows elevation
                chart.YAxis.MinValue = Math.Round(minAltitude - 5); 
                chart.YAxis.MaxValue = Math.Round(maxAltitude + 5); 

                chart.YAxis.Title.Text = "Elevation";
                chart.XAxis.Title.Text = "Northing";
                chart.View3D.HeightPercent = 25;
                chart.View3D.DepthPercent = 200;  // Takes values from 20 to 2000. Choose 200 by trial and error 

                if (!chart.HasLegend)
                    chart.Legend.Add();
                chart.Legend.Position = eLegendPosition.Left;
            }
            catch (Exception ex)
            {
                throw ThrowException("GroundSave.SavePivot", ex);
            }
*/
        }


        // Save ground data (if any) to the DataStore 
        public static void Save(GenericDataStore dataStore, GroundData groundData, bool full)
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
                dataStore.SetColumnWidth(LhsColOffset, 25);
                dataStore.SetColumnWidth(LhsColOffset + LabelToValueCellOffset, 25);

                SaveGrid(dataStore, groundData.DemGrid, DemTabName);
                dataStore.SetLastUpdateDateTime(DemTabName);

                SaveGrid(dataStore, groundData.DsmGrid, DsmTabName);
                dataStore.SetLastUpdateDateTime(DsmTabName);

                dataStore.SelectWorksheet(GroundTabName);

                SaveSurface(dataStore, groundData.DemGrid, DemTabName,  
                    "DemChart3D", "Ground Elevation (aka DEM) in metres",
                    GroundColors.GroundLowColor, GroundColors.GroundHighColor, 1);

                SaveSurface(dataStore, groundData.DsmGrid, DsmTabName,  
                    "DsmChart3D", "Surface Elevation (aka DSM aka tree-top) in metres",
                    GroundColors.SurfaceLowColor, GroundColors.SurfaceHighColor, 20);

                dataStore.SetLastUpdateDateTime(GroundTabName);
            }
        }
    }
}