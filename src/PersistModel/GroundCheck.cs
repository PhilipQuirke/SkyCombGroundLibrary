using SkyCombGround.CommonSpace;
using SkyCombGround.GroundLogic;


namespace SkyCombGround.PersistModel
{
    public class GroundCheck : GroundConstants
    {
        private const float MaxAllowedErrorM = 0.5f;

        public static GroundData? GroundData_RoundTrip_PreservesElevationsWithinTolerance(GroundData originalData, string tempExcelFile)
        {
            // Act: Reload data from file
            GroundData? reloadedData;
            using (var loadStore = new BaseDataStore(tempExcelFile, false))
            {
                loadStore.Open();
                reloadedData = GroundLoad.Load(loadStore, fullLoad: true);
                loadStore.FreeResources();
            }

            Assert(originalData != null, "Original GroundData is null");
            Assert(reloadedData != null, "Reloaded GroundData is null");

            CompareModels("DEM", originalData.DemModel, reloadedData.DemModel);
            CompareModels("DSM", originalData.DsmModel, reloadedData.DsmModel);
            //CompareModels("Swathe", originalData.SwatheModel, reloadedData.SwatheModel); We do not store summary datums for Swathes

            return reloadedData;
        }

        private static void CompareModels(string label, GroundModel.GroundModel original, GroundModel.GroundModel reloaded)
        {
            if (original == null)
                return;

            bool badData1 = original.MinElevationQuarterM < 0; // On save we convert negative values to zero (sea-level).
            bool badData2 = original.MaxElevationQuarterM > GroundNZMaxDEM * GroundScaleFactor; // On save we convert too large values to zero (sea-level).

            Assert(original.IsDem == reloaded.IsDem, "IsDem mismatch");
            Assert(original.MinCountryNorthingM == reloaded.MinCountryNorthingM, "MinCountryNorthingM mismatch");
            Assert(original.MaxCountryNorthingM == reloaded.MaxCountryNorthingM, "MaxCountryNorthingM mismatch");
            Assert(original.MinCountryEastingM == reloaded.MinCountryEastingM, "MinCountryEastingM mismatch");
            Assert(original.MaxCountryEastingM == reloaded.MaxCountryEastingM, "MaxCountryEastingM mismatch");
            Assert(badData1 || badData2 || original.MinElevationQuarterM == reloaded.MinElevationQuarterM, "MinElevationQuarterM mismatch");
            Assert(badData2 || original.MaxElevationQuarterM == reloaded.MaxElevationQuarterM, "MaxElevationQuarterM mismatch");
            Assert(original.NumRows == reloaded.NumRows, "NumRows mismatch");
            Assert(original.NumCols == reloaded.NumCols, "NumCols mismatch");
            Assert(original.NumDatums == reloaded.NumDatums, "NumDatums mismatch");

            float maxError = 0;

            if (!badData2)
                for (int row = 1; row <= original.NumRows; row++)
                    for (int col = 1; col <= original.NumCols; col++)
                    {
                        float origVal = original.GetElevationMByGridIndex(row, col);
                        float reloadedVal = reloaded.GetElevationMByGridIndex(row, col);
                        float error = Math.Abs(origVal - reloadedVal);

                        maxError = Math.Max(maxError, error);

                        // We convert negative values to zero (sea-level) on save 
                        Assert(origVal < 0 || error <= MaxAllowedErrorM,
                            $"{label} elevation mismatch at ({row},{col}): original={origVal}, reloaded={reloadedVal}, error={error}");
                    }

            Console.WriteLine($"{label} max error: {maxError}m");
        }
    }
}
