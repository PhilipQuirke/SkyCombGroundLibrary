// Copyright SkyComb Limited 2024. All rights reserved. 
using System.Text.Json.Serialization;

// This version of CategoryModels is used for loading off JSON settings and is designed for a record that persists across flights.
// It uses the same structure as SkyCombImage.CategorySpace CategoryModel; here all the classes are suffixed by J.
namespace SkyCombGround.CommonSpace
{
    // An "CategoryModel" represents the information that can be added to an object to categorise it.
    public class CategoryModelJ : ConfigBase
    {
        // What category does the object belong to?
        public string Category { get; set; }

        // Should the object be included in analysis?
        public bool Include { get; set; }

        // Notes about the object
        public string Notes { get; set; }

        public CategoryModelJ() { } //nq

        public CategoryModelJ(string category = "", bool include = true, string notes = "")
        {
            Category = category;
            Include = include;
            Notes = notes;
        }


        // Constructor used when loading objects from the datastore
        public CategoryModelJ(List<string>? settings)
        {
            Category = "";
            Include = true;
            Notes = "";

            if (settings != null)
                LoadSettings(settings);
        }


        // Is the annotation set to the default values?
        public bool IsDefault()
        {
            return Include && Category == "" && Notes == "";
        }


        public bool Equals(string category, bool include, string notes)
        {
            return
                Category == category &&
                Include == include &&
                Notes == notes;
        }


        public bool Equals(CategoryModelJ other)
        {
            return
                this.Category == other.Category &&
                this.Include == other.Include &&
                this.Notes == other.Notes;
        }


        // One-based settings index values. Must align with GetSettings procedure below     
        public const int NameSetting = 1;
        public const int IncludeSetting = 2;
        public const int NotesSetting = 3;


        // Get the class's settings as datapairs (e.g. for saving to the datastore). Must align with above index values.
        public virtual DataPairList GetSettings()
        {
            return new DataPairList
            {
                { "Category",  Category != "" ? Category : UnknownString  },
                { "Include", Include },
                { "Notes",  Notes != "" ? Notes : UnknownString  }
            };
        }


        // Load this object's settings from strings (loaded from a datastore)
        // This function must align to the above GetSettings function.
        public void LoadSettings_Internal(List<string> settings, int offset)
        {
            int i = offset;
            Category = settings[i++];
            Include = StringToBool(settings[i++]);
            Notes = settings[i++];

            if (Category == UnknownString)
                Category = "";
            if (Notes == UnknownString)
                Notes = "";
        }
        public virtual void LoadSettings(List<string> settings)
        {
            LoadSettings_Internal(settings, 0);
        }
    };


    // An "MasterCategoryModel" represents a category that can be assigned to an object to categorise it.
    public class MasterCategoryModelJ : CategoryModelJ
    {
        // Is the animal restricted to the ground? That is, unable to climb or fly.
        public bool Grounded { get; set; }

        // Minimum valid size of this category of object in cm2
        public float MinSizeCM2 { get; set; }
        // Maximum valid size of this category of object in cm2
        public float MaxSizeCM2 { get; set; }

        public MasterCategoryModelJ() { } //nq

        [JsonConstructor]
        public MasterCategoryModelJ(
            string category = "",
            bool include = true,
            string notes = "",
            bool grounded = true,
            float minSizeCM2 = 0.0f,
            float maxSizeCM2 = 0.0f) : base(category, include, notes)
        {
            Grounded = grounded;
            MinSizeCM2 = minSizeCM2;
            MaxSizeCM2 = maxSizeCM2;
        }


        // Constructor used when loading objects from the datastore
        public MasterCategoryModelJ(List<string> settings)
        {
            if (settings != null)
                LoadSettings(settings);
        }


        public bool Equals(MasterCategoryModelJ other)
        {
            return
                base.Equals(other) &&
                this.Grounded == other.Grounded &&
                this.MinSizeCM2 == other.MinSizeCM2 &&
                this.MaxSizeCM2 == other.MaxSizeCM2;

        }


        // One-based settings index values. Must align with GetSettings procedure below     
        public const int GroundedSetting = 4;
        public const int MinSizeCM2Setting = 5;
        public const int MaxSizeCM2Setting = 6;
        //        public const int MinTempSetting = 7;
        //        public const int MaxTempSetting = 8;


        // Get the class's settings as datapairs (e.g. for saving to the datastore). Must align with above index values.
        public override DataPairList GetSettings()
        {
            var answer = base.GetSettings();

            answer.Add("Grounded", Grounded);
            answer.Add("MinSizeCM2", MinSizeCM2, AreaCM2Ndp);
            answer.Add("MaxSizeCM2", MaxSizeCM2, AreaCM2Ndp);

            return answer;
        }


        // Load this object's settings from strings (loaded from a datastore)
        // This function must align to the above GetSettings function.
        public override void LoadSettings(List<string> settings)
        {
            base.LoadSettings_Internal(settings, 0);

            Grounded = StringToBool(settings[GroundedSetting - 1]);
            MinSizeCM2 = StringToNonNegFloat(settings[MinSizeCM2Setting - 1]);
            MaxSizeCM2 = StringToNonNegFloat(settings[MaxSizeCM2Setting - 1]);
        }
    };


    // An "MasterCategoryList" represents all the MasterCategoryModel supported.
    // This is the maximal list of categories that the user picks from when categorising objects.
    public class MasterCategoryListJ : SortedList<string, MasterCategoryModelJ>
    {
        public MasterCategoryListJ() : base() { } //nq

        public void Add(MasterCategoryModelJ category)
        {
            Add(category.Category.ToUpper().Trim(), category);
        }


        // Default NZ values. Areas assume animal seen from above has a ellipsoid shape.
        public void Default()
        {
            Clear();

            Add(new("Mouse", true, "", false, 19, 24));
            // Smallest(standing): House mouse (~8 cm × ~3 cm) = 19 cm²
            // Largest(lying down): Slightly larger house mouse (~10 cm × ~3 cm) = 24 cm²

            Add(new("Rat", true, "", false, 63, 137));
            // Smallest(standing): Ship rat (~16 cm × ~5 cm) = 63 cm²
            // Largest(lying down): Norway rat (~25 cm × ~7 cm) = 137 cm²

            Add(new("Rabbit", true, "", true, 157, 1885));
            // Smallest(standing): Netherland Dwarf (~20 cm × ~10 cm) = 157 cm²
            // Largest(lying down): Flemish Giant (~80 cm × ~30 cm) = 1,885 cm²

            Add(new("Cat", true, "", false, 541, 942));
            // Smallest(standing): Small domestic cat (~46 cm × ~15 cm) = 541 cm²
            // Largest(lying down): Large domestic cat (~60 cm × ~20 cm) = 942 cm²

            Add(new("Possum", true, "", false, 377, 911));
            // Smallest(standing): Common brushtail possum (~32 cm × ~15 cm) = 377 cm²
            // Largest(lying down): Larger brushtail possum (~58 cm × ~20 cm) = 911 cm²

            Add(new("Bird", true, "", false, 39, 3162));
            // Smallest(standing): Fantail (~10 cm × ~5 cm) = 39 cm²
            // Largest(lying down): Royal albatross (~115 cm × ~35 cm) = 3,162 cm²

            Add(new("Wallaby", true, "", true, 785, 1767));
            // Smallest(standing): Dama wallaby (~50 cm × ~20 cm) = 785 cm²
            // Largest(lying down): Bennett's wallaby (~90 cm × ~25 cm) = 1,767 cm²

            Add(new("Dog", true, "", true, 353, 2827));
            // Smallest(standing): Jack Russell Terrier(~30 cm × ~15 cm) = 353 cm²
            // Largest(lying down): German Shepherd(~100 cm × ~36 cm) = 2,827 cm²

            Add(new("Goat", true, "", true, 1767, 4712));
            // Smallest(standing): Arapawa goat(~75 cm × ~30 cm) = 1,767 cm²
            // Largest(lying down): Boer goat(~120 cm × ~50 cm) = 4,712 cm²

            Add(new("Sheep", true, "", true, 3142, 6123));
            // Smallest(standing): Merino sheep(~100 cm × ~40 cm) = 3,142 cm²
            // Largest(lying down): Romney sheep(~130 cm × ~60 cm) = 6,123 cm²

            Add(new("Pig", true, "", true, 3927, 9889));
            // Smallest(standing): Kunekune pig (~100 cm × ~50 cm) = 3,927 cm²
            // Largest(lying down): Large White pig(~180 cm × ~70 cm) = 9,889 cm²

            Add(new("Deer", true, "", true, 3024, 10996));
            // Smallest(standing): Fallow deer (~110 cm × ~35 cm) = 3,024 cm²
            // Largest(lying down): Red deer (~200 cm × ~70 cm) = 10,996 cm²

            Add(new("Cow", true, "", true, 9889, 18850));
            // Smallest(standing): Jersey cow (~180 cm × ~70 cm) = 9,889 cm²
            // Largest(lying down): Holstein Friesian cow (~240 cm × ~100 cm) = 18,850 cm²

            Add(new("Person", false, "", true, 943, 1571));
            // Smallest(standing): Adult of smaller stature (~30 cm × ~40 cm) = 943 cm²
            // Largest(lying down): Taller adult (~40 cm × ~50 cm) = 1,571 cm²

            Add(new("Inanimate", false, "", true));

            Add(new("Stone", false, "", true));

            Add(new("Water", false, "", true));
        }


        public void MaybeDefault()
        {
            if (Count == 0)
                Default();
        }


        // Describe (summarise) the categories.
        public string Describe()
        {
            var answer = "";

            if (Count > 0)
            {
                answer += Count.ToString() + " Categories: ";

                int samples = 0;
                foreach (var category in Values)
                {
                    answer += category.Category;

                    samples++;
                    if (samples >= 5)
                        break;

                    answer += ", ";
                }

                if (Count > samples)
                    answer += " ...";
            }

            return answer;
        }
    };


    public class GroundCategoryFactory
    {
        public static MasterCategoryModelJ NewMasterCategoryModel(List<string> settings)
        {
            return new MasterCategoryModelJ(settings);
        }

        public static MasterCategoryModelJ NewMasterCategoryModel(
                    string category = "",
                    bool include = true,
                    string notes = "",
                    bool grounded = true,
                    float minSizeCM2 = 0.0f,
                    float maxSizeCM2 = 0.0f)
        {
            return new MasterCategoryModelJ(category, include, notes, grounded, minSizeCM2, maxSizeCM2);
        }
    }
}
