using System;
using System.Collections.Generic;
using System.Linq;

namespace EddyLib.Radiance
{
    public class Sky

    {
        private double Sigma = 5.670374419e-8;

        public double[] Emissivity;

        public double[] Temp;

        public double[] HZ_IR;

        private double Kelvin = 273.15;

        public Sky(double[] T_dew, double[] T_DryBulb, double[] SkyCover, double[] RelHum, bool run, double SourceEmissivity = 1)
        {
            if (run)
            {
                var numberOfHours = T_DryBulb.Length;

                this.Emissivity = new double[numberOfHours];
                this.Temp = new double[numberOfHours];
                this.HZ_IR = new double[numberOfHours];

                for (int h = 0; h < numberOfHours; h++)
                {
                    this.Emissivity[h] = CalcEmissivity(T_dew[h], SkyCover[h]);

                    // this.Emissivity[h] = CalcEmissivityEnergyPlus(3, T_dew[h], T_DryBulb[h],SkyCover[h],RelHum[h]);
                    this.HZ_IR[h] = CalcHZ_IR(this.Emissivity[h], this.Sigma, T_DryBulb[h]);
                    this.Temp[h] = CalcTemp(HZ_IR[h], SourceEmissivity);
                }
            }
        }

        private double CalcHZ_IR(double Emissivity, double Sigma, double T_drybulb)
        {
            return Emissivity * Sigma * Math.Pow((T_drybulb + 273.15), 4);
        }

        private double CalcEmissivity(double T_dew, double N)
        {
            // N = SkyCover
            return (0.787 + 0.764 * Math.Log((T_dew + Kelvin) / Kelvin)) * (1 + (0.0224 * N) - (0.0035 * Math.Pow(N, 2)) + (0.00028 * Math.Pow(N, 3)));
        }

        private double CalcEs(double T_celcius)
        {
            //!~ **********************************************
            //!~calculates saturation vapour pressure over water in hPa for input air temperature(ta) in celsius according to:
            //!~Hardy, R.; ITS-90 Formulations for Vapor Pressure, Frostpoint Temperature, Dewpoint Temperature and Enhancement Factors in the Range -100 to 100 °C;
            //!~Proceedings of Third International Symposium on Humidity and Moisture; edited by National Physical Laboratory(NPL), London, 1998, pp. 214-221
            //!~http://www.thunderscientific.com/tech_info/reflibrary/its90formulas.pdf (retrieved 2008-10-01)

            // es = saturation vapour pressure in Pa // T is temperature in K // g is list of
            // coefficients for curve fit

            double T_kelvin; //int I;
            double[] g = {
        -2.8365744E3,
        -6.028076559E3, 1.954263612E1,
        -2.737830188E-2, 1.6261698E-5, 7.0229056E-10,
        -1.8680009E-13, 2.7150305 };

            T_kelvin = T_celcius + 273.15; //! air temp in K double
            var es = g[7] * Math.Log(T_kelvin);

            // do i=0,6
            for (int i = 0; i < 6; i++)
            {
                es = es + g[i] * Math.Pow(T_kelvin, (i - 2));
            }

            //end do

            es = Math.Exp(es) * 0.01; //! *0.01: convert Pa to hPa

            return es;
        }

        private double CalcEmissivityEnergyPlus(int ESkyCalcType, double OSky, double DryBulb, double DewPoint, double RelHum)
        {
            // Calculate Sky Emissivity
            // References:
            // M. Li, Y. Jiang and C. F. M. Coimbra,
            // "On the determination of atmospheric longwave irradiance under all-sky conditions,"
            // Solar Energy 144, 2017, pp. 40–48,
            // G. Clark and C. Allen, "The Estimation of Atmospheric Radiation for Clear and
            // Cloudy Skies," Proc. 2nd National Passive Solar Conference (AS/ISES), 1978, pp. 675-678.

            // var Pvsk = 6.105 * Math.Exp((17.27 * ((double)DryBulb + 273.15) - 4717.03) / (237.7 + (double)DryBulb));

            var TKelvin = 273.15;

            var ESky = 0.0;
            if (ESkyCalcType == 1)
            {
                double PartialPress = RelHum * CalcEs(DryBulb) * 0.01;
                ESky = 0.618 + 0.056 * Math.Pow(PartialPress, 0.5);
            }
            else if (ESkyCalcType == 2)
            {
                double PartialPress = RelHum * CalcEs(DryBulb) * 0.01;
                ESky = 0.685 + 0.000032 * PartialPress * Math.Exp(1699 / (DryBulb + TKelvin));
            }
            else if (ESkyCalcType == 3)
            {
                double TDewC = new List<double>() { DryBulb, DewPoint }.Min();
                ESky = 0.758 + 0.521 * (TDewC / 100) + 0.625 * Math.Pow((TDewC / 100), 2);
            }
            else
            {
                ESky = 0.787 + 0.764 * Math.Log((new List<double>() { DryBulb, DewPoint }.Min() + TKelvin) / TKelvin);
            }
            ESky = ESky * (1 + (0.0224 * OSky) - (0.0035 * Math.Pow(OSky, 2)) + (0.00028 * Math.Pow(OSky, 3)));
            return ESky;
        }

        private double CalcTemp(double HZ_IR, double SourceEmissivity)
        {
            return Math.Pow((HZ_IR / (SourceEmissivity * this.Sigma)), 0.25) - Kelvin;
        }
    }
}