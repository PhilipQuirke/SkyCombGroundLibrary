using Newtonsoft.Json;
using SkyCombGround.CommonSpace;
using System.Collections.Generic;
using System.ComponentModel;


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


        public static List<RecentFile> AddRecentFile(string newfilename, int numObjects)
        {

            string newname = Path.GetFileName(newfilename);
            string newpath = Path.GetDirectoryName(newfilename);
            JsonSettings currentsettings = JsonSettings.LoadSettings();

            // remove it if the file already exists in RecentFiles
            currentsettings.RecentFiles.RemoveAll(p => (p.Name != null && p.Name.Contains(newname, StringComparison.OrdinalIgnoreCase)) &&
                            (p.Path != null && p.Path.Contains(newpath, StringComparison.OrdinalIgnoreCase)));

            RecentFile thisfile = new RecentFile
            {
                Name = newname,
                Path = newpath,
                Description = "Last read: " + DateTime.Now,
                NumObjects = numObjects
            };

            currentsettings.RecentFiles.Add(thisfile);

            JsonSettings newsettings = JsonSettings.GetSettingsJson(currentsettings.InputDirectory
                , currentsettings.GroundDirectory, currentsettings.YoloDirectory
                , currentsettings.OutputDirectory, currentsettings.RecentFiles);
            JsonSettings.SaveSettings(newsettings);
            return currentsettings.RecentFiles;
        }
    }
}


