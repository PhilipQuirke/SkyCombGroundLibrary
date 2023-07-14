using SkyCombGround.CommonSpace;


// GroundSpace only depends on CommonSpace & PersistModel. It does not depend on DroneModel.


// Return ground & surface elevation data from under the drone path.
// Contains some code specific to New Zealand.
// Refer https://github.com/PhilipQuirke/SkyCombAnalystHelp/Ground.md for more background.
namespace SkyCombGround.GroundSpace
{
    // Represents the elevation in meters above sea level, based on a drone flight path encompassing box.
    public class GroundData : Constants
    {
        // The drone video footage extends beyond the flight path locations, so we add a buffer.
        // The DJI_0094 test video has a MaxInputWidthM of 42 meters, so we add 20 meters to all sides.
        public const int GroundBufferM = 20;


        // The location that we want ground data for
        public GlobalLocation? MinGlobalLocation;
        public GlobalLocation? MaxGlobalLocation;

        public GroundGrid? DemGrid { get; set; }
        public GroundGrid? DsmGrid { get; set; }


        public GroundData(List<string>? settings = null)
        {
            MinGlobalLocation = null;
            MaxGlobalLocation = null;
            DemGrid = null;
            DsmGrid = null;

            if (settings != null)
                LoadSettings(settings);
        }


        // Is this flight contained within the specified longitude latitude box?
        public bool ContainedByGlobalLocation(double minLatitude, double minLongitude, double maxLatitude, double maxLongitude)
        {
            if ((MinGlobalLocation == null) || (MaxGlobalLocation == null))
                return false;

            return
                (MaxGlobalLocation.Latitude > minLatitude) &&
                (MinGlobalLocation.Longitude > minLongitude) &&
                (MinGlobalLocation.Latitude < maxLatitude) &&
                (MaxGlobalLocation.Longitude < maxLongitude);
        }


        // Using the area the drone flew over, calculate the ground and surface elevation (in meters above sea level).
        // The Min/MaxLatitude/Longitude values represent a box encompassing the locations the drone flew over.
        // Commonly the drone flight path is NOT a rectangular box with sides aligned North and East,
        // so the Min/MaxLatitude/Longitude box is commonly a larger area than the area the drone flew over.
        // Also the video image from the drones extends sideways from the drone flight path, increasing the ground area of interest further.
        public void GlobalCalculateElevations(
            GlobalLocation minLocation, GlobalLocation maxLocation, string groundDirectory)
        {
            try
            {
                MinGlobalLocation = minLocation;
                MaxGlobalLocation = maxLocation;

                // Is the drone flight in New Zealand?
                if (ContainedByGlobalLocation(-50, 165, -34, 179))
                {
                    // We prefer TIFF over ASC as the TIFF data is faster & smaller

                    // Using TIFF files in subfolders of the groundDirectory folder,
                    // return a list of unsorted DEM and DSM elevations inside the min/max location range.
                    (DemGrid, DsmGrid) = GroundTiffNZ.CalcElevations(this, groundDirectory);
                    if (((DemGrid != null) && DemGrid.Datums.Count > 0) ||
                        ((DsmGrid != null) && DsmGrid.Datums.Count > 0))
                        return;

                    // Using PRJ and ASC files in subfolders of the groundDirectory folder,
                    // return a list of unsorted DEM and DSM elevations inside the min/max location range.
                    (DemGrid, DsmGrid) = GroundAscNZ.CalcElevations(this, groundDirectory);
                    if (((DemGrid != null) && DemGrid.Datums.Count > 0) ||
                        ((DsmGrid != null) && DsmGrid.Datums.Count > 0))
                        return;

                    return;
                }

                // Is the drone flight in the US?
                if (ContainedByGlobalLocation(25, -125, 50, -66))
                {
                    // ToDo: Add USA methods here.

                    return;
                }

                // Is the drone flight in the UK?
                if (ContainedByGlobalLocation(49, -12, 61, 1))
                {
                    // ToDo: Add UK methods here.

                    return;
                }

                // # ExtendGroundSpace: Add more country-specific code here.
            }
            catch (Exception ex)
            {
                throw ThrowException("GroundData.GlobalCalculateElevations", ex);
            }
        }


        // Get the object's settings as datapairs (e.g. for saving to a datastore)
        public DataPairList GetSettings()
        {
            var answer = new DataPairList()
            {
                {"Min Global Location", (MinGlobalLocation == null ? UnknownString : MinGlobalLocation.ToString())},
                {"Max Global Location", (MaxGlobalLocation == null ? UnknownString : MaxGlobalLocation.ToString())},
            };

            if (DemGrid != null)
            {
                answer.Add("Dem Source", DemGrid.Source);
                answer.Add("Dem Min M", (DemGrid.MinLocationM == null ? "" : DemGrid.MinLocationM.ToString()));
                answer.Add("Dem Max M", (DemGrid.MaxLocationM == null ? "" : DemGrid.MaxLocationM.ToString()));
                answer.Add("Dem Datums", DemGrid.Datums.Count);
                answer.Add("Dem Elevation Accuracy M", DemGrid.ElevationAccuracyM, 1);
            }

            if (DsmGrid != null)
            {
                answer.Add("Dsm Source", DsmGrid.Source);
                answer.Add("Dsm Min M", (DsmGrid.MinLocationM == null ? "" : DsmGrid.MinLocationM.ToString()));
                answer.Add("Dsm Max M", (DsmGrid.MaxLocationM == null ? "" : DsmGrid.MaxLocationM.ToString()));
                answer.Add("Dsm Datums", DsmGrid.Datums.Count);
                answer.Add("Dsm Elevation Accuracy M", DsmGrid.ElevationAccuracyM, 1);
            }

            return answer;
        }


        // Load this object's settings from strings (loaded from a datastore)
        // This function must align to the above GetSettings function.
        public void LoadSettings(List<string> settings)
        {
            MinGlobalLocation = new GlobalLocation(settings[0]);
            MaxGlobalLocation = new GlobalLocation(settings[1]);

            if (settings.Count >= 7)
            {
                DemGrid = new(true);
                DemGrid.Source = settings[2];
                DemGrid.MinLocationM = new RelativeLocation(settings[3]);
                DemGrid.MaxLocationM = new RelativeLocation(settings[4]);
                // DemDatums = settings[5];
                DemGrid.ElevationAccuracyM = ConfigBase.StringToFloat(settings[6]);
            }

            if (settings.Count >= 12)
            {
                DsmGrid = new(false);
                DsmGrid.Source = settings[7];
                DsmGrid.MinLocationM = new RelativeLocation(settings[8]);
                DsmGrid.MaxLocationM = new RelativeLocation(settings[9]);
                // DsmDatums = settings[10];
                DsmGrid.ElevationAccuracyM = ConfigBase.StringToFloat(settings[11]);
            }
        }
    }


    public class GroundDataFactory
    {
        public static GroundData Create(List<string>? settings = null)
        {
            return new GroundData(settings);
        }
    }
}
