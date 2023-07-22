using SkyCombGround.CommonSpace;
using System.Drawing;


namespace SkyCombGround.GroundSpace
{
    public class GroundGrid : BaseConstants
    {
        // The drone video footage extends beyond the flight path locations, so we add a buffer.
        // The DJI_0094 test video has a MaxInputWidthM of 42 meters, so we add 20 meters to all sides.
        public const int GroundBufferM = 20;

        // The 27.5 minute video DJI_20230630201405_0001_T.mp4
        // covers 840 m x 974 m = 817,560 m2.
        // Plus buffer gives so 880 m x 1014 m = 892,320 m2.
        // That how many datums we need to store.
        const int MaxDatums = 1000000;

        // The DEM & DSM data is available in a grid of 1 m x 1 m cells,
        // with heights in 0.25m increments.
        const float VerticalUnitM = 0.25f;


        // Is this DEM data? Else is DSM data. 
        public bool IsDem { get; }

        // DEM/DSM range from ~0 to 3,754 m (NZ's highest mountain).
        // In C#, short ints (signed 16-bit integer) can store values from -32,768 to 32,767
        // So a short int can store the DEM/DSM height values / VerticalUnitM.
        // We store the elevations in a Northing by Easting (2D array) in VerticalUnitM values
        private short[] ElevationQuarterM { get; }
        // Number of elevations values stored into the elevation array
        public int NumElevationsStored { get; set; }

        // Min/Max elevation
        public short MaxElevationQuarterM { get; set; }
        public short MinElevationQuarterM { get; set; }

        // The Northing/Easting coverage of the datums (including BufferM on each side)
        // in the local coordinate system. (Not drone zero-based coordinates.)
        public int MaxCountryNorthingM { get; } // e.g. 5786068
        public int MinCountryNorthingM { get; } // e.g. 5786000
        public int MaxCountryEastingM { get; } // e.g. 1954744
        public int MinCountryEastingM { get; } // e.g. 1954000


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
        public int NumDatums
        {
            get
            {
                return NumRows * NumCols;
            }
        }


        // Percentage of datums we found a valid elevation for
        public float PercentDatumElevationsAvailable
        {
            get
            {
                return 100.0f * NumElevationsStored / NumDatums;
            }
        }


        // Representation of the ground area in country coordinates
        public RectangleF TargetCountryAreaM()
        {
            return new RectangleF(
                (float)MinCountryEastingM,
                (float)MinCountryNorthingM,
                NumCols,
                NumRows);
        }


        // Partial constructor initialisation of the GroundGrid
        private void Initialise()
        {
            Assert(MinCountryNorthingM < MaxCountryNorthingM, "GroundGrid: NorthingM ordering");
            Assert(MinCountryEastingM < MaxCountryEastingM, "GroundGrid: EastingM ordering");

            Assert(MinCountryNorthingM > 5700000, "GroundGrid: NorthingM coord bad");
            Assert(MinCountryEastingM > 1700000, "GroundGrid: EastingM coord bad");

            for (int i = 0; i < NumDatums; i++)
                ElevationQuarterM[i] = UnknownValue;

            NumElevationsStored = 0;
            MaxElevationQuarterM = UnknownValue;
            MinElevationQuarterM = UnknownValue;

            ElevationAccuracyM = UnknownValue;
        }


        public GroundGrid(bool isDem, RelativeLocation minCountryLocnM, RelativeLocation maxCountryLocnM)
        {
            IsDem = isDem;
            Source = "";

            Assert(minCountryLocnM != null, "GroundGrid: minCountryLocnM missing");
            Assert(maxCountryLocnM != null, "GroundGrid: maxCountryLocnM missing");

            MinCountryNorthingM = (int)(minCountryLocnM.NorthingM - GroundBufferM);
            MinCountryEastingM = (int)(minCountryLocnM.EastingM - GroundBufferM);
            MaxCountryNorthingM = (int)(maxCountryLocnM.NorthingM + GroundBufferM);
            MaxCountryEastingM = (int)(maxCountryLocnM.EastingM + GroundBufferM);

            ElevationQuarterM = new short[NumDatums];

            Initialise();
        }


        public GroundGrid(bool isDem, List<string> settings, int offset)
        {
            IsDem = isDem;
            Source = "";

            Assert(settings != null, "GroundGrid: settings missing");

            MinCountryEastingM = ConfigBase.StringToInt(settings[offset + 1]);
            MinCountryNorthingM = ConfigBase.StringToInt(settings[offset + 2]);
            MaxCountryEastingM = ConfigBase.StringToInt(settings[offset + 3]);
            MaxCountryNorthingM = ConfigBase.StringToInt(settings[offset + 4]);

            ElevationQuarterM = new short[NumDatums];

            Initialise();
            LoadSettings(settings, offset);
        }


        // Has this object obtained some elevation data?
        public bool HasElevationData()
        {
            return NumElevationsStored > 0;
        }


        // The list must have at least 4 points. Generally it has 100s to 1M points.
        public void AssertGood()
        {
            FailIf(NumElevationsStored < 4, "GroundDatumList.AssertGood: Not enough ground data points.");
        }


        public void AssertGoodIndex(string usecase, int index)
        {
            Assert(index >= 0, "GroundGrid." + usecase + ": answer low");
            Assert(index < NumDatums, "GroundGrid." + usecase + ": answer high");
        }


        // Convert from drone location (e.g. [14,3] ) to a grid index. 
        private int DroneLocnToGridIndex(RelativeLocation droneLocnM, bool strict = true)
        {
            int index =
                ((int)(droneLocnM.NorthingM) + GroundBufferM) * NumCols +
                ((int)(droneLocnM.EastingM) + GroundBufferM);

            if (strict)
                AssertGoodIndex("DroneLocnToGridIndex", index);
            else if ((index < 0) || (index > NumDatums))
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
            return 1.0f * quarterMs * VerticalUnitM;
        }


        // Return the minimum & maximum ground elevations (excluding UnknownValue)
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


        // For a "query" point inside the grid, interpolate the elevation, from the surrounding points.
        // Assumes that the grid is 1m by 1m cells so the horizontal difference in
        // distance from queryLocn to each of surrounding points is not important. 
        public float GetElevationByDroneLocn(RelativeLocation droneLocnM)
        {
            // Because of GroundBufferM, the drone should not be near the edge of the grid.
            // But objects in the area seen by the camera may be near or past the edge of the grid.
            // And a lack of DEM & DSM Lidar data may mean that the grid is not as big as we want.
            int gridIndex = DroneLocnToGridIndex(droneLocnM, false);
            if(gridIndex == UnknownValue)
                return UnknownValue;

            if (ElevationQuarterM[gridIndex] != UnknownValue)
                return GridElevationQuarterMToM(ElevationQuarterM[gridIndex]);

            return UnknownValue;
        }


        // Used with Loading from DataStore, to set the elevation of a grid point.
        // Row and Col are one-based
        public float GetElevationMByGridIndex(int oneRow, int oneCol)
        {
            int gridIndex = (oneRow - 1) * NumCols + (oneCol - 1);

            AssertGoodIndex("GetElevationByGridIndex", gridIndex);

            return GridElevationQuarterMToM(ElevationQuarterM[gridIndex]);
        }


        private void AddDatum(int gridIndex, float elevationM)
        {
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


        public void GetSettings(string prefix, ref DataPairList settings)
        {
            settings.Add(prefix + " Source", Source);
            settings.Add(prefix + " Min Country Easting M", MinCountryEastingM);
            settings.Add(prefix + " Min Country Northing M", MinCountryNorthingM);
            settings.Add(prefix + " Max Country Easting M", MaxCountryEastingM);
            settings.Add(prefix + " Max Country Northing M", MaxCountryNorthingM);
            settings.Add(prefix + " Elevation Accuracy M", ElevationAccuracyM, 1);
            settings.Add(prefix + " Max Elevation Quarter M", MaxElevationQuarterM);
            settings.Add(prefix + " Min Elevation Quarter M", MinElevationQuarterM);
            settings.Add(prefix + " Num Elevations Stored", NumElevationsStored);
            settings.Add(prefix + " # Rows", NumRows);
            settings.Add(prefix + " # Cols", NumCols);
            settings.Add(prefix + " # Datums", NumDatums);
        }


        public void LoadSettings(List<string> settings, int offset)
        {
            Source = settings[offset];
            // minEastingM = ConfigBase.StringToInt(settings[offset + 1]);
            // minNorthingM = ConfigBase.StringToInt(settings[offset + 2]);
            // maxEastingM = ConfigBase.StringToInt(settings[offset + 3]);
            // maxNorthingM = ConfigBase.StringToInt(settings[offset + 4]);
            ElevationAccuracyM = ConfigBase.StringToFloat(settings[offset + 5]);
            MaxElevationQuarterM = (short)ConfigBase.StringToInt(settings[offset + 6]);
            MinElevationQuarterM = (short)ConfigBase.StringToInt(settings[offset + 7]);
            NumElevationsStored = ConfigBase.StringToInt(settings[offset + 8]);
            // NumRows = ConfigBase.StringToInt(settings[offset + 9]);
            // NumCols = ConfigBase.StringToInt(settings[offset + 10]);
            // NumDatums = ConfigBase.StringToInt(settings[offset + 11]);
        }


        /*
        // Class to calculate the portion of Ground Grid that was "seen" by the drone's video during flight.
        public class GroundSeen : Constants
        {
            private GroundGrid Grid;

            private int MinNorthingM = UnknownValue;
            private int MinEastingM = UnknownValue;
            private int MaxNorthingM = UnknownValue;
            private int MaxEastingM = UnknownValue;

            public int NorthingRangeM { get { return MaxNorthingM - MinNorthingM + 1; } }
            public int EastingRangeM { get { return MaxEastingM - MinEastingM + 1; } }

            // We use a 1 meter grid for the area seen calculations
            private int IndexSize { get { return NorthingRangeM * EastingRangeM; } }

            private bool[]? SeenGrid = null;


            public GroundSeen(GroundGrid grid)
            {
                Grid = grid;
                Assert(Grid != null, "GroundSeen: Missing GroundGrid");

                if (Grid.MinLocationM != null)
                {
                    MinNorthingM = (int)Math.Floor(Grid.MinLocationM.NorthingM);
                    MinEastingM = (int)Math.Floor(Grid.MinLocationM.EastingM);
                }
                if (Grid.maxCountryLocnM != null)
                {
                    MaxNorthingM = (int)Math.Floor(Grid.maxCountryLocnM.NorthingM);
                    MaxEastingM = (int)Math.Floor(Grid.maxCountryLocnM.EastingM);
                }

                // Create a grid of booleans to indicate if a grid point has been seen
                SeenGrid = new bool[IndexSize];
                for (int n = 0; n < IndexSize; n++)
                    SeenGrid[n] = false;
            }


            // Convert from Northing/Easting to a 1D index
            private int GetIndex(RelativeLocation theLocation)
            {
                var theNorth = (int)Math.Floor(theLocation.NorthingM);
                var theEast = (int)Math.Floor(theLocation.EastingM);

                if ((theNorth < MinNorthingM) ||
                    (theNorth >= MaxNorthingM) ||
                    (theEast < MinEastingM) ||
                    (theEast >= MaxEastingM))
                    return UnknownValue;

                int index =
                    (theNorth - MinNorthingM) * EastingRangeM +
                    (theEast - MinEastingM);

                if ((index < 0) || (index >= IndexSize))
                    return UnknownValue;

                return index;
            }


            // Set point to seen
            private void SeenPoint(RelativeLocation location)
            {
                var index = GetIndex(location);
                if (index != UnknownValue)
                    SeenGrid[index] = true;
            }


            // Set line to seen
            private void SeenLine(RelativeLocation fromLocation, RelativeLocation toLocation)
            {
                var distance = (float)RelativeLocation.DistanceM(fromLocation, toLocation);
                if (distance > 3)
                {
                    var translationStep = new RelativeLocation(
                        (toLocation.NorthingM - fromLocation.NorthingM) / distance,
                        (toLocation.EastingM - fromLocation.EastingM) / distance);
                    for (int edgeStep = 1; edgeStep < distance; edgeStep++)
                    {
                        var locn = new RelativeLocation(
                            fromLocation.NorthingM + translationStep.NorthingM * edgeStep,
                            fromLocation.EastingM + translationStep.EastingM * edgeStep);
                        SeenPoint(locn);
                    }
                }
            }


            // Given the (rotated) rectangle defined by the 4 corners, set the grid area as seen.
            // Assuming we are painting a sequence of flightsteps, painting the edges and diagonals is sufficient.
            public void SetSeen(RelativeLocation topLeftLocn, RelativeLocation topRightLocn, RelativeLocation bottomRightLocn, RelativeLocation bottomLeftLocn)
            {
                // Paint edges
                SeenLine(bottomLeftLocn, bottomRightLocn);
                SeenLine(topLeftLocn, topRightLocn);
                SeenLine(bottomLeftLocn, topLeftLocn);
                SeenLine(bottomRightLocn, topRightLocn);

                // Paint diagonals
                SeenLine(bottomLeftLocn, topRightLocn);
                SeenLine(topLeftLocn, bottomRightLocn);
            }


            // Update all GroundDatum Seen values
            public void FinaliseSeen()
            {
                foreach (var datum in Grid.Datums)
                {
                    var index = GetIndex(datum.FlightLocnM);
                    if (index != UnknownValue)
                        datum.Seen = SeenGrid[index];
                }
            }
        }
        */
    }
}
