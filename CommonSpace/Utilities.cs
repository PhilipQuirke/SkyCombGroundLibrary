using System.Drawing;


namespace SkyCombGround.CommonSpace
{
    // AreaF is a 2D area.  
    public class AreaF
    {
        public PointF Value;


        public AreaF(PointF value)
        {
            Value = new PointF(value.X, value.Y);
        }
        public AreaF(float X = 0, float Y = 0)
        {
            Value = new PointF(X, Y);
        }
        public AreaF(string pointAsString)
        {
            if (pointAsString == "")
                Value = new PointF(0, 0);
            else
            {
                var settings = pointAsString.Split(',');
                Value = new PointF(ConfigBase.StringToFloat(settings[0]), ConfigBase.StringToFloat(settings[1]));
            }
        }


        public float AreaM2()
        {
            return Value.X * Value.Y;
        }


        // Convert to string ready for saving to DataStore. Is partner function to the above constructor.
        public string ToString(int Ndp)
        {
            var xStr = (Value.X == 0 ? "0" : Value.X.ToString("F" + Ndp));
            var yStr = (Value.Y == 0 ? "0" : Value.Y.ToString("F" + Ndp));
            return xStr + "," + yStr;
        }
    }


    // Velocity is a 2D vector 
    public class VelocityF : AreaF
    {
        public VelocityF(VelocityF value) : base(value.Value)
        {
        }
        public VelocityF(float X = 0, float Y = 0) : base(X, Y)
        {
        }
        public VelocityF(string pointAsString) : base(pointAsString)
        {
        }


        // Absolute speed of this vector.
        public double Speed()
        {
            return Math.Sqrt(Math.Pow(Value.X, 2) + Math.Pow(Value.Y, 2));
        }


        // Return a copy of this vector modified to be one unit long
        public VelocityF GetUnitVector()
        {
            var length = Speed();
            if (length == 0)
                return new();

            return new VelocityF((float)(Value.X / length), (float)(Value.Y / length));
        }


        // Return a copy of this vector that is at right angle to the original and of same length
        public VelocityF GetPerpendicularVector()
        {
            return new VelocityF(-Value.Y, Value.X);
        }


        // Return a copy of this vector scaled up or down
        public VelocityF Scale(float scale)
        {
            return new VelocityF(Value.X * scale, Value.Y * scale);
        }


        // Return a copy of this vector 
        public VelocityF Clone()
        {
            return new VelocityF(this);
        }
    }


    // Class to transform from one coordinate system to another
    // e.g. from thermal video coordinates to optical video coordinates
    public class Transform
    {
        // Scale from one coordinate system to another.
        // e.g. Optical video resolution is greater than thermal video resolution e.g. 1 or 3
        public float Scale;

        // This is the horizontal distance to translate the X coordinate.
        // e.g. Optical video can cover a wider field of vision than thermal video. This is the left edge translation.
        public float XMargin;

        // This is the vertical distance to translate the Y coordinate.
        // e.g. Optical video can cover a wider field of vision than thermal video. This is the top edge translation.
        public float YMargin;


        // Constructor. Default params are a "no change" transform
        public Transform(float scale = 1, float xMargin = 0, float yMargin = 0)
        {
            Scale = scale;
            XMargin = xMargin;
            YMargin = yMargin;
        }


        public int CalcX(int x) { return (int)(XMargin + x * Scale); }
        public int CalcY(int y) { return (int)(YMargin + y * Scale); }


        public Rectangle CalcRect(Rectangle Rect)
        {
            return new Rectangle(
                CalcX(Rect.X),
                CalcY(Rect.Y),
                (int)(Rect.Width * Scale),
                (int)(Rect.Height * Scale));
        }


        // If the output frame is a different size (also resolution) from the input frame then return a transform.
        // Display video can cover a wider field of vision than input.
        // ExcludeMarginRatio is the (unitless) margin on the display video, not visible in the input video, on all display video edges
        public static Transform ImageToImageTransform(Size inputSize, Size outputSize, float excludeMarginRatio)
        {
            Transform inputToDisplayTransform = new();

            if (inputSize.Width != outputSize.Width || inputSize.Height != outputSize.Height)
            {
                float xMargin = excludeMarginRatio * outputSize.Width;
                float yMargin = excludeMarginRatio * outputSize.Height;
                float scale = 1.0f * (outputSize.Width - 2 * xMargin) / inputSize.Width;
                inputToDisplayTransform = new(scale, xMargin, yMargin);
            }

            return inputToDisplayTransform;
        }


        // Return a point at the center of box
        public static PointF GetBoxCenter(Rectangle box)
        {
            return new PointF(
                box.X + box.Width / 2.0f,
                box.Y + box.Height / 2.0f);
        }


        // Return a rectangle, centred on center of box, with width and height of radius
        public static Rectangle GetInflatedSquareBox(Rectangle box, int diameter)
        {
            var centerBox = GetBoxCenter(box);

            return new Rectangle(
                 (int)(centerBox.X - diameter / 2.0f),
                 (int)(centerBox.Y - diameter / 2.0f),
                 diameter,
                 diameter);
        }
    }


    public class DataPair : BaseConstants
    {
        public string Key;
        public string Value;
        public int Ndp = UnknownValue;

        public DataPair(string key, string value)
        {
            Key = key;
            Value = value;
            Ndp = UnknownValue;
        }
        public DataPair(string key, int value)
        {
            Key = key;
            Value = value.ToString();
            Ndp = 0;
        }

        // Format double as a string to desired ndp
        public DataPair(string key, double value, int ndp)
        {
            Key = key;
            Value = value.ToString("F" + ndp.ToString());
            Ndp = ndp;
        }
    }


    public class DataPairList : List<DataPair>
    {
        public void Add(string key, string value)
        {
            Add(new DataPair(key, value));
        }
        public void Add(string key, int value)
        {
            Add(new DataPair(key, value));
        }
        public void Add(string key, double value, int ndp)
        {
            Add(new DataPair(key, value, ndp));
        }
        public void Add(string key, bool value)
        {
            Add(new DataPair(key, value ? "true" : "false"));
        }

        public void AddRectange(string keyPrefix, Rectangle value)
        {
            Add(keyPrefix + ".X", value.X);
            Add(keyPrefix + ".Y", value.Y);
            Add(keyPrefix + ".Width", value.Width);
            Add(keyPrefix + ".Height", value.Height);
        }
    }

}
