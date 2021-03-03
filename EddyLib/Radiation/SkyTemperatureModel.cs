using System;

namespace EddyLib.Radiation
{
    public class SkyTemperatureModel

    {
        //Real64 const Sigma(5.6697e-8); // Stefan-Boltzmann constant; Taken from E+
        private readonly double Sigma = 5.6697e-8;

        public double[] Emissivity;

        public double[] Temp;

        public double[] HZ_IR;

        private readonly double Kelvin = 273.15;

        public enum CalculationType
        {
            DefaultClarkAllen,

            MartinBerdahl,

            Brunt,

            Idso,
        }

        public SkyTemperatureModel(double[] T_dew, double[] T_DryBulb, double[] OpaqueSkyCover, double[] RelHum, bool run, CalculationType type, double[] HZ_IR_EPW = null)
        {
            if (run)
            {
                var numberOfHours = T_DryBulb.Length;

                this.Emissivity = new double[numberOfHours];
                this.Temp = new double[numberOfHours];
                this.HZ_IR = new double[numberOfHours];

                if (HZ_IR_EPW != null && HZ_IR_EPW[0] <= 9999.0)
                {
                    this.HZ_IR = HZ_IR_EPW;

                    for (int h = 0; h < numberOfHours; h++)
                    {
                        this.Temp[h] = CalcTempValidIR(HZ_IR[h]);
                    }
                }
                else
                {
                    for (int h = 0; h < numberOfHours; h++)
                    {
                        this.Emissivity[h] = CalcEmissivityEnergyPlus(OpaqueSkyCover[h], T_DryBulb[h], T_dew[h], RelHum[h], type);
                        this.Temp[h] = CalcTemp(T_DryBulb[h], this.Emissivity[h]);
                    }
                }
            }
        }

        private double CalcHZ_IR(double Emissivity, double Sigma, double T_drybulb)
        {
            return Emissivity * Sigma * Math.Pow((T_drybulb + Kelvin), 4);
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

            T_kelvin = T_celcius + Kelvin; //! air temp in K double
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

        private double CalcEmissivityEnergyPlus(double OSky, double DryBulb, double DewPoint, double RelHum, CalculationType type)
        {
            // https://bigladdersoftware.com/epx/docs/9-3/engineering-reference/climate-calculations.html
            // "EnergyPlus\WeatherManager.cc" Line 3353

            // Calculate Sky Emissivity
            // References:
            // M. Li, Y. Jiang and C. F. M. Coimbra,
            // "On the determination of atmospheric longwave irradiance under all-sky conditions,"
            // Solar Energy 144, 2017, pp. 40–48,
            // G. Clark and C. Allen, "The Estimation of Atmospheric Radiation for Clear and
            // Cloudy Skies," Proc. 2nd National Passive Solar Conference (AS/ISES), 1978, pp. 675-678.

            // var Pvsk = 6.105 * Math.Exp((17.27 * ((double)DryBulb + 273.15) - 4717.03) / (237.7 + (double)DryBulb));

            var TKelvin = Kelvin;

            var ESky = 0.0;
            if (type == CalculationType.Brunt)
            {
                double PartialPress = RelHum * CalcEs(DryBulb) * 0.01;
                ESky = 0.618 + 0.056 * Math.Pow(PartialPress, 0.5);
            }
            else if (type == CalculationType.Idso)
            {
                double PartialPress = RelHum * CalcEs(DryBulb) * 0.01;
                ESky = 0.685 + 0.000032 * PartialPress * Math.Exp(1699 / (DryBulb + TKelvin));
            }
            else if (type == CalculationType.MartinBerdahl)
            {
                double TDewC = Math.Min(DryBulb, DewPoint);
                ESky = 0.758 + 0.521 * (TDewC / 100) + 0.625 * Math.Pow((TDewC / 100), 2);
            }

            // default
            else if (type == CalculationType.DefaultClarkAllen)
            {
                ESky = 0.787 + 0.764 * Math.Log((Math.Min(DryBulb, DewPoint) + TKelvin) / TKelvin);
            }
            ESky = ESky * (1 + (0.0224 * OSky) - (0.0035 * Math.Pow(OSky, 2)) + (0.00028 * Math.Pow(OSky, 3)));

            return ESky;
        }

        private double CalcTempValidIR(double HZ_IR)
        {
            // "EnergyPlus\WeatherManager.cc" Line 3353
            return Math.Pow((HZ_IR / this.Sigma), 0.25) - Kelvin;
        }

        private double CalcTemp(double DryBulb, double ESky)
        {
            // "EnergyPlus\WeatherManager.cc" Line 3353
            return (DryBulb + Kelvin) * Math.Pow(ESky, 0.25) - Kelvin;
        }
    }
}