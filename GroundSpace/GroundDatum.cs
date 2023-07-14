using SkyCombGround.CommonSpace;


// GroundSpace only depends on CommonSpace & PersistModel. It does not depend on DroneModel.


namespace SkyCombGround.GroundSpace
{
    public class GroundDatum : Constants
    {
        // Location relative to encompassing DRONE flight path box (in meters). For example [24,13]
        // As ground grid area is greater than drone flight area, includes some relative locations with negative Northing &/or Easting values.
        public RelativeLocation? FlightLocnM { get; set; } = null;
        // The (DEM or DSM) elevation.  
        public double ElevationM { get; set; } = UnknownValue;
        // Was overflown by the drone and was seen in the video
        public bool Seen { get; set; } = false;


        public GroundDatum(float northingM, float eastingM, float elevationM = UnknownValue)
        {
            FlightLocnM = new RelativeLocation(northingM, eastingM);
            ElevationM = elevationM;
        }
        public GroundDatum(List<string> settings)
        {
            LoadSettings(settings);
        }


        // Get the object's settings as datapairs (e.g. for saving to a datastore)
        public DataPairList GetSettings(int roundFactor, bool includeSeen)
        {
            var answer = new DataPairList {
                {"NorthM", FlightLocnM.NorthingM, 1 }, // Spit LocationM to allow graphing in xls
                {"EastM", FlightLocnM.EastingM, 1},  // Spit LocationM to allow graphing in xls
                {"NorthRndM", roundFactor * (int)(FlightLocnM.NorthingM/roundFactor) }, // Round to avoid xls graphing limits
                {"EastRndM", roundFactor * (int)(FlightLocnM.EastingM/roundFactor) },  // Round to avoid xls graphing limits
                {"ElevationM", ElevationM, 1},
            };

            if (includeSeen)
                answer.Add("Seen", Seen);

            return answer;
        }


        // Load this object's settings from strings (loaded from a datastore)
        // This function must align to the above GetSettings function.
        private void LoadSettings(List<string> settings)
        {
            FlightLocnM = new RelativeLocation(settings[0], settings[1]);
            // NorthingRndM settings[2]
            // EastingRndM settings[3]
            ElevationM = ConfigBase.StringToFloat(settings[4]);
            if (settings.Count >= 6)
                Seen = ConfigBase.StringToBool(settings[5]);
        }


        public void AssertGood()
        {
            Assert(FlightLocnM != null, "GroundDatum.AssertGood: No Relative Location");
        }
    }


    public class GroundGrid : Constants
    {
        // List of ground data points
        public List<GroundDatum> Datums { get; private set; }
        // Is this DEM data? Else is DSM data. 
        public bool IsDem { get; }
        // The Northing/Easting coverage of the datums
        public RelativeLocation? MaxLocationM { get; set; }
        public RelativeLocation? MinLocationM { get; set; }
        // Where are we getting this elevation data from?
        public string Source { get; set; } = "";
        // How accurate is this vertical data (in +/- meters)
        // Most available global data sets are +/- 20 meters
        // Local data sets, based on Lidar, can be accurate to +/- 0.2 meters
        public float ElevationAccuracyM { get; set; } = Constants.UnknownValue;


        public GroundGrid(bool isDem)
        {
            Datums = new();
            IsDem = isDem;
        }


        // Has this object obtained some elevation data?
        public bool HasElevationData()
        {
            return Datums.Count > 0 && Datums[0].ElevationM != Constants.UnknownValue;
        }


        // The list must have at least 4 points. Generally it has 100 to 800 points.
        public void AssertGood()
        {
            FailIf(Datums.Count < 4, "GroundDatumList.AssertGood: Not enough ground data points.");
        }


        // Return the minimum & maximum ground elevations (excluding UnknownValue)
        public (double minElevation, double maxElevation) GetMinMaxElevationM()
        {
            double min = double.MaxValue;
            double max = double.MinValue;

            foreach (GroundDatum datum in Datums)
                if (datum.ElevationM != UnknownValue)
                {
                    min = Math.Min(datum.ElevationM, min);
                    max = Math.Max(datum.ElevationM, max);
                }

            return (min, max);
        }


        // Assuming the datums are 1m by 1m squares, calculate the area of the ground seen by the drone
        public int GetGroundSeenM2()
        {
            int datumsSeen = 0;

            foreach (GroundDatum datum in Datums)
                if (datum.Seen)
                    datumsSeen++;

            return datumsSeen;
        }


        // Calculate the rectangular size of datum coverage (includes GroundBufferM space)
        public void CalculateMinMaxLocationM()
        {
            MinLocationM = null;
            MaxLocationM = null;

            if (!HasElevationData())
                return;

            float minNorthingM = Datums[0].FlightLocnM.NorthingM;
            float minEastingM = Datums[0].FlightLocnM.EastingM;
            float maxNorthingM = Datums[0].FlightLocnM.NorthingM;
            float maxEastingM = Datums[0].FlightLocnM.EastingM;

            foreach (var datum in Datums)
            {
                minNorthingM = Math.Min(minNorthingM, datum.FlightLocnM.NorthingM);
                minEastingM = Math.Min(minEastingM, datum.FlightLocnM.EastingM);
                maxNorthingM = Math.Max(maxNorthingM, datum.FlightLocnM.NorthingM);
                maxEastingM = Math.Max(maxEastingM, datum.FlightLocnM.EastingM);
            }

            MinLocationM = new(minNorthingM, minEastingM);
            MaxLocationM = new(maxNorthingM, maxEastingM);
        }


        // For any gaps we assume they are the minimum 
        // This case occurs when say 50% of the flight is over water, and the Lidar data source returns no data for water locations.
        // This water may be the sea, or a lake at a higher altitude, so we use the minimum elevation of that layer.
        public void SetGapsToMinimum()
        {
            (double minElevation, double _) = GetMinMaxElevationM();

            foreach (var datum in Datums)
                if (datum.ElevationM == UnknownValue)
                    datum.ElevationM = minElevation;
        }


        // For a "query" point inside the grid, interpolate the elevation, from the surrounding points.
        // Does not assume that the grid is ordered in any particular way.
        // Assumes that the datum.FlightLocnM points are closely packed,
        // so the difference in distance from queryLocn to each of surrounding points is not important. 
        public virtual float GetElevation(RelativeLocation queryLocn)
        {
            // Just want a small, close-by sample.
            // Assume we have a grid of 1x1m datums.
            // Only consider datums less than 2m away.
            float maxDistM = 2;

            int elevationCount = 0;
            double elevationSumM = 0;
            foreach (var datum in Datums)
                // Avoid use of slow square and squareroot functions 
                if (Math.Abs(datum.FlightLocnM.NorthingM - queryLocn.NorthingM) +
                    Math.Abs(datum.FlightLocnM.EastingM - queryLocn.EastingM) < maxDistM)
                {
                    elevationCount++;
                    elevationSumM += datum.ElevationM;
                }

            if (elevationCount > 0)
                return (float)(elevationSumM / elevationCount);

            return UnknownValue;
        }
    }


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
            if (Grid.MaxLocationM != null)
            {
                MaxNorthingM = (int)Math.Floor(Grid.MaxLocationM.NorthingM);
                MaxEastingM = (int)Math.Floor(Grid.MaxLocationM.EastingM);
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
}
