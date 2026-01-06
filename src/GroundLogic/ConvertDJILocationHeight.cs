// Copyright SkyComb Limited 2026. All rights reserved.
using OSGeo.GDAL;
using OSGeo.OSR;
using SkyCombGround.CommonSpace;


namespace SkyCombGround.GroundLogic
{
    public class ConvertDJILocationHeight
    {
        // From LINZ downloaded "NZ Quasigeoid 2016 Raster" described as: 
        //      Warning: This raster is a grid of a floating-point values; not a surface.To derive an accurate height transformation value,
        //      this raster grid must be downloaded in terms of NZGD2000 and then converted into a surface using bilinear interpolation.
        //      New Zealand Quasigeoid 2016 Raster, provides users with a one arc-minute gridded(approximately 1.8 kilometres) raster image
        //      of the New Zealand Quasigeoid 2016 (NZGeoid2016).
        //      The relationship between the GRS80 ellipsoid and the New Zealand Vertical Datum 2016 (NZVD2016) is modelled by[NZGeoid2016]
        //      and is represented by the attribute “N”, in metres.(data.linz.govt.nz/layer/3418).
        //      NZVD2016 is formally defined in the LINZ standard LINZS25009.
        // Stored it in D:\SkyComb\GroundData\ here
        static string NzQuasiGeoid2016FilePath = "nz-quasigeoid-2016-raster\\new_zealand_quasigeoid_2016_raster.tif";


        // Convert location: WGS84 lat/lon → NZTM2000 (EPSG:2193) (NZ ground Lidar format)
        public static CountryLocation Wgs84ToNztm(GlobalLocation globalLocation)
        {
            try
            {
                globalLocation.AssertNZ();

                var wgs84 = new SpatialReference("");
                wgs84.ImportFromEPSG(4326);
                wgs84.SetAxisMappingStrategy(AxisMappingStrategy.OAMS_TRADITIONAL_GIS_ORDER);

                var nztm = new SpatialReference("");
                nztm.ImportFromEPSG(2193);
                nztm.SetAxisMappingStrategy(AxisMappingStrategy.OAMS_TRADITIONAL_GIS_ORDER);

                using var ct = new CoordinateTransformation(wgs84, nztm);

                double[] xyz = { globalLocation.Longitude, globalLocation.Latitude, 0.0 }; // lon, lat
                ct.TransformPoint(xyz);

                return new CountryLocation((float)xyz[1], (float)xyz[0]); // (N, E)
            }
            catch (Exception ex)
            {
                throw BaseConstants.ThrowException("Wgs84ToNztm", ex);
            }
        }
        // Convert location: NZTM2000 (EPSG:2193) → WGS84 lat/lon
        public static GlobalLocation NztmToWgs84(CountryLocation countryLocation)
        {
            var nztm = new SpatialReference("");
            nztm.ImportFromEPSG(2193); // NZGD2000 / NZTM2000

            var wgs84 = new SpatialReference("");
            wgs84.ImportFromEPSG(4326);

            using var ct = new CoordinateTransformation(nztm, wgs84);

            // OSR expects (x = Easting, y = Northing)
            double[] xyz = { countryLocation.EastingM, countryLocation.NorthingM, 0.0 };
            ct.TransformPoint(xyz);

            // xyz now contains (lon, lat)
            return new GlobalLocation(
                latitude: (float)xyz[1],
                longitude: (float)xyz[0]
            );
        }



        private static NzQuasiGeoid2016Grid NZGeoid = null;


        /// <summary>
        /// Convert DJI GPS Altitude (ellipsoidal height, meters) to NZVD2016 height (meters) (NZ ground Lidar format)
        /// H_NZVD2016 = h_ellip - N, where N comes from NZGeoid2016 (quasigeoid).
        /// </summary>
        public static double DjiGpsAltToNzvd2016(string GroundDirectory, GlobalLocation globalLocation, double djiGpsAltitudeEllipM)
        {
            try
            {
                if (NZGeoid == null)
                    NZGeoid = new(GroundDirectory);

                double N = NZGeoid.GetN(globalLocation.Latitude, globalLocation.Longitude);   // meters
                return djiGpsAltitudeEllipM - N;           // meters NZVD2016
            }
            catch (Exception ex)
            {
                throw BaseConstants.ThrowException("DjiGpsAltToNzvd2016", ex);
            }
        }

        private sealed class NzQuasiGeoid2016Grid
        {
            private readonly Dataset _ds;
            private readonly Band _band;
            private readonly double[] _gt = new double[6];
            private readonly double _noData;
            private readonly bool _hasNoData;

            public NzQuasiGeoid2016Grid(string GroundDirectory)
            {
                string geoidTiffPath = GroundDirectory + "\\" + NzQuasiGeoid2016FilePath;

                try { 
                    Gdal.AllRegister();

                    _ds = Gdal.Open(geoidTiffPath, Access.GA_ReadOnly)
                          ?? throw new Exception($"Failed to open NZQuasiGeoid2016 raster: {geoidTiffPath}");

                    _ds.GetGeoTransform(_gt);
                    _band = _ds.GetRasterBand(1);

                    double nd;
                    int hasNd;
                    _band.GetNoDataValue(out nd, out hasNd);

                    _noData = nd;
                    _hasNoData = hasNd != 0;

                    // Assumes north-up raster (no rotation). Most LINZ geoid rasters are.
                    if (Math.Abs(_gt[2]) > 1e-12 || Math.Abs(_gt[4]) > 1e-12)
                        throw new NotSupportedException("Rotated geotransforms are not supported in this sampler.");
                }
                catch (Exception ex)
                {
                    throw BaseConstants.ThrowException("NzQuasiGeoid2016Grid", ex);
                }
            }

            /// <summary>
            /// Returns NZGeoid2016 N (meters) at WGS84/NZGD2000 latitude/longitude using bilinear interpolation.
            /// IMPORTANT: This samples the geoid raster in its native geographic CRS (lon/lat degrees).
            /// </summary>
            public double GetN(double latDeg, double lonDeg)
            {
                // Convert lon/lat -> fractional pixel coordinates
                // GeoTransform: Xgeo = gt[0] + px*gt[1]
                //               Ygeo = gt[3] + py*gt[5]  (gt[5] usually negative)
                double px = (lonDeg - _gt[0]) / _gt[1];
                double py = (latDeg - _gt[3]) / _gt[5];

                int x0 = (int)Math.Floor(px);
                int y0 = (int)Math.Floor(py);
                int x1 = x0 + 1;
                int y1 = y0 + 1;

                if (x0 < 0 || y0 < 0 || x1 >= _ds.RasterXSize || y1 >= _ds.RasterYSize)
                    throw new ArgumentOutOfRangeException("Point lies outside the NZQuasiGeoid2016 raster extent.");

                double dx = px - x0;
                double dy = py - y0;

                // Read a 2x2 block: [v00 v10; v01 v11]
                double[] buf = new double[4];
                _band.ReadRaster(x0, y0, 2, 2, buf, 2, 2, 0, 0);

                double v00 = buf[0];
                double v10 = buf[1];
                double v01 = buf[2];
                double v11 = buf[3];

                if (_hasNoData &&
                    (Near(v00, _noData) || Near(v10, _noData) || Near(v01, _noData) || Near(v11, _noData)))
                    throw new Exception("NZQuasiGeoid2016 raster returned NoData near this location.");

                // Bilinear interpolation
                double v0 = v00 * (1 - dx) + v10 * dx;
                double v1 = v01 * (1 - dx) + v11 * dx;
                return v0 * (1 - dy) + v1 * dy;
            }

            private static bool Near(double a, double b) => Math.Abs(a - b) < 1e-12;
        }

    }
}
