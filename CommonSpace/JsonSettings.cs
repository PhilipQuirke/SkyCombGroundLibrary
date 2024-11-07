using Newtonsoft.Json;
using SkyCombGround.CommonSpace;
using System.Collections.Generic;
using System.Text.Json;


namespace SkyCombGround.CommonSpace
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

        public static JsonSettings GetSettingsJson(string inputDirectory, string groundDirectory
            , string yoloDirectory, string outputDirectory, List<RecentFile> recentList) => new JsonSettings
        {
            InputDirectory = inputDirectory,
            GroundDirectory = groundDirectory,
            YoloDirectory = yoloDirectory,
            OutputDirectory = outputDirectory,
            RecentFiles = recentList
        };


        public static JsonSettings LoadSettings()
        {
            if (File.Exists(SettingsFilePath))
            {
                string json = File.ReadAllText(SettingsFilePath);
                return JsonConvert.DeserializeObject<JsonSettings>(json);
            }

            JsonSettings defaultSettings = GetSettingsJson(
                    "c:\\skycomb\\data_input\\",
                    "c:\\skycomb\\data_ground\\",
                    "c:\\skycomb\\data_yolo\\SkyCombYoloV8.onnx",
                    "c:\\skycomb\\data_output\\",
                    new()
                    );

            JsonSettings.SaveSettings(defaultSettings);

            return defaultSettings;
        }


        public static bool SettingsExist() => File.Exists(SettingsFilePath);


        public static void AddRecentFile(RecentFile newfile)
        {
            JsonSettings currentsettings = JsonSettings.LoadSettings();
            currentsettings.RecentFiles.Add(newfile);
            JsonSettings newsettings = JsonSettings.GetSettingsJson(currentsettings.InputDirectory
                , currentsettings.GroundDirectory, currentsettings.YoloDirectory
                , currentsettings.OutputDirectory, currentsettings.RecentFiles);
            JsonSettings.SaveSettings(newsettings);
        }
    }
}


