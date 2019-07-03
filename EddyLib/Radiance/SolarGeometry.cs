using System;

namespace EddyLib
{
    public class SolarGeometry
    {
        public double solarazimuth(double lat, double lon, double year, double month, double day, double hours, double minutes, double seconds, double timezone, double dlstime)
        {
            //***********************************************************************/
            //* Name:    solarazimuth
            //* Type:    Main Function
            //* Purpose: calculate solar azimuth (deg from north) for the entered
            //*          date, time and location. Returns -999999 if darker than twilight
            //*
            //* Arguments:
            //*   latitude, longitude, year, month, day, hour, minute, second,
            //*   timezone, daylightsavingstime
            //* Return value:
            //*   solar azimuth in degrees from north
            //*
            //* Note: solarelevation and solarazimuth functions are identical
            //*       and could be converted to a VBA subroutine that would return
            //*       both values.
            //*
            //***********************************************************************/

            double longitude = 0;
            double Latitude = 0;
            double zone = 0;
            double daySavings = 0;
            double hh = 0;
            double mm = 0;
            double SS = 0;
            double timenow = 0;
            double jd = 0;
            double t = 0;
            double r = 0;
            double alpha = 0;
            double theta = 0;
            double Etime = 0;
            double eqtime = 0;
            double SolarDec = 0;
            double earthRadVec = 0;
            double solarTimeFix = 0;
            double trueSolarTime = 0;
            double hourAngle = 0;
            double harad = 0;
            double csz = 0;
            double zenith = 0;
            double azDenom = 0;
            double azRad = 0;
            double azimuth = 0;
            double exoatmElevation = 0;
            double step1 = 0;
            double step2 = 0;
            double step3 = 0;
            double refractionCorrection = 0;
            double te = 0;
            double solarZen = 0;

            // change sign convention for longitude from negative to positive in western hemisphere
            longitude = lon * -1;
            Latitude = lat;
            if (Latitude > 89.8)
            {
                Latitude = 89.8;
            }
            if (Latitude < -89.8)
            {
                Latitude = -89.8;
            }

            //change time zone to ppositive hours in western hemisphere
            zone = timezone * -1;
            daySavings = dlstime * 60;
            hh = hours - (daySavings / 60);
            mm = minutes;
            SS = seconds;

            ////    timenow is GMT time for calculation in hours since 0Z
            timenow = hh + mm / 60 + SS / 3600 + zone;

            jd = calcJD(year, month, day);
            t = calcTimeJulianCent(jd + timenow / 24.0);
            r = calcSunRadVector(t);
            alpha = calcSunRtAscension(t);
            theta = calcSunDeclination(t);
            Etime = calcEquationOfTime(t);

            eqtime = Etime;
            SolarDec = theta;
            ////    in degrees
            earthRadVec = r;

            solarTimeFix = eqtime - 4.0 * longitude + 60.0 * zone;
            trueSolarTime = hh * 60.0 + mm + SS / 60.0 + solarTimeFix;
            ////    in minutes

            while ((trueSolarTime > 1440))
            {
                trueSolarTime = trueSolarTime - 1440;
            }

            hourAngle = trueSolarTime / 4.0 - 180.0;
            ////    Thanks to Louis Schwarzmayr for the next line:
            if (hourAngle < -180)
            {
                hourAngle = hourAngle + 360.0;
            }
            harad = deg2rad(hourAngle);

            csz = Math.Sin(deg2rad(Latitude)) * Math.Sin(deg2rad(SolarDec)) + Math.Cos(deg2rad(Latitude)) * Math.Cos(deg2rad(SolarDec)) * Math.Cos(harad);

            if ((csz > 1.0))
            {
                csz = 1.0;
            }
            else if ((csz < -1.0))
            {
                csz = -1.0;
            }

            zenith = rad2deg(Math.Acos(csz));

            azDenom = (Math.Cos(deg2rad(Latitude)) * Math.Sin(deg2rad(zenith)));

            if ((Math.Abs(azDenom) > 0.001))
            {
                azRad = ((Math.Sin(deg2rad(Latitude)) * Math.Cos(deg2rad(zenith))) - Math.Sin(deg2rad(SolarDec))) / azDenom;
                if ((Math.Abs(azRad) > 1.0))
                {
                    if ((azRad < 0))
                    {
                        azRad = -1.0;
                    }
                    else
                    {
                        azRad = 1.0;
                    }
                }

                azimuth = 180.0 - rad2deg(Math.Acos(azRad));

                if ((hourAngle > 0.0))
                {
                    azimuth = -azimuth;
                }
            }
            else
            {
                if ((Latitude > 0.0))
                {
                    azimuth = 180.0;
                }
                else
                {
                    azimuth = 0.0;
                }
            }
            if ((azimuth < 0.0))
            {
                azimuth = azimuth + 360.0;
            }

            exoatmElevation = 90.0 - zenith;

            //beginning of complex expression commented out
            /*if ((exoatmElevation > 85.0))
            {
            refractionCorrection = 0.0;
            }
            else
            {
            te = Math.Tan(deg2rad(exoatmElevation));
            if ((exoatmElevation > 5.0))
            {
            refractionCorrection = 58.1 / te - 0.07 / (te * te * te) + 8.6E-05 / (te * te * te * te * te);
            }
            else if ((exoatmElevation > -0.575))
            {
            refractionCorrection = 1735.0 + exoatmElevation * (-518.2 + exoatmElevation * (103.4 + exoatmElevation * (-12.79 + exoatmElevation * 0.711)));
            }
            else
            {
            refractionCorrection = -20.774 / te;
            }
            refractionCorrection = refractionCorrection / 3600.0;
            }*/
            //end of complex expression

            //beginning of simplified expression
            if ((exoatmElevation > 85.0))
            {
                refractionCorrection = 0.0;
            }
            else
            {
                te = Math.Tan(deg2rad(exoatmElevation));
                if ((exoatmElevation > 5.0))
                {
                    refractionCorrection = 58.1 / te - 0.07 / (te * te * te) + 8.6E-05 / (te * te * te * te * te);
                }
                else if ((exoatmElevation > -0.575))
                {
                    step1 = (-12.79 + exoatmElevation * 0.711);
                    step2 = (103.4 + exoatmElevation * (step1));
                    step3 = (-518.2 + exoatmElevation * (step2));
                    refractionCorrection = 1735.0 + exoatmElevation * (step3);
                }
                else
                {
                    refractionCorrection = -20.774 / te;
                }
                refractionCorrection = refractionCorrection / 3600.0;
            }
            //end of simplified expression

            solarZen = zenith - refractionCorrection;

            return azimuth;
            /*
                if ((solarZen < 108.0)) {
                  solarelevation = 90.0 - solarZen;
                  if ((solarZen < 90.0)) {
                    cosZen = Math.Cos(deg2rad(solarZen));
                  } else {
                    cosZen = 0.0;
                  }
                  //// do not report az & el after astro twilight
                } else {
                  solarazimuth = -999999;
                  solarelevation = -999999;
                  cosZen = -999999;
                }*/
        }

        public double solarelevation(double lat, double lon, double year, double month, double day, double hours, double minutes, double seconds, double timezone, double dlstime)
        {
            //***********************************************************************/
            //* Name:    solarazimuth
            //* Type:    Main Function
            //* Purpose: calculate solar azimuth (deg from north) for the entered
            //*          date, time and location. Returns -999999 if darker than twilight
            //*
            //* Arguments:
            //*   latitude, longitude, year, month, day, hour, minute, second,
            //*   timezone, daylightsavingstime
            //* Return value:
            //*   solar azimuth in degrees from north
            //*
            //* Note: solarelevation and solarazimuth functions are identical
            //*       and could converted to a VBA subroutine that would return
            //*       both values.
            //*
            //***********************************************************************/

            double longitude = 0;
            double Latitude = 0;
            double zone = 0;
            double daySavings = 0;
            double hh = 0;
            double mm = 0;
            double SS = 0;
            double timenow = 0;
            double jd = 0;
            double t = 0;
            double r = 0;
            double alpha = 0;
            double theta = 0;
            double Etime = 0;
            double eqtime = 0;
            double SolarDec = 0;
            double earthRadVec = 0;
            double solarTimeFix = 0;
            double trueSolarTime = 0;
            double hourAngle = 0;
            double harad = 0;
            double csz = 0;
            double zenith = 0;
            double azDenom = 0;
            double azRad = 0;
            double azimuth = 0;
            double exoatmElevation = 0;
            double step1 = 0;
            double step2 = 0;
            double step3 = 0;
            double refractionCorrection = 0;
            double te = 0;
            double solarZen = 0;

            // change sign convention for longitude from negative to positive in western hemisphere
            longitude = lon * -1;
            Latitude = lat;
            if (Latitude > 89.8)
            {
                Latitude = 89.8;
            }
            if (Latitude < -89.8)
            {
                Latitude = -89.8;
            }

            //change time zone to ppositive hours in western hemisphere
            zone = timezone * -1;
            daySavings = dlstime * 60;
            hh = hours - (daySavings / 60);
            mm = minutes;
            SS = seconds;

            ////    timenow is GMT time for calculation in hours since 0Z
            timenow = hh + mm / 60 + SS / 3600 + zone;

            jd = calcJD(year, month, day);
            t = calcTimeJulianCent(jd + timenow / 24.0);
            r = calcSunRadVector(t);
            alpha = calcSunRtAscension(t);
            theta = calcSunDeclination(t);
            Etime = calcEquationOfTime(t);

            eqtime = Etime;
            SolarDec = theta;
            ////    in degrees
            earthRadVec = r;

            solarTimeFix = eqtime - 4.0 * longitude + 60.0 * zone;
            trueSolarTime = hh * 60.0 + mm + SS / 60.0 + solarTimeFix;
            ////    in minutes

            while ((trueSolarTime > 1440))
            {
                trueSolarTime = trueSolarTime - 1440;
            }

            hourAngle = trueSolarTime / 4.0 - 180.0;
            ////    Thanks to Louis Schwarzmayr for the next line:
            if (hourAngle < -180)
            {
                hourAngle = hourAngle + 360.0;
            }

            harad = deg2rad(hourAngle);

            csz = Math.Sin(deg2rad(Latitude)) * Math.Sin(deg2rad(SolarDec)) + Math.Cos(deg2rad(Latitude)) * Math.Cos(deg2rad(SolarDec)) * Math.Cos(harad);

            if ((csz > 1.0))
            {
                csz = 1.0;
            }
            else if ((csz < -1.0))
            {
                csz = -1.0;
            }

            zenith = rad2deg(Math.Acos(csz));

            azDenom = (Math.Cos(deg2rad(Latitude)) * Math.Sin(deg2rad(zenith)));

            if ((Math.Abs(azDenom) > 0.001))
            {
                azRad = ((Math.Sin(deg2rad(Latitude)) * Math.Cos(deg2rad(zenith))) - Math.Sin(deg2rad(SolarDec))) / azDenom;
                if ((Math.Abs(azRad) > 1.0))
                {
                    if ((azRad < 0))
                    {
                        azRad = -1.0;
                    }
                    else
                    {
                        azRad = 1.0;
                    }
                }

                azimuth = 180.0 - rad2deg(Math.Acos(azRad));

                if ((hourAngle > 0.0))
                {
                    azimuth = -azimuth;
                }
            }
            else
            {
                if ((Latitude > 0.0))
                {
                    azimuth = 180.0;
                }
                else
                {
                    azimuth = 0.0;
                }
            }
            if ((azimuth < 0.0))
            {
                azimuth = azimuth + 360.0;
            }

            exoatmElevation = 90.0 - zenith;

            //beginning of complex expression commented out
            /*if ((exoatmElevation > 85.0)) {
            refractionCorrection = 0.0;
            } else {
            te = Math.Tan(deg2rad(exoatmElevation));
            if ((exoatmElevation > 5.0)) {
            refractionCorrection = 58.1 / te - 0.07 / (te * te * te) + 8.6E-05 / (te * te * te * te * te);
            } else if ((exoatmElevation > -0.575)) {
            refractionCorrection = 1735.0 + exoatmElevation * (-518.2 + exoatmElevation * (103.4 + exoatmElevation * (-12.79 + exoatmElevation * 0.711)));
            } else {
            refractionCorrection = -20.774 / te;
            }
            refractionCorrection = refractionCorrection / 3600.0;
            }*/
            //end of complex expression

            //beginning of simplified expression
            if ((exoatmElevation > 85.0))
            {
                refractionCorrection = 0.0;
            }
            else
            {
                te = Math.Tan(deg2rad(exoatmElevation));
                if ((exoatmElevation > 5.0))
                {
                    refractionCorrection = 58.1 / te - 0.07 / (te * te * te) + 8.6E-05 / (te * te * te * te * te);
                }
                else if ((exoatmElevation > -0.575))
                {
                    step1 = (-12.79 + exoatmElevation * 0.711);
                    step2 = (103.4 + exoatmElevation * (step1));
                    step3 = (-518.2 + exoatmElevation * (step2));
                    refractionCorrection = 1735.0 + exoatmElevation * (step3);
                }
                else
                {
                    refractionCorrection = -20.774 / te;
                }
                refractionCorrection = refractionCorrection / 3600.0;
            }
            //end of simplified expression

            solarZen = zenith - refractionCorrection;

            /*
                double solelev = 0;
                double solazi = 0;
                double cosZen = 0;

                if ((solarZen < 108.0)) {
                  solelev = 90.0 - solarZen;
                  if ((solarZen < 90.0)) {
                    cosZen = Math.Cos(deg2rad(solarZen));
                  } else {
                    cosZen = 0.0;
                  }
                  //// do not report az & el after astro twilight
                } else {
                  solazi = -999999;
                  solelev = -999999;
                  cosZen = -999999;
                }
            */
            return 90.0 - solarZen;
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