using System;
using System.Runtime.CompilerServices;

[assembly: InternalsVisibleTo("RhinoPlugin.Tests.Xunit.SolarGainTests")]

namespace EddyLib.Radiation
{
    public class SolarGain
    {
        public static float[][] ComputeStanding(Weather weather, float[][] totalRad, float[][] directRad)
        {
            int numberOfHours = directRad.Length;
            int numberOfSensors = directRad[0].Length;

            float[][] Values = new float[numberOfHours][];
            for (int h = 0; h < numberOfHours; h++)
            {
                Values[h] = new float[numberOfSensors];
            }

            System.Threading.Tasks.Parallel.For(0, numberOfSensors, p =>
            {
                for (int h = 0; h < numberOfHours; h++)
                {
                    double dMRT;
                    double diffRad = totalRad[h][p] - directRad[h][p];
                    double dirRad = directRad[h][p];

                    dMRT = SolarGain.ERF_Modified(weather.SolarElevation[h], SolarGain.Posture.standing, dirRad, diffRad);

                    Values[h][p] = (float)dMRT;
                }
            });

            return Values;
        }

        public static float[] ComputeStanding(Weather weather, float[] totalRad, float[] directRad)
        {
            int numberOfHours = directRad.Length;

            float[] Values = new float[numberOfHours];

            for (int h = 0; h < numberOfHours; h++)
            {
                double dMRT;
                double diffRad = totalRad[h] - directRad[h];
                double dirRad = directRad[h];

                dMRT = SolarGain.ERF_Modified(weather.SolarElevation[h], SolarGain.Posture.standing, dirRad, diffRad);

                Values[h] = (float)dMRT;
            }

            return Values;
        }

        //// https://github.com/CenterForTheBuiltEnvironment/pythermalcomfort/blob/master/src/pythermalcomfort/models.py

        ////
        ////         Calculates the solar gain to the human body using the Effective Radiant Field (ERF) [1]_. The ERF is a measure of the net energy flux to or from the human body.
        ////         ERF is expressed in W over human body surface area [w/m2]. In addition, it calculates the delta mean radiant temperature. Which is the amount by which the mean radiant
        ////         temperature of the space should be increased if no solar radiation is present.
        ////         Parameters
        ////         ----------
        ////         sol_altitude : float
        ////             Solar altitude, degrees from horizontal [deg]. Ranges between 0 and 90.
        ////         sol_azimuth : float
        ////             Solar azimuth, degrees clockwise from North [deg]. Ranges between 0 and 180.
        ////         posture : str
        ////             Default 'seated' list of available options 'standing', 'supine' or 'seated'
        ////         sol_radiation_dir : float
        ////             Direct-beam solar radiation, [W/m2]. Ranges between 200 and 1000. See Table C2-3 of ASHRAE 55 2017 [1]_.
        ////         sol_transmittance : float
        ////             Total solar transmittance, ranges from 0 to 1. The total solar transmittance of window systems, including glazing unit, blinds, and other façade treatments, shall be determined using one of the following methods:
        ////             i) Provided by manufacturer or from the National Fenestration Rating Council approved Lawrence Berkeley National Lab International Glazing Database.
        ////             ii) Glazing unit plus venetian blinds or other complex or unique shades shall be calculated using National Fenestration Rating Council approved software or Lawrence Berkeley National Lab Complex Glazing Database.
        ////         f_svv : float
        ////             Fraction of sky vault exposed to body, ranges from 0 to 1.
        ////         f_bes : float
        ////             Fraction of the possible body surface exposed to sun, ranges from 0 to 1. See Table C2-2 and equation C-7 ASHRAE 55 2017 [1]_.
        ////         asw: float
        ////             The average short-wave absorptivity of the occupant. It will range widely, depending on the color of the occupant’s skin as well as the color and amount of clothing covering the body.
        ////             A value of 0.7 shall be used unless more specific information about the clothing or skin color of the occupants is available.
        ////             Note: Short-wave absorptivity typically ranges from 0.57 to 0.84, depending on skin and clothing color. More information is available in Blum (1945).
        ////         floor_reflectance: float
        ////             Floor refectance. It is assumed to be constant and equal to 0.6.
        ////         Notes
        ////         -----
        ////         More information on the calculation procedure can be found in Appendix C of [1]_.
        ////         Returns
        ////         -------
        ////         erf: float
        ////             Solar gain to the human body using the Effective Radiant Field [W/m2]
        ////         delta_mrt: float
        ////             Delta mean radiant temperature. The amount by which the mean radiant temperature of the space should be increased if no solar radiation is present.
        ////         Examples
        ////         --------
        ////         .. code-block:: python
        ////             >>> from pythermalcomfort.models import solar_gain
        ////             >>> results = solar_gain(sol_altitude=0, sol_azimuth=120, sol_radiation_dir=800, sol_transmittance=0.5, f_svv=0.5, f_bes=0.5, asw=0.7, posture='seated')
        ////             >>> print(results)
        ////             {'erf': 42.9, 'delta_mrt': 10.3}
        ////

        public enum Posture
        {
            supine,

            seating,

            standing
        };

        public static double ERF_Modified(double alt, Posture posture, double Idir, double Idiff, double asa = 0.7)
        {
            //  ERF function to estimate the impact of solar radiation on occupant comfort
            //  INPUTS:
            //  alt : altitude of sun in degrees [0, 90]
            //  az : azimuth of sun in degrees [0, 180]
            //  posture: posture of occupant ('seated', 'standing', or 'supine')
            //  Idir : direct beam intensity (normal)
            //  tsol: total solar transmittance (SC * 0.87)
            //  fsvv : sky vault view fraction : fraction of sky vault in occupant's view [0, 1]
            //  fbes : fraction body exposed to sun [0, 1] // Patrick: In our case 1 since we have no windows
            //  asa : avg shortwave abs : average shortwave absorptivity of body [0, 1]
            //  tsol_factor : (optional) correction to tsol based on angle of incidence

            //var DEG_TO_RAD = 0.0174532925;
            var hr = 6;

            //var Idiff = 0.2 * Idir;
            //double fsvv = 1; never used

            // Floor reflectance
            // var Rfloor = 0.6;

            var rad = Utilities.Deg2Rad(alt);

            var fp = Get_fp_cylinder(rad);

            double feff;
            if (posture == Posture.standing || posture == Posture.supine)
            {
                feff = 0.725;
            }
            else
            {
                feff = 0.696;
            }

            var sw_abs = asa;
            var lw_abs = 0.95;

            // We take Idiff directly from the simulation

            var E_diff = feff * Idiff;

            //var E_diff = feff * Idiff;

            var E_direct = fp * Idir;

            //var E_refl = feff * fsvv * 0.5 * tsol * (Idir * Math.Sin(alt * DEG_TO_RAD) + Idiff) * Rfloor;

            var E_solar = E_diff + E_direct; // + E_refl;
            var ERF = E_solar * (sw_abs / lw_abs);
            var dMRT = ERF / (hr * feff);

            return dMRT;
        }

        public static void ERF(double alt, double az, Posture posture, double Idir, double tsol, double fsvv, double fbes, double asa, out double ERF, out double dMRT, double tsol_factor = 1.0)
        {
            //  ERF function to estimate the impact of solar radiation on occupant comfort
            //  INPUTS:
            //  alt : altitude of sun in degrees [0, 90]
            //  az : azimuth of sun in degrees [0, 180]
            //  posture: posture of occupant ('seated', 'standing', or 'supine')
            //  Idir : direct beam intensity (normal)
            //  tsol: total solar transmittance (SC * 0.87)
            //  fsvv : sky vault view fraction : fraction of sky vault in occupant's view [0, 1]
            //  fbes : fraction body exposed to sun [0, 1]
            //  asa : avg shortwave abs : average shortwave absorptivity of body [0, 1]
            //  tsol_factor : (optional) correction to tsol based on angle of incidence

            var DEG_TO_RAD = 0.0174532925;
            var hr = 6;
            var Idiff = 0.2 * Idir;

            // Floor reflectance
            var Rfloor = 0.6;

            var fp = Get_fp(alt, az, posture);

            double feff;
            if (posture == Posture.standing || posture == Posture.supine)
            {
                feff = 0.725;
            }
            else
            {
                feff = 0.696;
            }

            var sw_abs = asa;
            var lw_abs = 0.95;

            var E_diff = feff * fsvv * 0.5 * tsol * Idiff;
            var E_direct = fp * tsol * fbes * Idir;
            var E_refl = feff * fsvv * 0.5 * tsol * (Idir * Math.Sin(alt * DEG_TO_RAD) + Idiff) * Rfloor;

            var E_solar = E_diff + E_direct + E_refl;
            ERF = E_solar * (sw_abs / lw_abs);
            dMRT = ERF / (hr * feff);
        }

        private static int Find_span(int[] arr, double x)
        {
            // for ordered array arr and value x, find the left index
            // of the closed interval that the value falls in.

            for (var i = 0; i < arr.Length - 1; i++)
            {
                if (x <= arr[i + 1] && x >= arr[i])
                {
                    return i;
                }
            }

            return -1;
        }

        private static double Get_fp_cylinder(double theta, double r = 0.3, double h = 1.75)

        {
            //# From scratch:
            //# Consider a cylinder of radius r and height h rotated with respect to the flow direction of a fluid by  about an axis parallel to the base. The frontal area of the cylinder is the area perpendicular to the flow direction. If this shape is projected onto the 2D plane, the resulting 2D area is pi r^2 sin theta + 2 r h cos theta
            //# Test: For a cylinder with r = 1 and h = 2 this should be
            //# A = 2*1 = 2 m^2 for theta = 90°
            //# A = r^2 * PI = 3.14 m^2 for theta = 0°
            //# PI*r*2*sin(B) + 2*r*h*cos*(B)

            // Bolt optimization: Replace Math.Pow(r, 2) with r * r for faster computation
            double r2 = r * r;
            double cyl_surf_area = 2 * r2 * Math.PI + 2 * r * Math.PI * h;

            double diameter = 2 * r;

            return (Math.PI * r2 * Math.Sin(theta) + diameter * h * Math.Cos(theta)) / cyl_surf_area;
        }

        private static double Get_fp(double alt, double az, Posture posture)
        {
            //  This function calculates the projected sunlit fraction (fp)
            //  given a seated or standing posture, a solar altitude, and a
            //  solar horizontal angle relative to the person (SHARP). fp
            //  values are taken from Thermal Comfort, Fanger 1970, Danish
            //  Technical Press.

            //  alt : altitude of sun in degrees [0, 90] Integer
            //  az : azimuth of sun in degrees [0, 180] Integer

            // fix inputs

            if (alt > 90)
            {
                alt = 90;
            }
            if (az > 180)
            {
                az = 360 - az;
            }

            // fix inputs

            var jDim = 7;
            var iDim = 13;

            var fp_table = new double[iDim][];

            for (int ii = 0; ii < iDim; ii++)
            {
                fp_table[ii] = new double[jDim];
            }

            if (posture == Posture.standing || posture == Posture.supine)
            {
                fp_table[0] = new double[] { 0.25, 0.25, 0.23, 0.19, 0.15, 0.10, 0.06 };
                fp_table[1] = new double[] { 0.25, 0.25, 0.23, 0.18, 0.15, 0.10, 0.06 };
                fp_table[2] = new double[] { 0.24, 0.24, 0.22, 0.18, 0.14, 0.10, 0.06 };
                fp_table[3] = new double[] { 0.22, 0.22, 0.20, 0.17, 0.13, 0.09, 0.06 };
                fp_table[4] = new double[] { 0.21, 0.21, 0.18, 0.15, 0.12, 0.08, 0.06 };
                fp_table[5] = new double[] { 0.18, 0.18, 0.17, 0.14, 0.11, 0.08, 0.06 };
                fp_table[6] = new double[] { 0.17, 0.17, 0.16, 0.13, 0.11, 0.08, 0.06 };
                fp_table[7] = new double[] { 0.18, 0.18, 0.16, 0.13, 0.11, 0.08, 0.06 };
                fp_table[8] = new double[] { 0.20, 0.20, 0.18, 0.15, 0.12, 0.08, 0.06 };
                fp_table[9] = new double[] { 0.22, 0.22, 0.20, 0.16, 0.13, 0.09, 0.06 };
                fp_table[10] = new double[] { 0.24, 0.24, 0.21, 0.17, 0.13, 0.09, 0.06 };
                fp_table[11] = new double[] { 0.25, 0.25, 0.22, 0.18, 0.14, 0.09, 0.06 };
                fp_table[12] = new double[] { 0.25, 0.25, 0.22, 0.18, 0.14, 0.09, 0.06 };
            }
            else if (posture == Posture.seating)
            {
                fp_table[0] = new double[] { 0.20, 0.23, 0.21, 0.21, 0.18, 0.16, 0.12 };

                // typo in original code
                fp_table[1] = new double[] { 0.203232, 0.228288, 0.204624, 0.200448, 0.186528, 0.157992, 0.123192 };
                fp_table[2] = new double[] { 0.20, 0.23, 0.21, 0.20, 0.18, 0.15, 0.12 };
                fp_table[3] = new double[] { 0.19, 0.23, 0.20, 0.20, 0.18, 0.15, 0.12 };
                fp_table[4] = new double[] { 0.18, 0.21, 0.19, 0.19, 0.17, 0.14, 0.12 };
                fp_table[5] = new double[] { 0.16, 0.20, 0.18, 0.18, 0.16, 0.13, 0.12 };
                fp_table[6] = new double[] { 0.15, 0.18, 0.17, 0.17, 0.15, 0.13, 0.12 };
                fp_table[7] = new double[] { 0.16, 0.18, 0.16, 0.16, 0.14, 0.13, 0.12 };
                fp_table[8] = new double[] { 0.18, 0.18, 0.16, 0.14, 0.14, 0.12, 0.12 };
                fp_table[9] = new double[] { 0.19, 0.18, 0.15, 0.13, 0.13, 0.12, 0.12 };
                fp_table[10] = new double[] { 0.21, 0.18, 0.14, 0.12, 0.12, 0.12, 0.12 };
                fp_table[11] = new double[] { 0.21, 0.17, 0.13, 0.11, 0.11, 0.12, 0.12 };
                fp_table[12] = new double[] { 0.21, 0.17, 0.12, 0.11, 0.11, 0.11, 0.12 };
            }
            ;

            if (posture == Posture.supine)
            {
                // transpose alt and az for a supine person
                var alt_temp = alt;
                alt = Math.Abs(90 - az);
                az = alt_temp;
            }

            double fp;
            var alt_range = new int[] { 0, 15, 30, 45, 60, 75, 90 };
            var az_range = new int[] { 0, 15, 30, 45, 60, 75, 90, 105, 120, 135, 150, 165, 180 };

            var alt_i = Find_span(alt_range, alt);
            var az_i = Find_span(az_range, az);

            var fp11 = fp_table[az_i][alt_i];
            var fp12 = fp_table[az_i][alt_i + 1];
            var fp21 = fp_table[az_i + 1][alt_i];
            var fp22 = fp_table[az_i + 1][alt_i + 1];

            var az1 = az_range[az_i];
            var az2 = az_range[az_i + 1];
            var alt1 = alt_range[alt_i];
            var alt2 = alt_range[alt_i + 1];

            // bilinear interpolation
            fp = fp11 * (az2 - az) * (alt2 - alt);
            fp += fp21 * (az - az1) * (alt2 - alt);
            fp += fp12 * (az2 - az) * (alt - alt1);
            fp += fp22 * (az - az1) * (alt - alt1);
            fp /= (az2 - az1) * (alt2 - alt1);

            return fp;
        }
    }
}