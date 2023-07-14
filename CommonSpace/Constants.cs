using System;


namespace SkyCombGround.CommonSpace
{
    public class Constants : BaseColors
    {
        // Number of decimal places for commonly used types of data
        public const int FpsNdp = 6;
        public const int LatLongNdp = 6;
        public const int ElevationNdp = 1; // Used for DEM and DSM elevations. Lidar is +/- 0.2m
        public const int HeightNdp = 2; // Used for Altitude & Height
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


        public const string DateFormat = "yyyy-MM-dd HH:mm:ss.fff";
        public const string SpanFormat = "c";  // "HH:mm:ss.fff";

        public const float DegreesToRadians = (float)(Math.PI / 180.0);
        public const float RadiansToDegrees = 1 / DegreesToRadians;


        public float RadToDeg(float rad)
        {
            return (rad == UnknownValue ? rad : rad / DegreesToRadians);
        }
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


        // Refer to the leg by a single letter name (instead of a number). Supports up to 52 legs.
        public static string LegIdToName(int legId)
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
                return (int)(name.ToCharArray()[0]) - (int)('A') + 1;

            return (int)(name.ToCharArray()[1]) - (int)('a') + 27;
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
    }
}
