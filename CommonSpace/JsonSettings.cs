using Newtonsoft.Json;
using SkyCombGround.CommonSpace;


namespace SkyCombGroundLibrary.CommonSpace
{
    public class JsonSettings : ConfigBase
    {
        private const string SettingsFileName = "skycomb_settings.json";
        private static readonly string SettingsFilePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, SettingsFileName);


        public static void SaveSettings(JsonSettings settings)
        {
            string json = JsonConvert.SerializeObject(settings, Newtonsoft.Json.Formatting.Indented);
            File.WriteAllText(SettingsFilePath, json);
        }


        public static JsonSettings LoadSettings()
        {
            if (File.Exists(SettingsFilePath))
            {
                string json = File.ReadAllText(SettingsFilePath);
                return JsonConvert.DeserializeObject<JsonSettings>(json);
            }

            JsonSettings defaultSettings = new()
                {
                    InputDirectory = "D:\\SkyComb\\Data_Input\\",
                    LinzApiKey = "66f3193296d44904880f5be1fa9fac44",
                    GroundDirectory = "D:\\SkyComb\\Data_Ground\\",
                    YoloDirectory = "D:\\SkyComb\\Data_Yolo\\yolo_v8_s_e100.onnx",
                    OutputDirectory = "D:\\SkyComb\\Data_Output\\",
                };

            JsonSettings.SaveSettings(defaultSettings);

            return defaultSettings;
        }


        public static bool SettingsExist()
        {
            return File.Exists(SettingsFilePath);
        }
    }
}
