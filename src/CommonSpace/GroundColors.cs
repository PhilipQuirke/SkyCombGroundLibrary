using System.Drawing;


namespace SkyCombGround.CommonSpace
{
    public class GroundColors
    {
        // Ground color is used for the DEM elevation
        public static Color GroundLineColor = Color.Brown;
        public static Color GroundHighColor = Color.FromArgb(224, 210, 197); // Light brown
        public static Color GroundLowColor = Color.FromArgb(62, 40, 36); // Dark brown

        // Surface (aka Tree Top) color is used for the DSM elevation
        public static Color SurfaceLineColor = Color.DarkGreen; // Line on elevation graphs
        public static Color SurfaceHighColor = Color.FromArgb(198, 224, 197); // Light green. 
        public static Color SurfaceLowColor = Color.FromArgb(36, 62, 40); // Dark green. 

        // Swathe color is used for the 'seen' / 'unseen' elevation
        public static Color SwatheHighColor = SurfaceHighColor; // Seen
        public static Color SwatheLowColor = Color.DarkGray; // Unseen

        // Good and bad data cells (based on error ranges and error thresholds) are shown in pale red and pale green
        public static Color BadValueColor = Color.FromArgb(255, 154, 136); // Similar to "tomato"
        public static Color GoodValueColor = Color.FromArgb(198, 224, 197); // Light green


        public static Color MixColors(Color fromColor, float fromFraction, Color toColor)
        {
            var toFraction = 1.0f - fromFraction;

            return Color.FromArgb(
                (int)(fromColor.R * fromFraction + toColor.R * toFraction),
                (int)(fromColor.G * fromFraction + toColor.G * toFraction),
                (int)(fromColor.B * fromFraction + toColor.B * toFraction));
        }
    }
}
