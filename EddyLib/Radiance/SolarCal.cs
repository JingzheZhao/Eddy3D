using System;

namespace EddyLib.Radiance
{
    public class SolarGain
    {
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

        //public double DeltaMRT;

        //public double ERF;

        //public SolarGain(
        //  double sol_altitude,
        //  double sol_azimuth,
        //  double sol_radiation_dir,
        //  double sol_transmittance,
        //  double f_svv,
        //  double f_bes,
        //  double asw = 0.7,
        //  string posture = "seated",
        //  double floor_reflectance = 0.6)
        //{
        //    posture = posture.ToLower();
        //    if (!new List<string> {
        //  "standing",
        //  "supine",
        //  "seated"
        //  }.Contains(posture))
        //    {
        //        Exception e = new Exception("Posture has to be either standing, supine or seated");
        //    }

        //    var deg_to_rad = 0.0174532925;
        //    var hr = 6;
        //    var i_diff = 0.2 * sol_radiation_dir;

        //    /// Edit Patrick
        //    /// Break when larger than 180

        //    if (sol_azimuth > 180.0)
        //    {
        //        sol_azimuth = 360.0 - sol_azimuth;
        //    }

        //    /// Edit Patrick

        //    var fp_table = new List<List<double>> {
        //    new List<double> {  0.25,  0.25,  0.23, 0.19, 0.15, 0.1,  0.06 }, new List<double> { 0.25, 0.25, 0.23, 0.18, 0.15, 0.1, 0.06 }, new List<double> { 0.24, 0.24, 0.22, 0.18, 0.14, 0.1, 0.06 }, new List<double> { 0.22, 0.22, 0.2, 0.17, 0.13, 0.09, 0.06 }, new List<double> { 0.21, 0.21, 0.18, 0.15, 0.12, 0.08, 0.06 }, new List<double> { 0.18, 0.18, 0.17, 0.14, 0.11, 0.08, 0.06 }, new List<double> { 0.17, 0.17, 0.16, 0.13, 0.11, 0.08, 0.06 }, new List<double> { 0.18, 0.18, 0.16, 0.13, 0.11, 0.08, 0.06 }, new List<double> { 0.2, 0.2, 0.18, 0.15, 0.12, 0.08, 0.06 }, new List<double> { 0.22, 0.22, 0.2, 0.16, 0.13, 0.09, 0.06 }, new List<double> { 0.24, 0.24, 0.21, 0.17, 0.13, 0.09, 0.06 }, new List<double> { 0.25, 0.25, 0.22, 0.18, 0.14, 0.09, 0.06 }, new List<double> { 0.25, 0.25, 0.22, 0.18, 0.14, 0.09, 0.06}
        //    };
        //    if (posture == "seated")
        //    {
        //        fp_table = new List<List<double>>{
        //      new List<double> { 0.2, 0.23, 0.21, 0.21, 0.18, 0.16, 0.12 }, new List<double> { 0.2, 0.23, 0.2, 0.2, 0.19, 0.16, 0.12 }, new List<double> { 0.2, 0.23, 0.21, 0.2, 0.18, 0.15, 0.12 }, new List<double> { 0.19, 0.23, 0.2, 0.2, 0.18, 0.15, 0.12 }, new List<double> { 0.18, 0.21, 0.19, 0.19, 0.17, 0.14, 0.12 }, new List<double> { 0.16, 0.2, 0.18, 0.18, 0.16, 0.13, 0.12 }, new List<double> { 0.15, 0.18, 0.17, 0.17, 0.15, 0.13, 0.12 }, new List<double> { 0.16, 0.18, 0.16, 0.16, 0.14, 0.13, 0.12 }, new List<double> { 0.18, 0.18, 0.16, 0.14, 0.14, 0.12, 0.12 }, new List<double> { 0.19, 0.18, 0.15, 0.13, 0.13, 0.12, 0.12 }, new List<double> { 0.21, 0.18, 0.14, 0.12, 0.12, 0.12, 0.12 }, new List<double> { 0.21, 0.17, 0.13, 0.11, 0.11, 0.12, 0.12 }, new List<double> { 0.21, 0.17, 0.12, 0.11, 0.11, 0.11, 0.12 }
        //      };
        //    }
        //    if (posture == "supine")
        //    {
        //        var alt_temp = sol_altitude;
        //        sol_altitude = Math.Abs(90 - sol_azimuth);
        //        sol_azimuth = alt_temp;
        //    }
        //    var alt_range = new List<int> {
        //    0, 15, 30, 45, 60, 75, 90
        //    };
        //    var az_range = new List<int> {
        //    0, 15, 30, 45, 60, 75, 90, 105, 120, 135, 150, 165, 180
        //    };
        //    var alt_i = Find_span(alt_range, sol_altitude);
        //    var az_i = Find_span(az_range, sol_azimuth);
        //    double fp11 = fp_table[az_i][alt_i];
        //    double fp12 = fp_table[az_i][alt_i + 1];
        //    double fp21 = fp_table[az_i + 1][alt_i];
        //    double fp22 = fp_table[az_i + 1][alt_i + 1];
        //    var az1 = az_range[az_i];
        //    var az2 = az_range[az_i + 1];
        //    var alt1 = alt_range[alt_i];
        //    var alt2 = alt_range[alt_i + 1];
        //    var fp = fp11 * (az2 - sol_azimuth) * (alt2 - sol_altitude);
        //    fp += fp21 * (sol_azimuth - az1) * (alt2 - sol_altitude);
        //    fp += fp12 * (az2 - sol_azimuth) * (sol_altitude - alt1);
        //    fp += fp22 * (sol_azimuth - az1) * (sol_altitude - alt1);
        //    fp /= (az2 - az1) * (alt2 - alt1);
        //    var f_eff = 0.725;
        //    if (posture == "seated")
        //    {
        //        f_eff = 0.696;
        //    }
        //    var sw_abs = asw;
        //    var lw_abs = 0.95;
        //    var e_diff = f_eff * f_svv * 0.5 * sol_transmittance * i_diff;
        //    var e_direct = fp * sol_transmittance * f_bes * sol_radiation_dir;
        //    var e_refl = f_eff * f_svv * 0.5 * sol_transmittance * (sol_radiation_dir * Math.Sin(sol_altitude * deg_to_rad) + i_diff) * floor_reflectance;
        //    var e_solar = e_diff + e_direct + e_refl;
        //    var erf = e_solar * (sw_abs / lw_abs);
        //    var d_mrt = erf / (hr * f_eff);

        //    this.ERF = Math.Round(erf, 1);
        //    this.DeltaMRT = Math.Round(d_mrt, 1);

        //    // print(fp, e_diff, e_direct, e_refl, e_solar, erf, d_mrt)
        //}

        //public static int Find_span(List<int> arr, double x)

        //{
        //    for (int i = 0; i < arr.Count - 1; i++)
        //    {
        //        if (arr[i + 1] >= x && x >= arr[i])
        //        {
        //            return i;
        //        }
        //    }

        //    // Last element in Python, arr.Count in C#
        //    return arr.Count;
        //}

        public enum Posture
        {
            supine,

            seating,

            standing
        };

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

            var feff = 0.0;

            if (posture == Posture.standing || posture == Posture.supine)
            {
                feff = 0.725;
            }

            // (posture == Posture.seating)
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
            };

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

        //private function get_fp_real(alt, az, posture)
        //{
        //    //  alt : altitude of sun in degrees [0, 90] Integer
        //    //  az : azimuth of sun in degrees [0, 180] Integer

        //    if (posture == 'standing' | posture == 'supine')
        //    {
        //        var fp_table = [[0.25375, 0.25375, 0.22765, 0.18705, 0.14935, 0.1044, 0.05945],

        //            [0.24795, 0.24795, 0.22475, 0.1827, 0.145, 0.1015, 0.05945],

        //            [0.23925, 0.23925, 0.2175, , 0.1769, 0.13775, 0.0957, 0.05945],

        //            [0.22475, 0.22475, 0.199375, 0.1653, 0.126875, 0.0899, 0.05945],

        //            [0.205175, 0.205175, 0.181975, 0.1508, , 0.116, 0.08265, 0.05945],

        //            [0.1827, 0.1827, 0.1653, 0.1363, 0.10875, 0.0783, 0.05945],

        //            [0.16675, 0.16675, 0.15515, 0.1305, 0.1073, 0.0783, 0.05945],

        //            [0.17545, 0.17545, 0.16095, 0.1305, 0.110925, 0.0812, 0.05945],

        //            [0.19865, 0.19865, 0.177625, 0.147175, 0.119625, 0.0841, 0.05945],

        //            [0.2204, 0.2204, 0.19575, 0.1595, 0.12615, 0.087725, 0.05945],

        //            [0.2378, 0.2378, 0.21025, 0.16965, 0.132675, 0.090625, 0.05945],

        //            [0.2494, 0.2494, 0.2204, 0.1769, 0.13775, 0.0928, 0.05945],

        //            [0.251575, 0.251575, 0.2233, 0.17835, 0.138475, 0.0928, 0.05945]];
        //    }
        //    else if (posture == 'seated')
        //    {
        //        var fp_table = [[0.20184, 0.225504, 0.21228, 0.210888, 0.182352, 0.155904, 0.123192],

        //            [0.203232, 0.228288, 0.204624, 0.200448, 0.186528, 0.157992, 0.123192],

        //            [0.200448, 0.231072, 0.207408, 0.20184, 0.183744, 0.154512, 0.123192],

        //            [0.190704, 0.226896, 0.204624, 0.201144, 0.175392, 0.148944, 0.123192],

        //            [0.176784, 0.214368, 0.19488, 0.192096, 0.167736, 0.140592, 0.123192],

        //            [0.16008, 0.196272, 0.182352, 0.18096, 0.162168, 0.134328, 0.123192],

        //            [0.150336, 0.18096, 0.172608, 0.169824, 0.15312, 0.129456, 0.123192],

        //            [0.162864, 0.179568, 0.164256, 0.157992, 0.144768, 0.12528, 0.123192],

        //            [0.182352, 0.18096, 0.155904, 0.144768, 0.136416, 0.122496, 0.123192],

        //            [0.19488, 0.18096, 0.14616, 0.133632, 0.128064, 0.11832, 0.123192],

        //            [0.207408, 0.178176, 0.135024, 0.121104, 0.116928, 0.116928, 0.123192],

        //            [0.212976, 0.174, 0.12528, 0.108576, 0.108576, 0.115536, 0.123192],

        //            [0.2088, 0.16704, 0.116928, 0.105792, 0.105792, 0.114144, 0.123192]];
        //    }

        //    if (posture == 'supine')
        //    {
        //        // transpose alt and az for a supine person
        //        alt_temp = alt;
        //        alt = Math.abs(90 - az)
        //        az = alt_temp;
        //    }

        //    var fp;
        //    var alt_range = [0, 15, 30, 45, 60, 75, 90];
        //    var az_range = [0, 15, 30, 45, 60, 75, 90, 105, 120, 135, 150, 165, 180];

        //    var alt_i = find_span(alt_range, alt);
        //    var az_i = find_span(az_range, az);

        //    var fp11 = fp_table[az_i][alt_i];
        //    var fp12 = fp_table[az_i][alt_i + 1];
        //    var fp21 = fp_table[az_i + 1][alt_i];
        //    var fp22 = fp_table[az_i + 1][alt_i + 1];

        //    var az1 = az_range[az_i];
        //    var az2 = az_range[az_i + 1];
        //    var alt1 = alt_range[alt_i];
        //    var alt2 = alt_range[alt_i + 1];

        //    // bilinear interpolation
        //    fp = fp11 * (az2 - az) * (alt2 - alt);
        //    fp += fp21 * (az - az1) * (alt2 - alt);
        //    fp += fp12 * (az2 - az) * (alt - alt1);
        //    fp += fp22 * (az - az1) * (alt - alt1);
        //    fp /= (az2 - az1) * (alt2 - alt1);

        //    return fp;
        //}
    }
}