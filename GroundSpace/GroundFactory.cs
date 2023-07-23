using SkyCombGround.CommonSpace;


// Return ground & surface elevation data from under the drone path.
// Contains some code specific to New Zealand.
// Refer https://github.com/PhilipQuirke/SkyCombAnalystHelp/Ground.md for more background.
namespace SkyCombGround.GroundSpace
{
    // Represents the elevation in meters above sea level, based on a drone flight path encompassing box.
    public class GroundData : BaseConstants
    {
        // The location that we want ground data for
        public GlobalLocation? MinGlobalLocation;
        public GlobalLocation? MaxGlobalLocation;

        public GroundGrid? DemGrid { get; set; }
        public GroundGrid? DsmGrid { get; set; }
        public GroundSeen? SeenGrid { get; set; }

        public GroundData(List<string>? settings = null)
        {
            MinGlobalLocation = null;
            MaxGlobalLocation = null;
            DemGrid = null;
            DsmGrid = null;
            SeenGrid = null;

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
                    if (((DemGrid != null) && DemGrid.NumDatums > 0) ||
                        ((DsmGrid != null) && DsmGrid.NumDatums > 0))
                        return;

                    // Using PRJ and ASC files in subfolders of the groundDirectory folder,
                    // return a list of unsorted DEM and DSM elevations inside the min/max location range.
                    (DemGrid, DsmGrid) = GroundAscNZ.CalcElevations(this, groundDirectory);
                    if (((DemGrid != null) && DemGrid.NumDatums > 0) ||
                        ((DsmGrid != null) && DsmGrid.NumDatums > 0))
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
                DemGrid.GetSettings("Dem", ref answer);

            if (DsmGrid != null)
                DsmGrid.GetSettings("Dsm", ref answer);

            return answer;
        }


        // Load this object's settings from strings (loaded from a datastore)
        // This function must align to the above GetSettings function.
        public void LoadSettings(List<string> settings)
        {
            MinGlobalLocation = new GlobalLocation(settings[0]);
            MaxGlobalLocation = new GlobalLocation(settings[1]);

            if (settings.Count >= 14)
                DemGrid = new(true, settings, 2);

            if (settings.Count >= 26)
                DsmGrid = new(false, settings, 14);
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
