// Copyright SkyComb Limited 2025. All rights reserved. 

using System.Collections.Concurrent;
using System.Globalization;


namespace SkyCombGround.CommonSpace
{
    public class ConfigBase : BaseConstants
    {
        // Cache for frequently used string values to avoid repeated parsing
        private static readonly ConcurrentDictionary<string, int> IntCache = new();
        private static readonly ConcurrentDictionary<string, float> FloatCache = new();
        private static readonly ConcurrentDictionary<string, double> DoubleCache = new();
        private static readonly ConcurrentDictionary<string, bool> BoolCache = new();

        // Pre-computed values for common cases
        private static readonly HashSet<string> TrueValues = new(StringComparer.OrdinalIgnoreCase)
            { "true", "yes", "1" };
        private static readonly HashSet<string> EmptyValues = new(StringComparer.OrdinalIgnoreCase)
            { "", " ", "unknown" };

        public static string CleanString(string input)
        {
            return input.Trim().ToLowerInvariant(); // ToLowerInvariant is faster than ToLower
        }


        // Batch conversion methods for loading multiple values at once
        public static void ConvertStringBatch(ReadOnlySpan<string> inputs, Span<int> outputs)
        {
            for (int i = 0; i < inputs.Length; i++)
            {
                outputs[i] = StringToIntFast(inputs[i]);
            }
        }


        public static void ConvertStringBatch(ReadOnlySpan<string> inputs, Span<float> outputs)
        {
            for (int i = 0; i < inputs.Length; i++)
            {
                outputs[i] = StringToFloatFast(inputs[i]);
            }
        }


        // Optimized conversion methods using spans and reduced allocations
        public static int StringToIntFast(ReadOnlySpan<char> input)
        {
            if (input.IsEmpty) return 0;

            // Check for "Unknown" without creating string
            if (input.Length == 7 && input.SequenceEqual("Unknown".AsSpan()))
                return UnknownValue;

            // Use span-based parsing (no string allocation)
            if (int.TryParse(input, NumberStyles.Integer, CultureInfo.InvariantCulture, out int result))
                return result;

            return UnknownValue;
        }


        public static int StringToIntFast(string input)
        {
            // Check cache first for frequently used values
            if (IntCache.TryGetValue(input, out int cachedResult))
                return cachedResult;

            int result = StringToIntFast(input.AsSpan());

            // Cache the result if it's likely to be reused
            if (input.Length < 10) // Only cache short strings to avoid memory bloat
                IntCache.TryAdd(input, result);

            return result;
        }


        public static float StringToFloatFast(ReadOnlySpan<char> input)
        {
            if (input.IsEmpty) return 0f;

            if (input.Length == 7 && input.SequenceEqual("Unknown".AsSpan()))
                return UnknownValue;

            if (float.TryParse(input, NumberStyles.Float, CultureInfo.InvariantCulture, out float result))
                return result;

            return UnknownValue;
        }


        public static float StringToFloatFast(string input)
        {
            if (FloatCache.TryGetValue(input, out float cachedResult))
                return cachedResult;

            float result = StringToFloatFast(input.AsSpan());

            if (input.Length < 10)
                FloatCache.TryAdd(input, result);

            return result;
        }


        public static double StringToDoubleFast(ReadOnlySpan<char> input)
        {
            if (input.IsEmpty) return 0.0;

            if (input.Length == 7 && input.SequenceEqual("Unknown".AsSpan()))
                return UnknownValue;

            if (double.TryParse(input, NumberStyles.Float, CultureInfo.InvariantCulture, out double result))
                return result;

            return UnknownValue;
        }


        public static double StringToDoubleFast(string input)
        {
            if (DoubleCache.TryGetValue(input, out double cachedResult))
                return cachedResult;

            double result = StringToDoubleFast(input.AsSpan());

            if (input.Length < 10)
                DoubleCache.TryAdd(input, result);

            return result;
        }


        public static bool StringToBoolFast(string input)
        {
            if (BoolCache.TryGetValue(input, out bool cachedResult))
                return cachedResult;

            if (string.IsNullOrEmpty(input))
                return false;

            // Use pre-computed HashSet for O(1) lookup
            bool result = TrueValues.Contains(input.Trim());

            if (input.Length < 10)
                BoolCache.TryAdd(input, result);

            return result;
        }


        // Keep original methods for backwards compatibility
        public static int StringToInt(string input) => StringToIntFast(input);
        public static float StringToFloat(string input) => StringToFloatFast(input);
        public static double StringToDouble(string input) => StringToDoubleFast(input);
        public static bool StringToBool(string input) => StringToBoolFast(input);


        public static int StringToNonNegInt(string input)
        {
            var answer = StringToIntFast(input);
            return answer < 0 ? 0 : answer;
        }


        public static int StringToInt_BlankIsUnknown(string input)
        {
            if (EmptyValues.Contains(input))
                return UnknownValue;

            return StringToIntFast(input);
        }


        public static float StringToNonNegFloat(string input)
        {
            float answer = StringToFloatFast(input);
            return answer < 0 ? 0 : answer;
        }


        // Method to clear caches if memory usage becomes a concern
        public static void ClearCaches()
        {
            IntCache.Clear();
            FloatCache.Clear();
            DoubleCache.Clear();
            BoolCache.Clear();
        }


        // Method to get cache statistics for monitoring
        public static (int IntCacheSize, int FloatCacheSize, int DoubleCacheSize, int BoolCacheSize) GetCacheStats()
        {
            return (IntCache.Count, FloatCache.Count, DoubleCache.Count, BoolCache.Count);
        }
    }


    public class RecentFile
    {
        public string Name { get; set; }
        public string Path { get; set; }
        public string Description { get; set; }
        public int NumObjects { get; set; } = 0;
    }


    public class SettingsBase : ConfigBase
    {

        // Name of directory containing input video and SRT files. Trailing "\" (if any) is trimmed
        public string InputDirectory { get; set; } = "";

        // Ground path containing static ground contour data. Trailing "\" (if any) is trimmed
        public string GroundDirectory { get; set; } = "";

        // Directory path/file to load YOLOv8 model from.
        public string YoloDirectory { get; set; } = "";

        // Directory path to store created (video and spreadsheet) files into. Trailing "\" (if any) is trimmed
        public string OutputDirectory { get; set; } = "";

        // List of recent files and related info
        public List<RecentFile> RecentFiles { get; set; } = new();

        // List of object categories, updated by user
        public MasterCategoryListJ CategoryList { get; set; } = new();

    };
}