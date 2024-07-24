using Newtonsoft.Json;
using SkyCombGround.CommonSpace;


namespace SkyCombGroundLibrary.CommonSpace
{
    public class JsonSettings : SettingsBase
    {
        private const string SettingsFileName = "skycomb_settings.json";
        private static readonly string SettingsFilePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, SettingsFileName);


        public static void SaveSettings(JsonSettings settings)
        {
            string json = JsonConvert.SerializeObject(settings, Newtonsoft.Json.Formatting.Indented);
            File.WriteAllText(SettingsFilePath, json);
        }

        public static JsonSettings GetSettingsJson( string inputDirectory, string linzApiKey, string groundDirectory, string yoloDirectory, string outputDirectory)
        {
            return new JsonSettings
            {
                InputDirectory = inputDirectory,
                LinzApiKey = linzApiKey,
                GroundDirectory = groundDirectory,
                YoloDirectory = yoloDirectory,
                OutputDirectory = outputDirectory,
            };
        }


        public static JsonSettings LoadSettings()
        {
            if (File.Exists(SettingsFilePath))
            {
                string json = File.ReadAllText(SettingsFilePath);
                return JsonConvert.DeserializeObject<JsonSettings>(json);
            }

            JsonSettings defaultSettings = GetSettingsJson(
                    "d:\\skycomb\\data_input\\",
                    "66f3193296d44904880f5be1fa9fac44",
                    "d:\\skycomb\\data_ground\\",
                    "d:\\skycomb\\data_yolo\\yolo_v8_s_e100.onnx",
                    "d:\\skycomb\\data_output\\" );

            JsonSettings.SaveSettings(defaultSettings);

            return defaultSettings;
        }


        public static bool SettingsExist()
        {
            return File.Exists(SettingsFilePath);
        }
    }
}
