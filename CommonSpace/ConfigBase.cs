using System;


namespace SkyCombGround.CommonSpace
{
    public class ConfigBase : BaseConstants
    {

        public static string CleanString(string input)
        {
            return input.Trim().ToLower();
        }


        public static int StringToInt(string input)
        {
            try
            {
                if (input == "")
                    return 0;

                if (input == "Unknown")
                    return UnknownValue;

                return Convert.ToInt32(input);
            }
            catch
            {
                return UnknownValue;
            }
        }


        public static int StringToNonNegInt(string input)
        {
            var answer = StringToInt(input);

            if (answer < 0)
                answer = 0;

            return answer;
        }


        public static float StringToFloat(string input)
        {
            try
            {
                if (input == "")
                    return 0;

                if (input == "Unknown")
                    return UnknownValue;

                return float.Parse(input);
            }
            catch
            {
                // Sometimes get #NUM!
                return UnknownValue;
            }
        }


        public static float StringToNonNegFloat(string input)
        {
            float answer = StringToFloat(input);

            if (answer < 0)
                answer = 0;

            return answer;
        }


        public static double StringToDouble(string input)
        {
            try
            {
                if (input == "")
                    return 0;

                if (input == "Unknown")
                    return UnknownValue;

                return double.Parse(input);
            }
            catch
            {
                // Sometimes get #NUM!
                return UnknownValue;
            }
        }


        public static bool StringToBool(string input)
        {
            bool answer = false;

            if (input != "")
            {
                input = input.ToLower().Trim();
                answer = (input == "true") || (input == "yes") || (input == "1");
            }

            return answer;
        }

    };
}