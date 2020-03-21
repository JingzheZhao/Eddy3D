using System;
using System.Collections.Generic;

namespace EddyLib.Radiance
{
    public class SolarGain
    {
        // https://github.com/CenterForTheBuiltEnvironment/pythermalcomfort/blob/master/src/pythermalcomfort/models.py

        //
        //         Calculates the solar gain to the human body using the Effective Radiant Field (ERF) [1]_. The ERF is a measure of the net energy flux to or from the human body.
        //         ERF is expressed in W over human body surface area [w/m2]. In addition, it calculates the delta mean radiant temperature. Which is the amount by which the mean radiant
        //         temperature of the space should be increased if no solar radiation is present.
        //         Parameters
        //         ----------
        //         sol_altitude : float
        //             Solar altitude, degrees from horizontal [deg]. Ranges between 0 and 90.
        //         sol_azimuth : float
        //             Solar azimuth, degrees clockwise from North [deg]. Ranges between 0 and 180.
        //         posture : str
        //             Default 'seated' list of available options 'standing', 'supine' or 'seated'
        //         sol_radiation_dir : float
        //             Direct-beam solar radiation, [W/m2]. Ranges between 200 and 1000. See Table C2-3 of ASHRAE 55 2017 [1]_.
        //         sol_transmittance : float
        //             Total solar transmittance, ranges from 0 to 1. The total solar transmittance of window systems, including glazing unit, blinds, and other façade treatments, shall be determined using one of the following methods:
        //             i) Provided by manufacturer or from the National Fenestration Rating Council approved Lawrence Berkeley National Lab International Glazing Database.
        //             ii) Glazing unit plus venetian blinds or other complex or unique shades shall be calculated using National Fenestration Rating Council approved software or Lawrence Berkeley National Lab Complex Glazing Database.
        //         f_svv : float
        //             Fraction of sky vault exposed to body, ranges from 0 to 1.
        //         f_bes : float
        //             Fraction of the possible body surface exposed to sun, ranges from 0 to 1. See Table C2-2 and equation C-7 ASHRAE 55 2017 [1]_.
        //         asw: float
        //             The average short-wave absorptivity of the occupant. It will range widely, depending on the color of the occupant’s skin as well as the color and amount of clothing covering the body.
        //             A value of 0.7 shall be used unless more specific information about the clothing or skin color of the occupants is available.
        //             Note: Short-wave absorptivity typically ranges from 0.57 to 0.84, depending on skin and clothing color. More information is available in Blum (1945).
        //         floor_reflectance: float
        //             Floor refectance. It is assumed to be constant and equal to 0.6.
        //         Notes
        //         -----
        //         More information on the calculation procedure can be found in Appendix C of [1]_.
        //         Returns
        //         -------
        //         erf: float
        //             Solar gain to the human body using the Effective Radiant Field [W/m2]
        //         delta_mrt: float
        //             Delta mean radiant temperature. The amount by which the mean radiant temperature of the space should be increased if no solar radiation is present.
        //         Examples
        //         --------
        //         .. code-block:: python
        //             >>> from pythermalcomfort.models import solar_gain
        //             >>> results = solar_gain(sol_altitude=0, sol_azimuth=120, sol_radiation_dir=800, sol_transmittance=0.5, f_svv=0.5, f_bes=0.5, asw=0.7, posture='seated')
        //             >>> print(results)
        //             {'erf': 42.9, 'delta_mrt': 10.3}
        //

        public double DeltaMRT;

        public double ERF;

        public SolarGain(
          double sol_altitude,
          double sol_azimuth,
          double sol_radiation_dir,
          double sol_transmittance,
          double f_svv,
          double f_bes,
          double asw = 0.7,
          string posture = "seated",
          double floor_reflectance = 0.6)
        {
            posture = posture.ToLower();
            if (!new List<string> {
          "standing",
          "supine",
          "seated"
          }.Contains(posture))
            {
                Exception e = new Exception("Posture has to be either standing, supine or seated");
            }

            var deg_to_rad = 0.0174532925;
            var hr = 6;
            var i_diff = 0.2 * sol_radiation_dir;
            var fp_table = new List<List<double>> {
          new List<double> {            0.25,            0.25,            0.23,            0.19,            0.15,            0.1,                0.06 }, new List<double> { 0.25, 0.25, 0.23, 0.18, 0.15, 0.1, 0.06                  }, new List<double> { 0.24, 0.24, 0.22, 0.18, 0.14, 0.1, 0.06 }, new List<double> { 0.22, 0.22, 0.2, 0.17, 0.13, 0.09, 0.06 }, new List<double> { 0.21, 0.21, 0.18, 0.15, 0.12, 0.08, 0.06 }, new List<double> { 0.18, 0.18, 0.17, 0.14, 0.11, 0.08, 0.06 }, new List<double> { 0.17, 0.17, 0.16, 0.13, 0.11, 0.08, 0.06 }, new List<double> { 0.18, 0.18, 0.16, 0.13, 0.11, 0.08, 0.06 }, new List<double> { 0.2, 0.2, 0.18, 0.15, 0.12, 0.08, 0.06 }, new List<double> { 0.22, 0.22, 0.2, 0.16, 0.13, 0.09, 0.06 }, new List<double> { 0.24, 0.24, 0.21, 0.17, 0.13, 0.09, 0.06 }, new List<double> { 0.25, 0.25, 0.22, 0.18, 0.14, 0.09, 0.06 }, new List<double> { 0.25, 0.25, 0.22, 0.18, 0.14, 0.09, 0.06
            }
          };
            if (posture == "seated")
            {
                fp_table = new List<List<double>>{
            new List<double> { 0.2, 0.23, 0.21, 0.21, 0.18, 0.16, 0.12 }, new List<double> { 0.2, 0.23, 0.2, 0.2, 0.19, 0.16, 0.12 }, new List<double> { 0.2, 0.23, 0.21, 0.2, 0.18, 0.15, 0.12 }, new List<double> { 0.19, 0.23, 0.2, 0.2, 0.18, 0.15, 0.12 }, new List<double> { 0.18, 0.21, 0.19, 0.19, 0.17, 0.14, 0.12 }, new List<double> { 0.16, 0.2, 0.18, 0.18, 0.16, 0.13, 0.12 }, new List<double> { 0.15, 0.18, 0.17, 0.17, 0.15, 0.13, 0.12 }, new List<double> { 0.16, 0.18, 0.16, 0.16, 0.14, 0.13, 0.12 }, new List<double> { 0.18, 0.18, 0.16, 0.14, 0.14, 0.12, 0.12 }, new List<double> { 0.19, 0.18, 0.15, 0.13, 0.13, 0.12, 0.12 }, new List<double> { 0.21, 0.18, 0.14, 0.12, 0.12, 0.12, 0.12 }, new List<double> { 0.21, 0.17, 0.13, 0.11, 0.11, 0.12, 0.12 }, new List<double> { 0.21, 0.17, 0.12, 0.11, 0.11, 0.11, 0.12
              }
            };
            }
            if (posture == "supine")
            {
                var alt_temp = sol_altitude;
                sol_altitude = Math.Abs(90 - sol_azimuth);
                sol_azimuth = alt_temp;
            }
            var alt_range = new List<int> {
          0, 15, 30, 45, 60, 75, 90
          };
            var az_range = new List<int> {
          0, 15, 30, 45, 60, 75, 90, 105, 120, 135, 150, 165, 180
          };
            var alt_i = Find_span(alt_range, sol_altitude);
            var az_i = Find_span(az_range, sol_azimuth);
            double fp11 = fp_table[az_i][alt_i];
            double fp12 = fp_table[az_i][alt_i + 1];
            double fp21 = fp_table[az_i + 1][alt_i];
            double fp22 = fp_table[az_i + 1][alt_i + 1];
            var az1 = az_range[az_i];
            var az2 = az_range[az_i + 1];
            var alt1 = alt_range[alt_i];
            var alt2 = alt_range[alt_i + 1];
            var fp = fp11 * (az2 - sol_azimuth) * (alt2 - sol_altitude);
            fp += fp21 * (sol_azimuth - az1) * (alt2 - sol_altitude);
            fp += fp12 * (az2 - sol_azimuth) * (sol_altitude - alt1);
            fp += fp22 * (sol_azimuth - az1) * (sol_altitude - alt1);
            fp /= (az2 - az1) * (alt2 - alt1);
            var f_eff = 0.725;
            if (posture == "seated")
            {
                f_eff = 0.696;
            }
            var sw_abs = asw;
            var lw_abs = 0.95;
            var e_diff = f_eff * f_svv * 0.5 * sol_transmittance * i_diff;
            var e_direct = fp * sol_transmittance * f_bes * sol_radiation_dir;
            var e_refl = f_eff * f_svv * 0.5 * sol_transmittance * (sol_radiation_dir * Math.Sin(sol_altitude * deg_to_rad) + i_diff) * floor_reflectance;
            var e_solar = e_diff + e_direct + e_refl;
            var erf = e_solar * (sw_abs / lw_abs);
            var d_mrt = erf / (hr * f_eff);

            this.ERF = Math.Round(erf, 1);
            this.DeltaMRT = Math.Round(d_mrt, 1);

            // print(fp, e_diff, e_direct, e_refl, e_solar, erf, d_mrt)
        }

        public static int Find_span(List<int> arr, double x)

        {
            for (int i = 0; i < arr.Count; i++)
            {
                if (arr[i + 1] >= x && x >= arr[i])
                {
                    return i;
                }
            }
            return -1;
        }
    }
}