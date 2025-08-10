// Copyright SkyComb Limited 2024. All rights reserved. 
using SkyCombGround.CommonSpace;
using SkyCombGround.GroundModel;


// Return ground & surface elevation data from under the drone path.
// Contains some code specific to New Zealand.
// Refer https://github.com/PhilipQuirke/SkyCombAnalystHelp/Ground.md for more background.
namespace SkyCombGround.GroundLogic
{
    // Represents ground controur data for a (drone flight path encompassing) box.
    public class GroundData : BaseConstants, IDisposable
    {
        // The location that we want ground data for
        public GlobalLocation? MinGlobalLocation;
        public GlobalLocation? MaxGlobalLocation;


        // The ground surface (tree-top) data
        public GroundModel.GroundModel? DsmModel { get; set; }

        // The ground elevation data
        public GroundModel.GroundModel? DemModel { get; set; }

        // The portion of the encompassing box videoed during the drone flight.
        public GroundModel.SwatheModel? SwatheModel { get; set; }


        public bool HasDsmModel { get { return DsmModel != null; } }
        public bool HasDemModel { get { return DemModel != null; } }
        public bool HasSwatheModel { get { return SwatheModel != null; } }


        public GroundData(
            List<string>? globalSettings,
            List<string>? dsmSettings,
            List<string>? demSettings)
        {
            MinGlobalLocation = null;
            MaxGlobalLocation = null;
            DsmModel = null;
            DemModel = null;
            SwatheModel = null;

            if (globalSettings != null)
                LoadSettings(globalSettings);

            bool haveDsmSettings = (dsmSettings != null) && (dsmSettings.Count > 0);
            bool haveDemSettings = (demSettings != null) && (demSettings.Count > 0);

            if (haveDsmSettings)
                DsmModel = new(false, dsmSettings);

            if (haveDemSettings)
                DemModel = new(true, demSettings);

            // Reuse the DSM or DEM settings as the Swathe settings.
            if (haveDsmSettings)
                SwatheModel = new(dsmSettings);
            else if (haveDemSettings)
                SwatheModel = new(demSettings);
        }


        public void FreeResources()
        {
            DsmModel = null;
            DemModel = null;
            SwatheModel = null;
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

                // Currently only supports New Zealand
                if (ContainedByGlobalLocation(-50, 165, -34, 179))
                {
                    // Using TIFF files in subfolders of the groundDirectory folder,
                    // return a list of unsorted DEM and DSM elevations inside the min/max location range.
                    (DemModel, DsmModel) = GroundTiffNZ.CalcElevations(this, groundDirectory);
                    if (((DemModel != null) && DemModel.NumDatums > 0) ||
                        ((DsmModel != null) && DsmModel.NumDatums > 0))
                        return;

                    return;
                }

                // Location is not supported
                throw new ArgumentException("Location is outside New Zealand bounds. This library currently only supports New Zealand elevation data.");
            }
            catch (Exception ex)
            {
                FreeResources();
                throw ThrowException("GroundData.GlobalCalculateElevations", ex);
            }
        }


        // Get the object's settings as datapairs (e.g. for saving to a datastore)
        public DataPairList GetSettings()
        {
            return new DataPairList{
                {"Min Global Location", (MinGlobalLocation == null ? UnknownString : MinGlobalLocation.ToString())},
                {"Max Global Location", (MaxGlobalLocation == null ? UnknownString : MaxGlobalLocation.ToString())},
            };
        }


        public DataPairList? GetDemSettings()
        {
            if (DemModel != null)
                return DemModel.GetSettings();
            return null;
        }


        public DataPairList? GetDsmSettings()
        {
            if (DsmModel != null)
                return DsmModel.GetSettings();
            return null;
        }


        // Load this object's settings from strings (loaded from a datastore)
        // This function must align to the above GetSettings function.
        public void LoadSettings(List<string> globalSettings)
        {
            MinGlobalLocation = new GlobalLocation(globalSettings[0]);
            MaxGlobalLocation = new GlobalLocation(globalSettings[1]);
        }


        public GroundModel.GroundModel? GroundModelByType(GroundType groundType)
        {
            switch (groundType)
            {
                case GroundType.DsmElevations:
                    return DsmModel;
                case GroundType.DemElevations:
                    return DemModel;
                case GroundType.SwatheSeen:
                    return SwatheModel;
            }
            return null;
        }


        private bool disposed = false;


        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }


        protected virtual void Dispose(bool disposing)
        {
            if (!disposed)
            {
                if (disposing)
                {
                    // Dispose managed resources
                }

                // Dispose unmanaged resources

                disposed = true;
            }
        }
    }


    public class GroundDataFactory
    {
        public static GroundData Create(
            List<string>? globalSettings = null,
            List<string>? dsmSettings = null,
            List<string>? demSettings = null)
        {
            return new GroundData(globalSettings, dsmSettings, demSettings);
        }
    }
}
