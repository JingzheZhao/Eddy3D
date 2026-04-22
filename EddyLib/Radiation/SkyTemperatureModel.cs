using System;

namespace EddyLib.Radiation
{
    /// <summary>
    /// Calculates sky temperature and emissivity using various models.
    /// Based on EnergyPlus WeatherManager implementation.
    /// </summary>
    public class SkyTemperatureModel
    {
        #region Physical Constants

        /// <summary>Stefan-Boltzmann constant (W/m²·K⁴).</summary>
        private const double StefanBoltzmann = 5.6697e-8;

        /// <summary>Kelvin offset for Celsius conversion.</summary>
        private const double KelvinOffset = 273.15;

        /// <summary>Missing data indicator in EPW files.</summary>
        private const double MissingDataValue = 9999.0;

        #endregion

        #region Results

        /// <summary>Hourly sky emissivity values.</summary>
        public double[] Emissivity { get; private set; }

        /// <summary>Hourly sky temperature values (°C).</summary>
        public double[] Temperature { get; private set; }

        /// <summary>Hourly horizontal infrared radiation (W/m²).</summary>
        public double[] HorizontalIR { get; private set; }

        #endregion

        /// <summary>
        /// Sky temperature calculation method.
        /// </summary>
        public enum CalculationType
        {
            /// <summary>Clark and Allen (1978) - default in EnergyPlus.</summary>
            DefaultClarkAllen,

            /// <summary>Martin and Berdahl model - uses dew point.</summary>
            MartinBerdahl,

            /// <summary>Brunt model - uses partial pressure.</summary>
            Brunt,

            /// <summary>Idso model - uses partial pressure.</summary>
            Idso,
        }

        /// <summary>
        /// Creates sky temperature model from weather data.
        /// </summary>
        public SkyTemperatureModel(
            double[] dewPointTemp,
            double[] dryBulbTemp,
            double[] opaqueSkyCover,
            double[] relativeHumidity,
            bool run,
            CalculationType calculationType,
            double[] horizontalIR_EPW = null)
        {
            if (!run) return;

            int hourCount = dryBulbTemp.Length;
            Emissivity = new double[hourCount];
            Temperature = new double[hourCount];
            HorizontalIR = new double[hourCount];

            // If valid IR data exists in EPW, use it directly
            if (horizontalIR_EPW != null && horizontalIR_EPW[0] <= MissingDataValue)
            {
                HorizontalIR = horizontalIR_EPW;
                for (int h = 0; h < hourCount; h++)
                {
                    Temperature[h] = CalculateTempFromIR(HorizontalIR[h]);
                }
            }
            else
            {
                for (int h = 0; h < hourCount; h++)
                {
                    // Fall back to simpler model if sky cover data is missing
                    var effectiveType = (opaqueSkyCover[h] <= 0 || opaqueSkyCover[h] > 10)
                        ? CalculationType.MartinBerdahl
                        : calculationType;

                    Emissivity[h] = CalculateEmissivity(
                        opaqueSkyCover[h],
                        dryBulbTemp[h],
                        dewPointTemp[h],
                        relativeHumidity[h],
                        effectiveType);

                    Temperature[h] = CalculateSkyTemp(dryBulbTemp[h], Emissivity[h]);
                }
            }
        }

        #region Private Calculation Methods

        /// <summary>
        /// Calculates horizontal IR radiation from emissivity and temperature.
        /// </summary>
        private double CalculateHorizontalIR(double emissivity, double dryBulbC)
        {
            double tempK = dryBulbC + KelvinOffset;
            double tempK2 = tempK * tempK;
            return emissivity * StefanBoltzmann * (tempK2 * tempK2);
        }

        /// <summary>
        /// Calculates saturation vapor pressure using Hardy (1998) formula.
        /// </summary>
        /// <remarks>
        /// Reference: Hardy, R.; ITS-90 Formulations for Vapor Pressure
        /// http://www.thunderscientific.com/tech_info/reflibrary/its90formulas.pdf
        /// </remarks>
        private static double CalculateSaturationVaporPressure(double tempC)
        {
            // Coefficients for ITS-90 curve fit
            double[] g = {
                -2.8365744E3,
                -6.028076559E3,
                1.954263612E1,
                -2.737830188E-2,
                1.6261698E-5,
                7.0229056E-10,
                -1.8680009E-13,
                2.7150305
            };

            double tempK = tempC + KelvinOffset;
            double es = g[7] * Math.Log(tempK);

            double tempK_inv = 1.0 / tempK;
            double tempK_inv2 = tempK_inv * tempK_inv;

            es += g[0] * tempK_inv2;           // i=0: tempK^-2
            es += g[1] * tempK_inv;            // i=1: tempK^-1
            es += g[2];                        // i=2: tempK^0 = 1
            es += g[3] * tempK;                // i=3: tempK^1
            double tempK2 = tempK * tempK;
            es += g[4] * tempK2;               // i=4: tempK^2
            es += g[5] * (tempK2 * tempK);     // i=5: tempK^3
            es += g[6] * (tempK2 * tempK2);    // i=6: tempK^4

            // Convert Pa to hPa
            return Math.Exp(es) * 0.01;
        }

        /// <summary>
        /// Calculates sky emissivity using selected model.
        /// Based on EnergyPlus WeatherManager.cc Line 3353.
        /// </summary>
        private double CalculateEmissivity(
            double skyCover,
            double dryBulbC,
            double dewPointC,
            double relHumidity,
            CalculationType type)
        {
            double emissivity = type switch
            {
                CalculationType.Brunt => CalculateBruntEmissivity(dryBulbC, relHumidity),
                CalculationType.Idso => CalculateIdsoEmissivity(dryBulbC, relHumidity),
                CalculationType.MartinBerdahl => CalculateMartinBerdahlEmissivity(dryBulbC, dewPointC),
                _ => CalculateClarkAllenEmissivity(dryBulbC, dewPointC) // DefaultClarkAllen
            };

            // Apply cloud correction factor
            double skyCover2 = skyCover * skyCover;
            return emissivity * (1 + 0.0224 * skyCover - 0.0035 * skyCover2 + 0.00028 * (skyCover2 * skyCover));
        }

        private double CalculateBruntEmissivity(double dryBulbC, double relHumidity)
        {
            double partialPressure = relHumidity * CalculateSaturationVaporPressure(dryBulbC) * 0.01;
            return 0.618 + 0.056 * Math.Sqrt(partialPressure);
        }

        private double CalculateIdsoEmissivity(double dryBulbC, double relHumidity)
        {
            double partialPressure = relHumidity * CalculateSaturationVaporPressure(dryBulbC) * 0.01;
            double tempK = dryBulbC + KelvinOffset;
            return 0.685 + 0.000032 * partialPressure * Math.Exp(1699.0 / tempK);
        }

        private double CalculateMartinBerdahlEmissivity(double dryBulbC, double dewPointC)
        {
            double dewC = Math.Min(dryBulbC, dewPointC);
            double ratio = dewC / 100.0;
            return 0.758 + 0.521 * ratio + 0.625 * ratio * ratio;
        }

        private double CalculateClarkAllenEmissivity(double dryBulbC, double dewPointC)
        {
            double dewK = Math.Min(dryBulbC, dewPointC) + KelvinOffset;
            return 0.787 + 0.764 * Math.Log(dewK / KelvinOffset);
        }

        /// <summary>
        /// Calculates sky temperature from horizontal IR radiation.
        /// </summary>
        private double CalculateTempFromIR(double horizontalIR)
        {
            return Math.Sqrt(Math.Sqrt(horizontalIR / StefanBoltzmann)) - KelvinOffset;
        }

        /// <summary>
        /// Calculates effective sky temperature from dry bulb and emissivity.
        /// </summary>
        private static double CalculateSkyTemp(double dryBulbC, double emissivity)
        {
            double tempK = dryBulbC + KelvinOffset;
            return tempK * Math.Sqrt(Math.Sqrt(emissivity)) - KelvinOffset;
        }

        #endregion

        #region Backward Compatibility

        /// <summary>Legacy property - use Temperature instead.</summary>
        public double[] Temp
        {
            get => Temperature;
            set => Temperature = value;
        }

        /// <summary>Legacy property - use HorizontalIR instead.</summary>
        public double[] HZ_IR
        {
            get => HorizontalIR;
            set => HorizontalIR = value;
        }

        #endregion
    }
}