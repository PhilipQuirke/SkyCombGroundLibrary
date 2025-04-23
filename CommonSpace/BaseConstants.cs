// Copyright SkyComb Limited 2025. All rights reserved. 


namespace SkyCombGround.CommonSpace
{
    public class BaseConstants : GroundColors
    {
        // The current version of the code base.
        public static string CodeVersion = "10.1";


        // Number of decimal places for commonly used types of data
        public const int FpsNdp = 6;
        public const int LatLongNdp = 6;
        public const int ElevationNdp = 1; // Used for Altitude, DEM and DSM elevations. Lidar is +/- 0.2m
        public const int HeightNdp = 2; // Used for Object Height
        public const int AreaM2Ndp = 2;
        public const int AreaCM2Ndp = 0;
        public const int TemperatureNdp = 0;
        public const int LocationNdp = 2;
        public const int PixelVelNdp = 4;
        public const int PixelNdp = 0;
        public const int RadiansNdp = 6;
        public const int DegreesNdp = 2;
        public const int MillisecondsNdp = 0;
        // Needed for millisecond accuracy.
        public const int SecondsNdp = 3;


        public const int UnknownValue = -999;
        public const string UnknownString = "Unknown";
        public const int UnknownSectionId = 999999;
        public const int UnknownStepId = UnknownSectionId;
        public const int UnknownHeight = -2;

        public const float Epsilon = 0.000001f;

        public const string DateFormat = "yyyy-MM-dd HH:mm:ss.fff";
        public const string MediumDateFormat = "yyyy-MM-dd HH:mm:ss";
        public const string ShortDateFormat = "yyyy-MM-dd HH:mm";
        public const string SpanFormat = "c";  // "HH:mm:ss.fff";

        public const float DegreesToRadians = (float)(Math.PI / 180.0);
        public const float RadiansToDegrees = 1 / DegreesToRadians;


        // Fonts
        public const int LargeTitleFontSize = 16;
        public const int MediumTitleFontSize = 14;


        // Titles
        public const string PrefixTitle = "SkyComb Analyst";
        public const string IndexTitle = "Table of Contents";
        public const string AnimalReportTitle = "Animal Report";
        public const string DroneReportTitle = "Drone Report";
        public const string GroundReportTitle = "Ground Report";
        public const string FilesTitle = "File Settings";
        public const string FlightLocationTitle = "Flight Location";
        public const string GroundInputTitle = "Global Location";
        public const string DemInputTitle = "DEM Data";
        public const string DsmInputTitle = "DSM Data";


        // Datastore Tab Names
        public const string HomeTabName = "Home";
        public const string AnimalReportTabName = "Animals"; // Aka Objects
        public const string DroneReportTabName = "Drone";
        public const string GroundReportTabName = "Ground";
        public const string FileSettingsTabName = "FileSettings";
        public const string DroneSettingsTabName = "DroneSettings";
        public const string ProcessSettingsTabName = "ProcessSettings";
        public const string AnimalsDataTabName = "AnimalsData"; // Aka Objects
        public const string AnimalImageDataTabName = "ImageData"; // Aka Features. Aka Animal images
        public const string BlockDataTabName = "BlockData";
        public const string SpanDataTabName = "SpanData";
        public const string LegDataTabName = "LegData";
        public const string StepDataTabName = "StepsData";
        public const string SectionDataTabName = "SectionsData";
        public const string DemDataTabName = "DemData";
        public const string DsmDataTabName = "DsmData";
        public const string SwatheDataTabName = "SwatheData";
        public const string MasterCategoryTabName = "CatsData";
        public const string AnimalCategoryTabName = "AnimalCatData";
        public const string PivotsTabName = "Pivots";
        public const string HelpTabName = "Help";


        // Chart outline sizes
        public const int StandardChartCols = 13;
        public const int StandardChartRows = 15;
        public const int LargeChartRows = 2 * StandardChartRows;
        public const int ChartFullWidthPixels = 1400;


        // Rows
        public const int Chapter1TitleRow = 3;
        public const int Chapter2TitleRow = 21;
        public const int Chapter3TitleRow = 38;
        public const int Chapter4TitleRow = 50;
        public const int IndexContentRow = 3;


        // Columns / Column offset
        public const int LhsColOffset = 1;
        public const int MidColOffset = 4;
        public const int RhsColOffset = 7;
        public const int FarRhsColOffset = 10;
        public const int LabelToValueCellOffset = 1;


        // Ground Grid constants
        protected const int GroundValuesPerCell = 75;
        protected const int GroundScaleFactor = 4; // To preserve 0.25 intervals


        protected const int CountryImageWidth = 300; // fixed size in pixels
        protected const int CountryImageHeight = 200; // fixed size in pixels



        public float DegToRad(float deg)
        {
            return (deg == UnknownValue ? deg : deg * DegreesToRadians);
        }


        public string SafeFloatToStr(float value, string format)
        {
            return (value == UnknownValue ? "0" : value.ToString(format));
        }


        // These two functions form a symmetrical pair
        public static string TimeSpanToString(TimeSpan timeSpan)
        {
            return timeSpan.ToString("mm':'ss':'fff");
        }
        public static TimeSpan StringToTimeSpan(string timeSpanAsString)
        {
            return new TimeSpan(0, 0,
                ConfigBase.StringToNonNegInt(timeSpanAsString.Substring(0, 2)),
                ConfigBase.StringToNonNegInt(timeSpanAsString.Substring(3, 2)),
                ConfigBase.StringToNonNegInt(timeSpanAsString.Substring(6, 3)));
        }


        // Refer to the leg by a single letter name (instead of a number). Supports up to 52 objects.
        public static string IdToLetter(int legId)
        {
            string answer = "";

            if (legId <= 0)
                answer = "?";
            else if (legId <= 26)
                answer += (char)('A' - 1 + legId);
            else
                answer += (char)('a' - 1 + legId - 26);

            return answer;
        }


        // Copes with "C", "C5", "e", "e7", etc. Fails on "#23", "?" and "".
        public static int LegNameToId(string name)
        {
            if ((name == "?") || (name == ""))
                return UnknownValue;

            char letter = name.ToCharArray()[0];
            if (letter == '#')
                return UnknownValue;

            if ((letter >= 'A') && (letter <= 'Z'))
                return name.ToCharArray()[0] - 'A' + 1;

            return name.ToCharArray()[1] - 'a' + 27;
        }


        // Assert an assumption & throw an exception if it` is not true
        public static void Assert(bool assertion, string reasonBad)
        {
            if (!assertion)
                // Set breakpoint here to aid debugging
                throw new Exception(reasonBad);
        }
        public static void FailIf(bool assertion, string reasonBad)
        {
            Assert(!assertion, reasonBad);
        }


        // Used to help debug exceptions
        public static Exception ThrowException(string reason)
        {
            // Set breakpoint here to aid debugging
            return new Exception(reason);
        }
        public static Exception ThrowException(string reason, Exception ex)
        {
            return ThrowException(reason + ": " + ex.Message);
        }


        // Free up memory
        public static void GcFreeMemory()
        {
            // Forces a garbage collection of all generations.
            GC.Collect();
            // Ensure that all finalizers have had a chance to run before the next line of code is executed.
            GC.WaitForPendingFinalizers();
            // Calling GC.Collect again after finalizers have run, collects any objects that were finalized.
            GC.Collect();
        }

    }
}
