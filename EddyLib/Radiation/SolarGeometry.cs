using System;
using Rhino.Geometry;

namespace EddyLib
{
    public class SolarGeometry
    {
        public static int[] DaysInMonth = new int[] { 31, 28, 31, 30, 31, 30, 31, 31, 30, 31, 30, 31 };

        /// <summary>
        /// Generates 288 (12 months * 24 hours) representative sun vectors based on the 1st day of each month.
        /// </summary>
        /// <param name="solarElevation">List of 8760 solar elevation values from Weather data.</param>
        /// <param name="solarAzimuth">List of 8760 solar azimuth values from Weather data.</param>
        /// <param name="elevationCutoff">Minimum elevation angle to consider (default 3.0 degrees).</param>
        /// <returns>Array of 288 Vector3d sun positions.</returns>
        public Vector3d[] GetMonthlyRepresentativeSunVectors(System.Collections.Generic.IList<double> solarElevation, System.Collections.Generic.IList<double> solarAzimuth, double elevationCutoff = 3.0)
        {
            var sunPositions = new Vector3d[12 * 24];
            int vcnt = 0;

            for (int m = 0; m < 12; m++)
            {
                for (int h = 0; h < 24; h++)
                {
                    int hourOfYear = HourInYear(m, 0, h);

                    if (hourOfYear >= solarElevation.Count)
                    {
                        sunPositions[vcnt++] = Vector3d.Zero;
                        continue;
                    }

                    double el = solarElevation[hourOfYear];
                    double az = solarAzimuth[hourOfYear];

                    if (el > elevationCutoff)
                    {
                        double elRad = deg2rad(el);
                        double azRad = deg2rad(90 - az); // Convert cardinal azimuth to trig angle

                        double x = Math.Cos(azRad) * Math.Cos(elRad);
                        double y = Math.Sin(azRad) * Math.Cos(elRad);
                        double z = Math.Sin(elRad);
                        sunPositions[vcnt] = new Vector3d(x, y, z);
                    }
                    else
                    {
                        sunPositions[vcnt] = Vector3d.Zero;
                    }
                    vcnt++;
                }
            }
            return sunPositions;
        }

        public int HourInYear(int monthIndex, int dayIndex, int hourIndex)
        {
            int hr = 0;
            for (int m = 0; m < 12; m++)
            {
                if (m < monthIndex) hr += DaysInMonth[m] * 24;
                else
                {
                    hr += dayIndex * 24 + hourIndex;
                    break;
                }
            }
            return hr;
        }

        public void DayOfYear_To_MonthAndDay(int dayOfYear, out int month, out int day)
        {
            month = 0;
            day = 0;
            int doy = 0;
            for (int m = 0; m < 12; m++)
            {
                if (doy + DaysInMonth[m] > dayOfYear)
                {
                    month = m;
                    day = dayOfYear - doy;
                    return;
                }
                doy += DaysInMonth[m];
            }
        }

        public void HourOfYear_To_MDH(int hourOfYear, out int month, out int day, out int hour)
        {
            int dayOfYear = (int)Math.Floor(hourOfYear / 24d);
            DayOfYear_To_MonthAndDay(dayOfYear, out month, out day);
            hour = hourOfYear - dayOfYear * 24;
        }

        public double solarazimuth(double lat, double lon, double year, double month, double day, double hours, double minutes, double seconds, double timezone, double dlstime)
        {
            GetSolarPosition(lat, lon, year, month, day, hours, minutes, seconds, timezone, dlstime, out _, out double azimuth);
            return azimuth;
        }

        public double solarelevation(double lat, double lon, double year, double month, double day, double hours, double minutes, double seconds, double timezone, double dlstime)
        {
            GetSolarPosition(lat, lon, year, month, day, hours, minutes, seconds, timezone, dlstime, out double elevation, out _);
            return elevation;
        }

        public void GetSolarPosition(double lat, double lon, double year, double month, double day, double hours, double minutes, double seconds, double timezone, double dlstime, out double elevation, out double azimuth)
        {
            // Bolt optimization: Merged logic of solarelevation and solarazimuth to avoid redundant math.
            // Shared values like Julian Day, Sun Declination, and Equation of Time are calculated only once.

            double longitude = lon * -1;
            double Latitude = lat;
            if (Latitude > 89.8) Latitude = 89.8;
            if (Latitude < -89.8) Latitude = -89.8;

            double zone = timezone * -1;
            double daySavings = dlstime * 60;
            double hh = hours - (daySavings / 60);

            double timenow = hh + minutes / 60 + seconds / 3600 + zone;

            double jd = calcJD(year, month, day);
            double t = calcTimeJulianCent(jd + timenow / 24.0);
            double theta = calcSunDeclination(t);
            double Etime = calcEquationOfTime(t);

            double SolarDec = theta;

            double solarTimeFix = Etime - 4.0 * longitude + 60.0 * zone;
            double trueSolarTime = hh * 60.0 + minutes + seconds / 60.0 + solarTimeFix;

            while (trueSolarTime > 1440) trueSolarTime -= 1440;

            double hourAngle = trueSolarTime / 4.0 - 180.0;
            if (hourAngle < -180) hourAngle += 360.0;
            double harad = deg2rad(hourAngle);

            double csz = Math.Sin(deg2rad(Latitude)) * Math.Sin(deg2rad(SolarDec)) + Math.Cos(deg2rad(Latitude)) * Math.Cos(deg2rad(SolarDec)) * Math.Cos(harad);

            if (csz > 1.0) csz = 1.0;
            else if (csz < -1.0) csz = -1.0;

            double zenith = rad2deg(Math.Acos(csz));
            double azDenom = (Math.Cos(deg2rad(Latitude)) * Math.Sin(deg2rad(zenith)));

            if (Math.Abs(azDenom) > 0.001)
            {
                double azRad = ((Math.Sin(deg2rad(Latitude)) * Math.Cos(deg2rad(zenith))) - Math.Sin(deg2rad(SolarDec))) / azDenom;
                if (Math.Abs(azRad) > 1.0) azRad = azRad < 0 ? -1.0 : 1.0;

                azimuth = 180.0 - rad2deg(Math.Acos(azRad));
                if (hourAngle > 0.0) azimuth = -azimuth;
            }
            else
            {
                azimuth = Latitude > 0.0 ? 180.0 : 0.0;
            }
            if (azimuth < 0.0) azimuth += 360.0;

            double exoatmElevation = 90.0 - zenith;
            double refractionCorrection;
            if (exoatmElevation > 85.0)
            {
                refractionCorrection = 0.0;
            }
            else
            {
                double te = Math.Tan(deg2rad(exoatmElevation));
                if (exoatmElevation > 5.0)
                {
                    refractionCorrection = 58.1 / te - 0.07 / (te * te * te) + 8.6E-05 / (te * te * te * te * te);
                }
                else if (exoatmElevation > -0.575)
                {
                    double step1 = (-12.79 + exoatmElevation * 0.711);
                    double step2 = (103.4 + exoatmElevation * (step1));
                    double step3 = (-518.2 + exoatmElevation * (step2));
                    refractionCorrection = 1735.0 + exoatmElevation * (step3);
                }
                else
                {
                    refractionCorrection = -20.774 / te;
                }
                refractionCorrection /= 3600.0;
            }

            elevation = 90.0 - (zenith - refractionCorrection);
        }

        //--------------------------------------------------------------------
        //This section contains subroutines used in calculating solar position
        //--------------------------------------------------------------------

        //Convert radian angle to degrees
        //--------------------------------------------------------------------
        public double rad2deg(double angleRad)
        {
            return (180.0 * angleRad / Math.PI);
        }

        public double deg2rad(double angleDeg)
        {
            return Math.PI * angleDeg / 180.0;
        }

        //--------------------------------------------------------------------

        //Purpose:Julian day from calendar day
        //year : 4 digit year
        //month : January = 1
        //day : 1-31
        //Return value: The Julian day corresponding to the date
        //Note: Number is returned for start of day. Fractional days should be added later.
        //--------------------------------------------------------------------
        public double calcJD(double yr, double mth, double day)
        {
            if (mth <= 2)
            {
                yr = yr - 1;
                mth = mth + 12;
            }
            double A = Math.Floor(yr / 100);
            double B = 2 - A + Math.Floor(A / 4);
            double JD = Math.Floor(365.25 * (yr + 4716)) + Math.Floor(30.6001 * (mth + 1)) + day + B - 1524.5;
            return JD;
        }

        //--------------------------------------------------------------------

        //Purpose: convert Julian Day to centuries since J2000.0
        //Arguments: jd - the Julian Day to convert
        //Return value: the T value corresponding to the Julian Day
        public double calcTimeJulianCent(double jd)
        {
            double T = (jd - 2451545.0) / 36525.0;
            return T;
        }

        //-----------------------------------------------------------------------
        // Name:    calGeomMeanLongSun
        // Type:    Function
        // Purpose: calculate the Geometric Mean Longitude of the Sun
        // Arguments:
        //   t : number of Julian centuries since J2000.0
        // Return value:
        //   the Geometric Mean Longitude of the Sun in degrees
        //-----------------------------------------------------------------------
        public double calcGeomMeanLongSun(double t)
        {
            double LO = 280.46646 + t * (36000.76983 + 0.0003032 * t);
            while (LO > 360.0)
            {
                LO -= 360.0;
            }
            while (LO < 0.0)
            {
                LO += 360;
            }
            return LO; // in degrees
        }

        //-----------------------------------------------------------------------
        // Name:    calGeomAnomalySun
        // Type:    Function
        // Purpose: calculate the Geometric Mean Anomaly of the Sun
        // Arguments:
        //   t : number of Julian centuries since J2000.0
        // Return value:
        //   the Geometric Mean Anomaly of the Sun in degrees
        //-----------------------------------------------------------------------
        public double calcGeomMeanAnomalySun(double t)
        {
            double M = 357.52911 + t * (35999.05029 - 0.0001537 * t);
            return M; // in degrees
        }

        //-----------------------------------------------------------------------
        // Name:    calcEccentricityEarthOrbit
        // Type:    Function
        // Purpose: calculate the eccentricity of earth's orbit
        // Arguments:
        //   t : number of Julian centuries since J2000.0
        // Return value:
        //   the unitless eccentricity
        //-----------------------------------------------------------------------
        public double calcEccentricityEarthOrbit(double t)
        {
            double e = 0.016708634 - t * (0.000042037 + 0.0000001267 * t);
            return e; // unitless
        }

        //-----------------------------------------------------------------------
        // Name:    calcSunEqOfCenter
        // Type:    Function
        // Purpose: calculate the equation of center for the sun
        // Arguments:
        //   t : number of Julian centuries since J2000.0
        // Return value:
        //   in degrees
        //-----------------------------------------------------------------------
        public double calcSunEqOfCenter(double t)
        {
            double m = calcGeomMeanAnomalySun(t);
            double mrad = deg2rad(m);
            double sinm = Math.Sin(mrad);
            double sin2m = Math.Sin(mrad + mrad);
            double sin3m = Math.Sin(mrad + mrad + mrad);

            double C = sinm * (1.914602 - t * (0.004817 + 0.000014 * t)) + sin2m * (0.019993 - 0.000101 * t) + sin3m * 0.000289;
            return C; // in degrees
        }

        //-----------------------------------------------------------------------
        // Name:    calcSunTrueLong
        // Type:    Function
        // Purpose: calculate the true longitude of the sun
        // Arguments:
        //   t : number of Julian centuries since J2000.0
        // Return value:
        //   sun's true longitude in degrees
        //-----------------------------------------------------------------------
        public double calcSunTrueLong(double t)
        {
            double lo = calcGeomMeanLongSun(t);
            double c = calcSunEqOfCenter(t);
            double O = lo + c;
            return O; // in degrees
        }

        //-----------------------------------------------------------------------
        // Name:    calcSunTrueAnomaly
        // Type:    Function
        // Purpose: calculate the true anamoly of the sun
        // Arguments:
        //   t : number of Julian centuries since J2000.0
        // Return value:
        //   sun's true anamoly in degrees
        //-----------------------------------------------------------------------
        public double calcSunTrueAnomaly(double t)
        {
            double m = calcGeomMeanAnomalySun(t);
            double c = calcSunEqOfCenter(t);
            double v = m + c;
            return v; // in degrees
        }

        //-----------------------------------------------------------------------
        // Name:    calcSunRadVector
        // Type:    Function
        // Purpose: calculate the distance to the sun in AU
        // Arguments:
        //   t : number of Julian centuries since J2000.0
        // Return value:
        //   sun radius vector in AUs
        //-----------------------------------------------------------------------
        public double calcSunRadVector(double t)
        {
            double v = calcSunTrueAnomaly(t);
            double e = calcEccentricityEarthOrbit(t);
            double R = (1.000001018 * (1 - e * e)) / (1 + e * Math.Cos(deg2rad(v)));
            return R; // in AUs
        }

        //Functions to calculate Ascension
        //-----------------------------------------------
        //-----------------------------------------------------------------------
        // Name:    calcSunApparentLong
        // Type:    Function
        // Purpose: calculate the apparent longitude of the sun
        // Arguments:
        //   t : number of Julian centuries since J2000.0
        // Return value:
        //   sun's apparent longitude in degrees
        //-----------------------------------------------------------------------
        public double calcSunApparentLong(double t)
        {
            double o = calcSunTrueLong(t);
            double omega = 125.04 - 1934.136 * t;
            double lambda = o - 0.00569 - 0.00478 * Math.Sin(deg2rad(omega));
            return lambda; // in degrees
        }

        //-----------------------------------------------------------------------
        // Name:    calcMeanObliquityOfEcliptic
        // Type:    Function
        // Purpose: calculate the mean obliquity of the ecliptic
        // Arguments:
        //   t : number of Julian centuries since J2000.0
        // Return value:
        //   mean obliquity in degrees
        //-----------------------------------------------------------------------
        public double calcMeanObliquityOfEcliptic(double t)
        {
            double seconds = 21.448 - t * (46.8150 + t * (0.00059 - t * (0.001813)));
            double eO = 23.0 + (26.0 + (seconds / 60.0)) / 60.0;
            return eO; // in degrees
        }

        //-----------------------------------------------------------------------
        // Name:    calcObliquityCorrection
        // Type:    Function
        // Purpose: calculate the corrected obliquity of the ecliptic
        // Arguments:
        //   t : number of Julian centuries since J2000.0
        // Return value:
        //   corrected obliquity in degrees
        //-----------------------------------------------------------------------
        public double calcObliquityCorrection(double t)
        {
            double eO = calcMeanObliquityOfEcliptic(t);
            double omega = 125.04 - 1934.136 * t;
            double e = eO + 0.00256 * Math.Cos(deg2rad(omega));
            return e; // in degrees
        }

        //-----------------------------------------------------------------------
        // Name:    calcSunRtAscension
        // Type:    Function
        // Purpose: calculate the right ascension of the sun
        // Arguments:
        //   t : number of Julian centuries since J2000.0
        // Return value:
        //   sun's right ascension in degrees
        //-----------------------------------------------------------------------
        public double calcSunRtAscension(double t)
        {
            double e = calcObliquityCorrection(t);
            double lambda = calcSunApparentLong(t);

            double tananum = (Math.Cos(deg2rad(e)) * Math.Sin(deg2rad(lambda)));
            double tanadenom = (Math.Cos(deg2rad(lambda)));
            double alpha = rad2deg(Math.Atan2(tananum, tanadenom));
            return alpha; // in degrees
        }

        //-----------------------------------------------------------------------
        // Name:    calcSunDeclination
        // Type:    Function
        // Purpose: calculate the declination of the sun
        // Arguments:
        //   t : number of Julian centuries since J2000.0
        // Return value:
        //   sun's declination in degrees
        //-----------------------------------------------------------------------
        public double calcSunDeclination(double t)
        {
            double e = calcObliquityCorrection(t);
            double lambda = calcSunApparentLong(t);
            double sint = Math.Sin(deg2rad(e)) * Math.Sin(deg2rad(lambda));
            double theta = rad2deg(Math.Asin(sint));
            return theta; // in degrees
        }

        //-----------------------------------------------------------------------
        // Name:    calcEquationOfTime
        // Type:    Function
        // Purpose: calculate the difference between true solar time and mean
        //		solar time
        // Arguments:
        //   t : number of Julian centuries since J2000.0
        // Return value:
        //   equation of time in minutes of time
        //-----------------------------------------------------------------------
        public double calcEquationOfTime(double t)
        {
            double epsilon = calcObliquityCorrection(t);
            double l0 = calcGeomMeanLongSun(t);
            double e = calcEccentricityEarthOrbit(t);
            double m = calcGeomMeanAnomalySun(t);
            double y = Math.Tan(deg2rad(epsilon) / 2.0);
            y *= y;
            double sin210 = Math.Sin(2.0 * deg2rad(l0));
            double sinm = Math.Sin(deg2rad(m));
            double cos210 = Math.Cos(2.0 * deg2rad(l0));
            double sin410 = Math.Sin(4.0 * deg2rad(l0));
            double sin2m = Math.Sin(2.0 * deg2rad(m));

            double Etime = y * sin210 - 2.0 * e * sinm + 4.0 * e * y * sinm * cos210 - 0.5 * y * y * sin410 - 1.25 * e * e * sin2m;
            return rad2deg(Etime) * 4.0; // in minutes of time
        }

        //----------------------------------------
        //Return the hour angle for the given location, decl, and time of day
        public double calcHourAngle(double time, double longitude, double eqtime)
        {
            return 15.0 * (time - (longitude / 15.0) - (eqtime / 60.0));
        }
    }
}