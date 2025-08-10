using SkyCombGround.CommonSpace;


namespace SkyCombGround.GroundLogic
{
    // Based on C# code in https://gis.stackexchange.com/questions/225065/converting-nztm-new-zealand-transverse-mercator-to-lat-long/325294#325294
    public class Projection
    {
        // Structure used to define a TM projection
        protected struct tmProjection
        {

            internal double meridian;          // Central meridian 
            internal double scalef;            // Scale factor 
            internal double orglat;            // Origin latitude 
            internal double falsee;            // False easting 
            internal double falsen;            // False northing 
            internal double utom;              // Unit to metre conversion 
            internal double a, rf, f, e2, ep2; // Ellipsoid parameters 
            internal double om;                // Intermediate calculation 
        }


        protected const double PI = 3.1415926535898;
        protected const double TWOPI = 2.0 * PI;
        protected const double rad2deg = 180 / PI;


        /***************************************************************************/
        /*                                                                         */
        /*  meridian_arc                                                           */
        /*                                                                         */
        /*  Returns the length of meridional arc (Helmert formula)                 */
        /*  Method based on Redfearn's formulation as expressed in GDA technical   */
        /*  manual at http://www.anzlic.org.au/icsm/gdatm/index.html               */
        /*                                                                         */
        /*  Parameters are                                                         */
        /*    projection                                                           */
        /*    latitude (radians)                                                   */
        /*                                                                         */
        /*  Return value is the arc length in metres                               */
        /*                                                                         */
        /***************************************************************************/
        protected static double meridian_arc(tmProjection tm, double lt)
        {
            double e2 = tm.e2;
            double a = tm.a;

            double e4 = e2 * e2;
            double e6 = e4 * e2;

            double A0 = 1 - (e2 / 4.0) - (3.0 * e4 / 64.0) - (5.0 * e6 / 256.0);
            double A2 = (3.0 / 8.0) * (e2 + e4 / 4.0 + 15.0 * e6 / 128.0);
            double A4 = (15.0 / 256.0) * (e4 + 3.0 * e6 / 4.0);
            double A6 = 35.0 * e6 / 3072.0;

            return a * (A0 * lt - A2 * Math.Sin(2 * lt) + A4 * Math.Sin(4 * lt) - A6 * Math.Sin(6 * lt));
        }


        /*************************************************************************/
        /*                                                                       */
        /*   foot_point_lat                                                      */
        /*                                                                       */
        /*   Calculates the foot point latitude from the meridional arc          */
        /*   Method based on Redfearn's formulation as expressed in GDA technical*/
        /*   manual at http://www.anzlic.org.au/icsm/gdatm/index.html            */
        /*                                                                       */
        /*   Takes parameters                                                    */
        /*      tm definition (for scale factor)                                 */
        /*      meridional arc (metres)                                          */
        /*                                                                       */
        /*   Returns the foot point latitude (radians)                           */
        /*                                                                       */
        /*************************************************************************/
        protected static double foot_point_lat(tmProjection tm, double m)
        {
            double f = tm.f;
            double a = tm.a;

            double n = f / (2.0 - f);
            double n2 = n * n;
            double n3 = n2 * n;
            double n4 = n2 * n2;

            double g = a * (1.0 - n) * (1.0 - n2) * (1 + 9.0 * n2 / 4.0 + 225.0 * n4 / 64.0);
            double sig = m / g;

            double phio = sig + (3.0 * n / 2.0 - 27.0 * n3 / 32.0) * Math.Sin(2.0 * sig)
                            + (21.0 * n2 / 16.0 - 55.0 * n4 / 32.0) * Math.Sin(4.0 * sig)
                            + (151.0 * n3 / 96.0) * Math.Sin(6.0 * sig)
                            + (1097.0 * n4 / 512.0) * Math.Sin(8.0 * sig);

            return phio;
        }


        /***************************************************************************/
        /*                                                                         */
        /*   tmgeod                                                                */
        /*                                                                         */
        /*   Routine to convert from Tranverse Mercator to latitude and longitude. */
        /*   Method based on Redfearn's formulation as expressed in GDA technical  */
        /*   manual at http://www.anzlic.org.au/icsm/gdatm/index.html              */
        /*                                                                         */
        /*   Takes parameters                                                      */
        /*      input easting (metres)                                             */
        /*      input northing (metres)                                            */
        /*      output latitude (radians)                                          */
        /*      output longitude (radians)                                         */
        /*                                                                         */
        /***************************************************************************/
        protected static (double lat, double lon) tm_geod(tmProjection tm, double ce, double cn)
        {
            double lat;
            double lon;

            double fn = tm.falsen;
            double fe = tm.falsee;
            double sf = tm.scalef;
            double e2 = tm.e2;
            double a = tm.a;
            double cm = tm.meridian;
            double om = tm.om;
            double utom = tm.utom;

            double t;
            double t2;
            double t4;
            double trm1;
            double trm2;
            double trm3;
            double trm4;

            double cn1 = (cn - fn) * utom / sf + om;
            double fphi = foot_point_lat(tm, cn1);
            double slt = Math.Sin(fphi);
            double clt = Math.Cos(fphi);

            double eslt = (1.0 - e2 * slt * slt);
            double eta = a / Math.Sqrt(eslt);
            double rho = eta * (1.0 - e2) / eslt;
            double psi = eta / rho;

            double E = (ce - fe) * utom;
            double x = E / (eta * sf);
            double x2 = x * x;


            t = slt / clt;
            t2 = t * t;
            t4 = t2 * t2;

            trm1 = 1.0 / 2.0;

            trm2 = ((-4.0 * psi
                         + 9.0 * (1 - t2)) * psi
                         + 12.0 * t2) / 24.0;

            trm3 = ((((8.0 * (11.0 - 24.0 * t2) * psi
                          - 12.0 * (21.0 - 71.0 * t2)) * psi
                          + 15.0 * ((15.0 * t2 - 98.0) * t2 + 15)) * psi
                          + 180.0 * ((-3.0 * t2 + 5.0) * t2)) * psi + 360.0 * t4) / 720.0;

            trm4 = (((1575.0 * t2 + 4095.0) * t2 + 3633.0) * t2 + 1385.0) / 40320.0;

            lat = fphi + (t * x * E / (sf * rho)) * (((trm4 * x2 - trm3) * x2 + trm2) * x2 - trm1);

            trm1 = 1.0;

            trm2 = (psi + 2.0 * t2) / 6.0;

            trm3 = (((-4.0 * (1.0 - 6.0 * t2) * psi
                       + (9.0 - 68.0 * t2)) * psi
                       + 72.0 * t2) * psi
                       + 24.0 * t4) / 120.0;

            trm4 = (((720.0 * t2 + 1320.0) * t2 + 662.0) * t2 + 61.0) / 5040.0;

            lon = cm - (x / clt) * (((trm4 * x2 - trm3) * x2 + trm2) * x2 - trm1);

            return (lat * rad2deg, lon * rad2deg);
        }


        /***************************************************************************/
        /*                                                                         */
        /*   geodtm                                                                */
        /*                                                                         */
        /*   Routine to convert from latitude and longitude to Transverse Mercator.*/
        /*   Method based on Redfearn's formulation as expressed in GDA technical  */
        /*   manual at http://www.anzlic.org.au/icsm/gdatm/index.html              */
        /*   Loosely based on FORTRAN source code by J.Hannah and A.Broadhurst.    */
        /*                                                                         */
        /*   Takes parameters                                                      */
        /*      input latitude (radians)                                           */
        /*      input longitude (radians)                                          */
        /*      output easting  (metres)                                           */
        /*      output northing (metres)                                           */
        /*                                                                         */
        /***************************************************************************/
        protected static (double n, double e) geod_tm(tmProjection tm, double ln, double lt)
        {
            double fn = tm.falsen;
            double fe = tm.falsee;
            double sf = tm.scalef;
            double e2 = tm.e2;
            double a = tm.a;
            double cm = tm.meridian;
            double om = tm.om;
            double utom = tm.utom;
            double dlon;
            double m;
            double slt;
            double eslt;
            double eta;
            double rho;
            double psi;
            double clt;
            double w;
            double wc;
            double wc2;
            double t;
            double t2;
            double t4;
            double t6;
            double trm1;
            double trm2;
            double trm3;
            double gce;
            double trm4;
            double gcn;

            dlon = ln - cm;
            while (dlon > PI) dlon -= TWOPI;
            while (dlon < -PI) dlon += TWOPI;

            m = meridian_arc(tm, lt);

            slt = Math.Sin(lt);

            eslt = (1.0 - e2 * slt * slt);
            eta = a / Math.Sqrt(eslt);
            rho = eta * (1.0 - e2) / eslt;
            psi = eta / rho;

            clt = Math.Cos(lt);
            w = dlon;

            wc = clt * w;
            wc2 = wc * wc;

            t = slt / clt;
            t2 = t * t;
            t4 = t2 * t2;
            t6 = t2 * t4;

            trm1 = (psi - t2) / 6.0;

            trm2 = (((4.0 * (1.0 - 6.0 * t2) * psi
                          + (1.0 + 8.0 * t2)) * psi
                          - 2.0 * t2) * psi + t4) / 120.0;

            trm3 = (61 - 479.0 * t2 + 179.0 * t4 - t6) / 5040.0;

            gce = (sf * eta * dlon * clt) * (((trm3 * wc2 + trm2) * wc2 + trm1) * wc2 + 1.0);
            double e = gce / utom + fe;

            trm1 = 1.0 / 2.0;

            trm2 = ((4.0 * psi + 1) * psi - t2) / 24.0;

            trm3 = ((((8.0 * (11.0 - 24.0 * t2) * psi
                        - 28.0 * (1.0 - 6.0 * t2)) * psi
                        + (1.0 - 32.0 * t2)) * psi
                        - 2.0 * t2) * psi
                        + t4) / 720.0;

            trm4 = (1385.0 - 3111.0 * t2 + 543.0 * t4 - t6) / 40320.0;

            gcn = (eta * t) * ((((trm4 * wc2 + trm3) * wc2 + trm2) * wc2 + trm1) * wc2);
            double n = (gcn + m - om) * sf / utom + fn;

            return (n, e);
        }


        protected static tmProjection define_tmprojection(double a, double rf, double cm, double sf, double lto, double fe, double fn, double utom)
        {
            double f = (rf != 0.0 ? 1.0 / rf : 0.0);

            var tm = new tmProjection();

            tm.meridian = cm;
            tm.scalef = sf;
            tm.orglat = lto;
            tm.falsee = fe;
            tm.falsen = fn;
            tm.utom = utom;
            tm.a = a;
            tm.rf = rf;
            tm.f = f;
            tm.e2 = 2.0 * f - f * f;
            tm.ep2 = tm.e2 / (1.0 - tm.e2);
            tm.om = meridian_arc(tm, tm.orglat);

            return tm;
        }
    }


    // Implement the TM projection specifically for the NZTM coordinate system. 
    public class NztmProjection : Projection
    {
        protected const double NZTM_A = 6378137;
        protected const double NZTM_RF = 298.257222101;

        protected const double NZTM_CM = 173.0;
        protected const double NZTM_OLAT = 0.0;
        protected const double NZTM_SF = 0.9996;
        protected const double NZTM_FE = 1600000.0;
        protected const double NZTM_FN = 10000000.0;

        protected static tmProjection nztmProjection = define_tmprojection(NZTM_A, NZTM_RF,
                            NZTM_CM / rad2deg, NZTM_SF, NZTM_OLAT / rad2deg, NZTM_FE, NZTM_FN,
                            1.0);


        // For the NZTM coordinate system convert Northing, Easting to Latitude, Longitude
        public static (double lat, double lon) NztmToWgs(double n, double e)
        {
            return tm_geod(nztmProjection, e, n);
        }


        // For the NZTM coordinate system convert Latitude, Longitude to Northing, Easting
        public static (double n, double e) WgsToNztm(double lat, double lon)
        {
            return geod_tm(nztmProjection, lon / rad2deg, lat / rad2deg);
        }
        public static CountryLocation WgsToNztm(GlobalLocation location)
        {
            (double n, double e) = geod_tm(nztmProjection, location.Longitude / rad2deg, location.Latitude / rad2deg);
            var answer = new CountryLocation((float)n, (float)e);
            answer.AssertGood();
            return answer;
        }

        // Check that round-trip conversions of 2 sample NZ latitude/longitude coordinates give a very small error.
        public static void AssertGood()
        {
            double westBoundLongitude = 174.15686794873625;
            double eastBoundLongitude = 175.55578930011853;
            double southBoundLatitude = -37.057123653801554;
            double northBoundLatitude = -36.02306055654798;

            (double northing1, double easting1) = WgsToNztm(northBoundLatitude, westBoundLongitude);
            (double latitude1, double longitude1) = NztmToWgs(northing1, easting1);

            (double northing2, double easting2) = WgsToNztm(southBoundLatitude, eastBoundLongitude);
            (double latitude2, double longitude2) = NztmToWgs(northing2, easting2);

            BaseConstants.Assert(Math.Abs(northBoundLatitude - latitude1) < 0.000001, "NztmProjection.UnitTest: Bad northBoundLatitude");
            BaseConstants.Assert(Math.Abs(westBoundLongitude - longitude1) < 0.000001, "NztmProjection.UnitTest: Bad westBoundLongitude");
            BaseConstants.Assert(Math.Abs(southBoundLatitude - latitude2) < 0.000001, "NztmProjection.UnitTest: Bad southBoundLatitude");
            BaseConstants.Assert(Math.Abs(eastBoundLongitude - longitude2) < 0.000001, "NztmProjection.UnitTest: Bad eastBoundLongitude");
        }
    }
}
