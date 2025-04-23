using SkyCombGround.CommonSpace;
using SkyCombGround.GroundLogic;


namespace SkyCombGround.PersistModel
{
    public class GroundCheck
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

            BaseConstants.Assert(originalData != null, "Original GroundData is null");
            BaseConstants.Assert(reloadedData != null, "Reloaded GroundData is null");

            CompareModels("DEM", originalData.DemModel, reloadedData.DemModel);
            CompareModels("DSM", originalData.DsmModel, reloadedData.DsmModel);
            //CompareModels("Swathe", originalData.SwatheModel, reloadedData.SwatheModel); We do not store summary datums for Swathes

            return reloadedData;
        }

        private static void CompareModels(string label, GroundModel.GroundModel original, GroundModel.GroundModel reloaded)
        {
            if (original == null)
                return;

            BaseConstants.Assert(original.IsDem == reloaded.IsDem, "IsDem mismatch");
            BaseConstants.Assert(original.MinCountryNorthingM == reloaded.MinCountryNorthingM, "MinCountryNorthingM mismatch");
            BaseConstants.Assert(original.MaxCountryNorthingM == reloaded.MaxCountryNorthingM, "MaxCountryNorthingM mismatch");
            BaseConstants.Assert(original.MinCountryEastingM == reloaded.MinCountryEastingM, "MinCountryEastingM mismatch");
            BaseConstants.Assert(original.MaxCountryEastingM == reloaded.MaxCountryEastingM, "MaxCountryEastingM mismatch");
            BaseConstants.Assert(original.MinElevationQuarterM < 0 || // On save we convert negative values to zero (sea-level).
                                 original.MinElevationQuarterM == reloaded.MinElevationQuarterM, "MinElevationQuarterM mismatch");
            BaseConstants.Assert(original.MaxElevationQuarterM == reloaded.MaxElevationQuarterM, "MaxElevationQuarterM mismatch");
            BaseConstants.Assert(original.NumRows == reloaded.NumRows, "NumRows mismatch");
            BaseConstants.Assert(original.NumCols == reloaded.NumCols, "NumCols mismatch");
            BaseConstants.Assert(original.NumDatums == reloaded.NumDatums, "NumDatums mismatch");

            float maxError = 0;

            for (int row = 1; row <= original.NumRows; row++)
                for (int col = 1; col <= original.NumCols; col++)
                {
                    float origVal = original.GetElevationMByGridIndex(row, col);
                    float reloadedVal = reloaded.GetElevationMByGridIndex(row, col);
                    float error = Math.Abs(origVal - reloadedVal);

                    maxError = Math.Max(maxError, error);

                    // We convert negative values to zero (sea-level) on save 
                    BaseConstants.Assert(origVal < 0 || error <= MaxAllowedErrorM,
                        $"{label} elevation mismatch at ({row},{col}): original={origVal}, reloaded={reloadedVal}, error={error}");
                }

            Console.WriteLine($"{label} max error: {maxError}m");
        }
    }
}
