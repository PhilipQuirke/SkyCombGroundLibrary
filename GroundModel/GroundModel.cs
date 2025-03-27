// Copyright SkyComb Limited 2024. All rights reserved. 
using SkyCombGround.CommonSpace;
using System.Drawing;


namespace SkyCombGround.GroundModel
{
    // Class supports drawing various backgrounds
    public enum GroundType
    {
        DsmElevations,  // Surface elevations
        DemElevations,  // Ground elevations
        SwatheSeen      // Area overflown by drone and seen by video camera
    }


    // Holds ground elevation data at all locations in a rectangular area (grid).
    public class GroundModel : BaseConstants
    {
        public static string DsmTitle = "Surface (aka tree-top, DSM) elevations";
        public static string DemTitle = "Earth (aka ground, DEM) elevations";
        public static string SwatheTitle = "Swathe seen";


        // The drone video footage extends beyond the flight path locations, so we add a buffer.
        public const int GroundBufferM = 50;


        // The DEM & DSM data is available in a grid of 1 m x 1 m cells,
        // with heights in 0.25m increments.
        public const float VerticalUnitM = 0.25f;


        // Is this DEM data? Else is DSM data. 
        public bool IsDem { get; }

        // DEM/DSM range from ~0 to 3,754 m (NZ's highest mountain).
        // In C#, short ints (signed 16-bit integer) can store values from -32,768 to 32,767
        // So a short int can store the DEM/DSM height values / VerticalUnitM.
        // We store the elevations in a Northing by Easting (2D array) in VerticalUnitM values
        protected short[] ElevationQuarterM { get; set; }
        // Number of elevations values stored into the elevation array
        public int NumElevationsStored { get; set; } = UnknownValue;

        // Min/Max elevation
        public short MaxElevationQuarterM { get; set; }
        public short MinElevationQuarterM { get; set; }

        // The Northing/Easting coverage of the datums (including BufferM on each side)
        // in the local coordinate system. (Not drone zero-based coordinates.)
        public int MaxCountryNorthingM { get; set; } // e.g. 5786068
        public int MinCountryNorthingM { get; set; } // e.g. 5786000
        public int MaxCountryEastingM { get; set; } // e.g. 1954744
        public int MinCountryEastingM { get; set; } // e.g. 1954000


        // Where are we getting this elevation data from?
        public string Source { get; set; }

        // How accurate is this vertical data (in +/- meters)
        // Most available global data sets are +/- 20 meters
        // Local data sets, based on Lidar, can be accurate to +/- 0.25 meters
        public float ElevationAccuracyM { get; set; }


        public int NumRows
        {
            get
            {
                return MaxCountryNorthingM - MinCountryNorthingM + 1;
            }
        }
        public int NumCols
        {
            get
            {
                return MaxCountryEastingM - MinCountryEastingM + 1;
            }
        }


        // Number of datums required to cover the ground area
        // The 27.5 minute video DJI_20230630201405_0001_T.mp4
        // covers 840 m x 974 m = 817,560 m2.
        // Plus buffer gives so 880 m x 1014 m = 892,320 m2. So need to store 892K elevations
        public int NumDatums
        {
            get
            {
                return NumRows * NumCols;
            }
        }


        // Percentage of datums we found a valid elevation for
        public int PercentDatumElevationsAvailable
        {
            get
            {
                if (NumElevationsStored <= 0)
                    return 0;

                return (int)Math.Round(100.0f * NumElevationsStored / NumDatums);
            }
        }


        // Representation of the ground area in country coordinates
        public RectangleF TargetCountryAreaM()
        {
            return new RectangleF(
                MinCountryEastingM,
                MinCountryNorthingM,
                NumCols,
                NumRows);
        }


        // Partial constructor initialisation of the GroundGrid
        private void Initialise()
        {
            ElevationQuarterM = new short[NumDatums];
            for (int i = 0; i < NumDatums; i++)
                ElevationQuarterM[i] = UnknownValue;

            MaxElevationQuarterM = UnknownValue;
            MinElevationQuarterM = UnknownValue;

            ElevationAccuracyM = UnknownValue;
        }


        public GroundModel(bool isDem, RelativeLocation minCountryLocnM, RelativeLocation maxCountryLocnM)
        {
            IsDem = isDem;
            Source = "";
            NumElevationsStored = 0;

            Assert(minCountryLocnM != null, "GroundModel: minCountryLocnM missing");
            Assert(maxCountryLocnM != null, "GroundModel: maxCountryLocnM missing");

            MinCountryNorthingM = (int)(minCountryLocnM.NorthingM - GroundBufferM);
            MinCountryEastingM = (int)(minCountryLocnM.EastingM - GroundBufferM);
            MaxCountryNorthingM = (int)(maxCountryLocnM.NorthingM + GroundBufferM);
            MaxCountryEastingM = (int)(maxCountryLocnM.EastingM + GroundBufferM);

            Initialise();
            AssertGood();
        }


        public GroundModel(bool isDem, List<string>? settings)
        {
            IsDem = isDem;
            Source = "";
            NumElevationsStored = 0;

            Assert(settings != null, "GroundModel: settings missing");

            LoadSettings(settings);

            Initialise();
            AssertGood();
        }


        // Has this object obtained some elevation data?
        public bool HasElevationData()
        {
            return NumElevationsStored > 0;
        }


        public void AssertGood()
        {
            Assert(MinCountryNorthingM < MaxCountryNorthingM, "GroundModel: NorthingM ordering");
            Assert(MinCountryEastingM < MaxCountryEastingM, "GroundModel: EastingM ordering");

            Assert(MinCountryNorthingM > 4500000, "GroundModel: NorthingM coord bad");
            Assert(MinCountryEastingM > 1350000, "GroundModel: EastingM coord bad");
        }


        // The list must have at least 4 points. Generally it has 1000s of points.
        public void AssertListGood()
        {
            FailIf(NumElevationsStored < 4, "GroundModel.AssertListGood: Not enough ground data points.");
        }


        public void AssertGoodIndex(string usecase, int index)
        {
            try
            {
                Assert(index >= 0, "GroundModel." + usecase + ": answer low");
                Assert(index < NumDatums, "GroundModel." + usecase + ": answer high");
            }
            catch (Exception ex)
            {
                throw ThrowException(ex.ToString());
            }
        }



        // Convert from drone location (e.g. [14,3] ) to a grid index. 
        protected int DroneLocnToGridIndex(RelativeLocation droneLocnM, bool strict = true)
        {
            int index =
                ((int)(droneLocnM.NorthingM) + GroundBufferM) * NumCols +
                ((int)(droneLocnM.EastingM) + GroundBufferM);

            if (strict)
                AssertGoodIndex("DroneLocnToGridIndex", index);
            else if ((index < 0) || (index >= NumDatums))
                index = UnknownValue;

            return index;
        }


        // Translate from country location (e.g. [5786000,1954744] ) to grid index
        private int CountryLocnToGridIndex(RelativeLocation countryLocnM)
        {
            int answer =
                ((int)(countryLocnM.NorthingM) - MinCountryNorthingM) * NumCols +
                ((int)(countryLocnM.EastingM) - MinCountryEastingM);

            AssertGoodIndex("CountryLocnToGridIndex", answer);

            return answer;
        }


        private float GridElevationQuarterMToM(int quarterMs)
        {
            if (quarterMs == UnknownValue)
                return UnknownValue;

            return 1.0f * quarterMs * VerticalUnitM;
        }


        // Return the minimum & maximum ground elevations (excluding UnknownValue) in meters
        public (float minElevationM, float maxElevationM) GetMinMaxElevationM()
        {
            return (GridElevationQuarterMToM(MinElevationQuarterM),
                    GridElevationQuarterMToM(MaxElevationQuarterM));
        }


        // For any gaps we assume they are the minimum 
        // This case occurs when say 50% of the flight is over water, and the Lidar data source returns no data for water locations.
        // This water may be the sea, or a lake at a higher altitude, so we use the minimum elevation of that layer.
        public void SetGapsToMinimum()
        {
            for (int i = 0; i < NumDatums; i++)
                if (ElevationQuarterM[i] == UnknownValue)
                    ElevationQuarterM[i] = MinElevationQuarterM;
        }


        // For a "query" point inside the grid, calculate the elevation.
        // As the grid is 1m by 1m cells, the horizontal difference in
        // distance from queryLocn to the closest point is not important. 
        public float GetElevationByDroneLocn(DroneLocation droneLocnM)
        {
            try
            {
                // Because of GroundBufferM, the drone should not be near the edge of the grid.
                // But objects in the area seen by the camera may be near or past the edge of the grid.
                // And a lack of DEM & DSM Lidar data may mean that the grid is not as big as we want.
                int gridIndex = DroneLocnToGridIndex(droneLocnM, false);
                if (gridIndex == UnknownValue)
                    return UnknownValue;

                if (ElevationQuarterM[gridIndex] == UnknownValue)
                    return UnknownValue;

                return GridElevationQuarterMToM(ElevationQuarterM[gridIndex]);
            }
            catch (Exception ex)
            {
                throw ThrowException(ex.ToString());
            }
        }


        // Used with Loading from DataStore, to set the elevation of a grid point.
        // Row and Col are one-based. Can return UnknownValue.
        public float GetElevationMByGridIndex(int oneRow, int oneCol)
        {
            int gridIndex = (oneRow - 1) * NumCols + (oneCol - 1);

            AssertGoodIndex("GetElevationByGridIndex", gridIndex);

            return GridElevationQuarterMToM(ElevationQuarterM[gridIndex]);
        }


        private void AddDatum(int gridIndex, float elevationM)
        {
            if (ElevationQuarterM[gridIndex] != UnknownValue)
                // We have already updated this grid point
                // Do not change NumElevationsStored or Max/MinElevationQuarterM
                return;

            if (elevationM == UnknownValue)
            {
                ElevationQuarterM[gridIndex] = UnknownValue;
                // Do not change NumElevationsStored or Max/MinElevationQuarterM
                return;
            }

            short quarterMs = (short)(elevationM / VerticalUnitM);

            ElevationQuarterM[gridIndex] = quarterMs;

            NumElevationsStored++;

            if (MaxElevationQuarterM == UnknownValue)
            {
                MaxElevationQuarterM = quarterMs;
                MinElevationQuarterM = quarterMs;
            }
            else
            {
                MaxElevationQuarterM = Math.Max(quarterMs, MaxElevationQuarterM);
                MinElevationQuarterM = Math.Min(quarterMs, MinElevationQuarterM);
            }
        }


        // Add a datum, in world local coordinate system (e.g. 115028,262743)
        public void AddCountryDatum(RelativeLocation countryLocnM, float elevationM)
        {
            int gridIndex = CountryLocnToGridIndex(countryLocnM);

            AddDatum(gridIndex, elevationM);
        }


        // Add a datum from spreadsheet. Row and col are 1 based
        public void AddSettingDatum(int oneRow, int oneCol, float elevationM)
        {
            int gridIndex = (oneRow - 1) * NumCols + (oneCol - 1);

            AssertGoodIndex("AddSettingDatum", gridIndex);

            AddDatum(gridIndex, elevationM);
        }


        public DataPairList GetSettings()
        {
            return new DataPairList{
                { "Source", Source },
                { "Min Country Easting M", MinCountryEastingM },
                { "Min Country Northing M", MinCountryNorthingM },
                { "Max Country Easting M", MaxCountryEastingM },
                { "Max Country Northing M", MaxCountryNorthingM },
                { "Elevation Accuracy M", ElevationAccuracyM, 1 },
                { "Max Elevation Quarter M", MaxElevationQuarterM },
                { "Min Elevation Quarter M", MinElevationQuarterM },
                { "Num Elevations Stored", NumElevationsStored },
            };
        }


        public void LoadSettings(List<string>? settings)
        {
            if (settings == null)
                return;

            Source = settings[0];
            MinCountryEastingM = ConfigBase.StringToInt(settings[1]);
            MinCountryNorthingM = ConfigBase.StringToInt(settings[2]);
            MaxCountryEastingM = ConfigBase.StringToInt(settings[3]);
            MaxCountryNorthingM = ConfigBase.StringToInt(settings[4]);
            ElevationAccuracyM = ConfigBase.StringToFloat(settings[5]);
            MaxElevationQuarterM = (short)ConfigBase.StringToInt(settings[6]);
            MinElevationQuarterM = (short)ConfigBase.StringToInt(settings[7]);
            NumElevationsStored = ConfigBase.StringToInt(settings[8]);
        }
    }


    public class GroundModelList : List<GroundModel>
    {
        public GroundModelList()
        {
        }


        // Return the minimum & maximum ground elevations (excluding UnknownValue) in meters
        public (float minElevationM, float maxElevationM) GetMinMaxElevationM()
        {
            float minElevationM = -BaseConstants.UnknownValue;
            float maxElevationM = BaseConstants.UnknownValue;

            foreach (var groundModel in this)
            {
                (float currMinElevationM, float currMaxElevationM) = groundModel.GetMinMaxElevationM();

                if (currMinElevationM != BaseConstants.UnknownValue)
                    minElevationM = Math.Min(minElevationM, currMinElevationM);

                if (currMaxElevationM != BaseConstants.UnknownValue)
                    maxElevationM = Math.Max(maxElevationM, currMaxElevationM);
            }

            return (minElevationM, maxElevationM);
        }

    }




    // Class to calculate the swathe of GroundModel that was "seen" by the drone's video during flight.
    public class SwatheModel : GroundModel
    {
        public SwatheModel(GroundModel grid) :
            base(false,
                new RelativeLocation(grid.MinCountryNorthingM, grid.MinCountryEastingM),
                new RelativeLocation(grid.MaxCountryNorthingM, grid.MaxCountryEastingM))
        {
            // GroundSwathe uses 0 as the "UnknownValue". 0 also means "not seen"
            for (int i = 0; i < NumDatums; i++)
                ElevationQuarterM[i] = 0;
        }


        public SwatheModel(List<string>? settings) : base(false, settings)
        {
        }


        public int M2Seen { get { return NumElevationsStored; } }


        // Set point to seen - the point is part of the drone swathe
        private void DronePointSeen(RelativeLocation droneLocnM)
        {
            var index = DroneLocnToGridIndex(droneLocnM, false);

            // With a camera pointing near horizontal, the area seen can
            // extend qway past the grid area
            if ((index < 0) || (index >= NumDatums))
                return;

            if (ElevationQuarterM[index] == 0)
            {
                ElevationQuarterM[index] = 1;
                NumElevationsStored++;
            }
        }


        // Set line to seen - the line is part of the drone swathe
        private void DroneLineSeen(RelativeLocation fromDroneLocn, RelativeLocation toDroneLocn)
        {
            var distance = (float)RelativeLocation.DistanceM(fromDroneLocn, toDroneLocn);
            if (distance > 3)
            {
                var translationStep = new RelativeLocation(
                    (toDroneLocn.NorthingM - fromDroneLocn.NorthingM) / distance,
                    (toDroneLocn.EastingM - fromDroneLocn.EastingM) / distance);
                for (int edgeStep = 1; edgeStep < distance; edgeStep++)
                {
                    var locn = new RelativeLocation(
                        fromDroneLocn.NorthingM + translationStep.NorthingM * edgeStep,
                        fromDroneLocn.EastingM + translationStep.EastingM * edgeStep);
                    DronePointSeen(locn);
                }
            }
        }


        // Given the (rotated) rectangle defined by the 4 corners, set the grid area as seen.
        // The rectangle is part of the drone swathe.
        // Assuming we are painting a sequence of flightsteps, painting the edges and diagonals
        // seems sufficient to represent the seen area.
        public void DroneRectSeen(RelativeLocation topLeftLocn, RelativeLocation topRightLocn, RelativeLocation bottomRightLocn, RelativeLocation bottomLeftLocn)
        {
            // Paint edges
            DroneLineSeen(bottomLeftLocn, bottomRightLocn);
            DroneLineSeen(topLeftLocn, topRightLocn);
            DroneLineSeen(bottomLeftLocn, topLeftLocn);
            DroneLineSeen(bottomRightLocn, topRightLocn);

            // Paint diagonals
            DroneLineSeen(bottomLeftLocn, topRightLocn);
            DroneLineSeen(topLeftLocn, bottomRightLocn);


            // Paint the core (unrotated) rectangle contained within the four corners
            List<float> northList = new List<float> {
                topLeftLocn.NorthingM,
                topRightLocn.NorthingM,
                bottomRightLocn.NorthingM,
                bottomLeftLocn.NorthingM};
            northList.Sort(); // Sort in ascending order

            List<float> eastList = new List<float> {
                topLeftLocn.EastingM,
                topRightLocn.EastingM,
                bottomRightLocn.EastingM,
                bottomLeftLocn.EastingM };
            eastList.Sort(); // Sort in ascending order

            for (float north = northList[1]; north <= northList[2]; north++)
                for (float east = eastList[1]; east <= eastList[2]; east++)
                {
                    var locn = new RelativeLocation(north, east);
                    DronePointSeen(locn);
                }
        }
    }
}
