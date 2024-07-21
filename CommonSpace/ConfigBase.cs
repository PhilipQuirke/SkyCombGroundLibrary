using System;


namespace SkyCombGround.CommonSpace
{
    public class ConfigBase : BaseConstants
    {
        // Name of directory containing input video and SRT files. Trailing "\" (if any) is trimmed
        public string InputDirectory { get; set; } = "";

        // New Zealand Land Information API key
        public string LinzApiKey { get; set; } = "";

        // Ground path containing static ground contour data. Trailing "\" (if any) is trimmed
        public string GroundDirectory { get; set; } = "";

        // Directory path/file to load YOLOv8 model from. If is a bare directory path, code appends "\yolo_v8_s_e100.onnx"
        // yolo_v8_s_e100.onnx was generated in and exported from Supervisely.
        public string YoloDirectory { get; set; } = "";

        // Directory path to store created (video and spreadsheet) files into. Trailing "\" (if any) is trimmed
        public string OutputDirectory { get; set; } = "";


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