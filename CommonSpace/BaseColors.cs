using System.Drawing;


namespace SkyCombGround.CommonSpace
{
    public class BaseColors
    {
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
