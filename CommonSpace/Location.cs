using System.Drawing;


namespace SkyCombGround.CommonSpace
{
    // A global location based on latitude/Longitude
    // Latitude/Longitude is an asymmetrical global coordinate system
    // Refer https://gisgeography.com/latitude-longitude-coordinates/
    // That is 1 unit Latitude is not the same distance as 1 unit Longitude in meters (apart from at the Equator).
    public class GlobalLocation : BaseConstants
    {
        public static string Format = "0.0000000";


        // Latitude (North South) ranges from -90 to +90 degrees e.g. -36.871811
        public double Latitude { get; set; }
        // Longitude (West East) ranges from -180 to +180 degrees e.g. 174.701012
        public double Longitude { get; set; }


        // If the latitude and longitude are zero this is not a real location
        public bool Specified { get { return Latitude != 0 && Longitude != 0; } }


        public GlobalLocation(double latitude = 0, double longitude = 0)
        {
            Latitude = latitude;
            Longitude = longitude;
        }


        public GlobalLocation(GlobalLocation location)
        {
            Latitude = location.Latitude;
            Longitude = location.Longitude;
        }


        // This constructor is the inverse of the ToString function below.
        public GlobalLocation(string locationAsString)
        {
            var stringList = locationAsString.Split(",");
            Latitude = Convert.ToDouble(stringList[0]);
            Longitude = Convert.ToDouble(stringList[1]);
        }


        public override string ToString()
        {
            return Latitude.ToString(Format) + "," + Longitude.ToString(Format);
        }


        public void AssertNZ()
        {
            if (Latitude < -47 || Latitude > -33 || Longitude < 165 || Longitude > 180)
                throw new Exception("Location is not in New Zealand");
        }
    }


    // A relative location, based on some origin, with distances in meters.
    // RelativeLocation is a symmetrical local coordinate system. 1 unit Easting = 1 unit Northing = 1 meter
    public class RelativeLocation
    {
        public static string Format = "0.00";


        // Distance North (South if negative) in meters. Similar to Latitude 
        public float NorthingM { get; set; }

        // Distance East (West if negative) in meters. Similar to Longitude
        public float EastingM { get; set; }

        // Length of the vector in Meters
        public float DiagonalM { get { return (float)Math.Sqrt(Math.Pow(EastingM, 2) + Math.Pow(NorthingM, 2)); } }


        public RelativeLocation(float northingM = 0, float eastingM = 0)
        {
            NorthingM = northingM;
            EastingM = eastingM;
        }


        public RelativeLocation(RelativeLocation location)
        {
            NorthingM = location.NorthingM;
            EastingM = location.EastingM;
        }


        public RelativeLocation(string northingMString, string eastingMString)
        {
            NorthingM = ConfigBase.StringToFloat(northingMString);
            EastingM = ConfigBase.StringToFloat(eastingMString);
        }


        // This constructor mirrors the ToString function below.
        public RelativeLocation(string locationAsString)
        {
            var stringList = locationAsString.Split(",");
            NorthingM = ConfigBase.StringToFloat(stringList[0]);
            EastingM = ConfigBase.StringToFloat(stringList[1]);
        }


        // This function mirrors the "string" constructor above.
        public override string ToString()
        {
            return NorthingM.ToString(Format) + "," + EastingM.ToString(Format);
        }


        // This is used for flight areas to show "302 Northing x 279 Easting"
        public string ToString_Area()
        {
            return NorthingM.ToString("0") + "m Northing x " + EastingM.ToString("0") + "m Easting";
        }

        public virtual RelativeLocation Clone()
        {
            return new RelativeLocation(this);
        }


        // Distance from location1 to location2 in meters.
        public static double DistanceM(RelativeLocation? location1, RelativeLocation? location2)
        {
            if (location1 == null || location2 == null)
                return 0;

            var northingM = location2.NorthingM - location1.NorthingM;
            var eastingM = location2.EastingM - location1.EastingM;

            return (float)Math.Sqrt(Math.Pow(eastingM, 2) + Math.Pow(northingM, 2));
        }



        // Distance from Latitude/Longitude to Latitude/Longitude in meters.
        // Converts from global LOCATION coordinate system Latitude / Longitude 
        // to a local RELATIVE-DISTANCE coordinate system Easting (aka X axis) / Northing (aka Y axis) 
        public static DroneLocation DistanceM(GlobalLocation? location1, GlobalLocation? location2)
        {
            if (location1 == null || location2 == null)
                return new DroneLocation();

            if (location1.Latitude == location2.Latitude && location1.Longitude == location2.Longitude)
                return new DroneLocation();

            double latMidDegrees = (location1.Latitude + location2.Latitude) / 2.0;
            double latMidRadians = latMidDegrees * BaseConstants.DegreesToRadians;

            double per_deg_lat = 111132.954 - 559.822 * Math.Cos(2.0 * latMidRadians) + 1.175 * Math.Cos(4.0 * latMidRadians);
            double per_deg_lon = (Math.PI / 180.0) * 6367449.0 * Math.Cos(latMidRadians);

            // Floats have 6 to 9 decimal places of precision, which is plenty for
            // drone flight relative distances measure in meters.
            float northingM = (float)((location2.Latitude - location1.Latitude) * per_deg_lat);
            float eastingM = (float)((location2.Longitude - location1.Longitude) * per_deg_lon);

            var answer = new DroneLocation(northingM, eastingM);
            answer.AssertGood();
            return answer;
        }


        public static RelativeLocation TravelM(RelativeLocation? fromLocation, RelativeLocation? toLocation)
        {
            RelativeLocation answer = new();

            if ((fromLocation != null) && (toLocation != null))
            {
                answer.NorthingM = toLocation.NorthingM - fromLocation.NorthingM;
                answer.EastingM = toLocation.EastingM - fromLocation.EastingM;
            }

            return answer;
        }
    }


    // A location in drone-specific coordinates.
    public class DroneLocation : RelativeLocation
    {
        public DroneLocation(float northingM = 0, float eastingM = 0) : base(northingM, eastingM)
        {
        }


        public DroneLocation(DroneLocation location) : base(location)
        {
        }


        public DroneLocation(string northingMString, string eastingMString) : base(northingMString, eastingMString)
        {
        }


        // This constructor mirrors the ToString function below.
        public DroneLocation(string locationAsString) : base(locationAsString)
        {
        }


        public DroneLocation(PointF location)
        {
            NorthingM = location.Y;
            EastingM = location.X;
        }


        public override DroneLocation Clone()
        {
            return new DroneLocation(this);
        }


        // Return a new DroneLocation equal to the sume of this and the specified vector
        public DroneLocation Add(VelocityF delta, float factor = 1)
        {
            return new DroneLocation(
                this.NorthingM + delta.Value.Y * factor,
                this.EastingM + delta.Value.X * factor);
        }


        // Return copy of this vector translated by the specified distance
        public DroneLocation Translate(DroneLocation? distance, float factor = 1)
        {
            if (distance == null)
                return this.Clone();

            return new DroneLocation(
                this.NorthingM + distance.NorthingM * factor,
                this.EastingM + distance.EastingM * factor);
        }


        // Return a new DroneLocation equal to the sum of this and the other DroneLocation
        public DroneLocation Add(DroneLocation other)
        {
            return Translate(other, +1);
        }


        // Return a new DroneLocation equal to the difference of this and the other DroneLocation
        public DroneLocation Subtract(DroneLocation other)
        {
            return Translate(other, -1);
        }


        // Return a new DroneLocation equal to this location scaled by factor
        public DroneLocation Multiply(float factor)
        {
            return new DroneLocation(
                this.NorthingM * factor,
                this.EastingM * factor);
        }


        // Return a new DroneLocation equal to the negated copy of this vector
        public DroneLocation Negate()
        {
            return Multiply(-1);
        }


        // Return a new DroneLocation equal to a unit vector of this location
        public DroneLocation UnitVector()
        {
            var diagonalM = DiagonalM;
            return Multiply(diagonalM == 0 ? 0 : 1 / diagonalM);
        }


        /// <summary>
        /// Rotates one point around another
        /// </summary>
        /// <param name="pointToRotate">The point to rotate.</param>
        /// <param name="centerPoint">The center point of rotation.</param>
        /// <param name="angleInDegrees">The rotation angle in degrees.</param>
        /// <returns>Rotated point</returns>
        public static PointF RotatePoint(PointF pointToRotate, PointF centerPoint, double angleInRadians)
        {
            double cosTheta = Math.Cos(angleInRadians);
            double sinTheta = Math.Sin(angleInRadians);
            return new PointF
            {
                X =
                    (float)
                    (cosTheta * (pointToRotate.X - centerPoint.X) -
                    sinTheta * (pointToRotate.Y - centerPoint.Y) + centerPoint.X),
                Y =
                    (float)
                    (sinTheta * (pointToRotate.X - centerPoint.X) +
                    cosTheta * (pointToRotate.Y - centerPoint.Y) + centerPoint.Y)
            };
        }
        public static PointF RotatePoint(PointF pointToRotate, double angleInRadians)
        {
            return RotatePoint(pointToRotate, new(0, 0), angleInRadians);
        }


        // Assert that this location is in drone coordinate system & reasonable
        public void AssertGood()
        {
            if (EastingM < -99000 || EastingM > 99000 || NorthingM < -99000 || NorthingM > 99000)
                throw new Exception("Drone location is 99km from drone origin");
        }
    }


    // A location in country-specific coordinates.
    public class CountryLocation : RelativeLocation
    {
        public CountryLocation(float northingM = 0, float eastingM = 0) : base(northingM, eastingM)
        {
        }


        public CountryLocation(CountryLocation location) : base(location)
        {
        }


        public CountryLocation(string northingMString, string eastingMString) : base(northingMString, eastingMString)
        {
        }


        // This constructor mirrors the ToString function below.
        public CountryLocation(string locationAsString) : base(locationAsString)
        {
        }


        public override CountryLocation Clone()
        {
            return new CountryLocation(this);
        }


        // Assert that this location is in country-specific coordinates.
        // In NZ, NorthingM / EastingM should be greater than 1 million.
        public void AssertGood()
        {
            if (EastingM < 100000 || EastingM > 3000000 || NorthingM < 1000000 || NorthingM > 9000000)
                throw new Exception("Location is not in New Zealand");
        }
    }
}