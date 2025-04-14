using Newtonsoft.Json;



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
            , string yoloDirectory, string outputDirectory, List<RecentFile> recentList
            , MasterCategoryListJ categoryList) => new JsonSettings
        {
            InputDirectory = inputDirectory,
            GroundDirectory = groundDirectory,
            YoloDirectory = yoloDirectory,
            OutputDirectory = outputDirectory,
            RecentFiles = recentList,
            CategoryList = categoryList
        };

        public static JsonSettings LoadSettings()
        {
 
        if (File.Exists(SettingsFilePath))
            {
                string json = File.ReadAllText(SettingsFilePath);
                return JsonConvert.DeserializeObject<JsonSettings>(json);
            }
            MasterCategoryListJ defaultCategoryList = [];
            defaultCategoryList.Default();
            ;
            //          MasterCategoryListJ defaultCategoryList = [];
            //            defaultCategoryList.Default();
            JsonSettings defaultSettings = GetSettingsJson(
                    "c:\\skycomb\\data_input\\",
                    "c:\\skycomb\\data_ground\\",
                    "c:\\skycomb\\data_yolo\\SkyCombYoloV8.onnx",
                    "c:\\skycomb\\data_output\\",
                    new(),
                    defaultCategoryList
                    );

            JsonSettings.SaveSettings(defaultSettings);

            return defaultSettings;
        }


        public static bool SettingsExist() => File.Exists(SettingsFilePath);


        public static List<RecentFile> AddRecentFile(string newfilename, int numObjects)
        {
            string newname = Path.GetFileName(newfilename);
            string newpath = Path.GetDirectoryName(newfilename);

            if(newname=="")
                // This is a folder of images. Set newname to the last folder name in newpath
                newname = newpath.Substring(newpath.LastIndexOf("\\") + 1);

            JsonSettings currentsettings = LoadSettings();

            // Pick up old numobjects if incoming is zero.
            int num = 0;
            int index = currentsettings.RecentFiles.FindIndex(p => (p.Name != null && p.Name.Contains(newname, StringComparison.OrdinalIgnoreCase)) &&
                        (p.Path != null && p.Path.Contains(newpath, StringComparison.OrdinalIgnoreCase)));
            if (index != -1)
            {
                num = currentsettings.RecentFiles[index].NumObjects;
                currentsettings.RecentFiles.RemoveAt(index);
            }
            RecentFile thisfile = new RecentFile
            {
                Name = newname,
                Path = newpath,
                Description = "Last read: " + DateTime.Now,
                NumObjects = (numObjects == 0 ? num : numObjects)
            };

            currentsettings.RecentFiles.Add(thisfile);

            JsonSettings newsettings = GetSettingsJson(currentsettings.InputDirectory
                , currentsettings.GroundDirectory, currentsettings.YoloDirectory
                , currentsettings.OutputDirectory, currentsettings.RecentFiles, currentsettings.CategoryList);

            SaveSettings(newsettings);

            return currentsettings.RecentFiles;
        }
    }
}


