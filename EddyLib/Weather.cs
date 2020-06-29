using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace EddyLib
{
    public class Weather
    {
        public string epwFilePath;

        public double[] DryBulbTemp;

        public double[] DewPointTemp;

        public double[] RelativeHumidity;

        public double[] Pressure;

        public double[] WindSpeed;

        public int[] WindDirection;

        public double[] DirectNormalRadiation;

        public double[] DiffuseHorizontalRadiation;

        public double[] SkyCover;

        public string Location;

        private double Latitude;

        private double Longitude;

        private double TimeZone;

        //public string KoeppenZone;

        public List<double> SolarElevation = new List<double>();

        public List<double> SolarAzi = new List<double>();

        // constants that should be dealt with later
        //-----------------------

        //double Wst, Hst, BodyA, GrRef;
        public double Wst = 30;

        public double Hst = 30;

        public double BodyA = 0.5;

        public double GrRef = 0.2;

        public object GH_RuntimeMessageLevel { get; private set; }

        public Weather(string filePath)
        {
            try
            {
                // load weather data -----------------
                string[] epwData = File.ReadAllLines(filePath);

                // get header data
                string[] ln1 = epwData[0].Split(',');

                this.Location = System.Text.RegularExpressions.Regex.Replace((ln1[1]), @"\s+", "");
                this.Latitude = Double.Parse(ln1[6]);
                this.Longitude = Double.Parse(ln1[7]);
                this.TimeZone = Double.Parse(ln1[8]);

                // get hourly data
                string[] epwNoHeader = epwData.Skip(8).Take(8760).ToArray(); // new ArraySegment<string>(epwData, 8, 8760).Array;//.ToArray();

                this.epwFilePath = filePath;

                this.DryBulbTemp = epwNoHeader.Select(o => Double.Parse(o.Split(',')[6])).ToArray(); // Dry Bulb Temperature
                this.DewPointTemp = epwNoHeader.Select(o => Double.Parse(o.Split(',')[7])).ToArray(); // Dew Point Temperature
                this.RelativeHumidity = epwNoHeader.Select(o => Double.Parse(o.Split(',')[8])).ToArray(); // Relative Humidity
                this.Pressure = epwNoHeader.Select(o => Double.Parse(o.Split(',')[9])).ToArray(); // Barometric Pressure
                this.WindSpeed = epwNoHeader.Select(o => Double.Parse(o.Split(',')[21])).ToArray(); // WindSpeed
                this.WindDirection = epwNoHeader.Select(o => Int32.Parse(o.Split(',')[20])).ToArray(); // Wind Direction
                this.DirectNormalRadiation = epwNoHeader.Select(o => Double.Parse(o.Split(',')[14])).ToArray(); // Direct Normal Radiation
                this.DiffuseHorizontalRadiation = epwNoHeader.Select(o => Double.Parse(o.Split(',')[15])).ToArray(); // Diffuse Horizontal Illuminance

                //var GlobalHorizontalRadiation = epwNoHeader.Select(o => Double.Parse(o.Split(',')[13])); // Global Horizontal Illuminance
                //var SkyCover = epwNoHeader.Select(o => Double.Parse(o.Split(',')[22])); // Global Horizontal Illuminance

                this.SkyCover = epwNoHeader.Select(o => Double.Parse(o.Split(',')[22])).ToArray(); // SkyCover

                var Yr = epwNoHeader.Select(o => Double.Parse(o.Split(',')[0])).ToArray();
                var Mo = epwNoHeader.Select(o => Double.Parse(o.Split(',')[1])).ToArray();
                var Dy = epwNoHeader.Select(o => Double.Parse(o.Split(',')[2])).ToArray();
                var Hr = epwNoHeader.Select(o => Double.Parse(o.Split(',')[3])).ToArray();
                var DateTime = epwNoHeader.Select(o => o.Split(',')[0] + "." + o.Split(',')[1] + "." + o.Split(',')[2] + " " + o.Split(',')[3]).ToArray();

                Console.WriteLine("Calculating: Solar Geometry");
                var sg = new SolarGeometry();

                for (int i = 0; i < Yr.Length; i++)
                {
                    double _el = sg.solarelevation(Latitude, Longitude, Yr[i], Mo[i], Dy[i], Hr[i], 0, 0, TimeZone, 0);
                    double _az = sg.solarazimuth(Latitude, Longitude, Yr[i], Mo[i], Dy[i], Hr[i], 0, 0, TimeZone, 0);

                    if (_el > 0)
                    {
                        SolarElevation.Add(_el);
                        SolarAzi.Add(_az);
                    }
                    else
                    {
                        SolarElevation.Add(0);
                        SolarAzi.Add(0);
                    }
                }
            }
            catch (Exception e)
            {
                throw e;
            }
        }

        public string ClassifyClimateZone(string epwFilePath, string workingDirToSaveCSV)
        {
            string KC = "";

            string line1Weather = File.ReadLines(epwFilePath).First(); // gets the first line from file.
            double longitude = double.Parse(line1Weather.Split(',')[6]);
            double latitude = double.Parse(line1Weather.Split(',')[7]);

            string filePathKoeppen = workingDirToSaveCSV + @"\Koeppen.csv";

            if (!File.Exists(filePathKoeppen))
            {
                Utilities.DownLoadFile("http://www.rforscience.com/wpmain/wp-content/uploads/2014/06/Koeppen-Geiger-ASCII.txt", filePathKoeppen);
            }

            string[] txt = File.ReadAllLines(filePathKoeppen);

            // Stupid formatting of this file creates 4 columns
            int columnsCnt = 4;
            var Matrix = ArrayHelper.CreateJaggedMatrix(txt.Length, columnsCnt);

            for (int i = 1; i < txt.Length; i++)
            {
                var line = System.Text.RegularExpressions.Regex.Split(txt[i], @"\s{1,}");
                for (int c = 0; c < columnsCnt; c++)
                {
                    // Data is stored in column 1-3, column 0 is empty
                    Matrix[i][c] = line[c];
                }
            }

            string climateClass = "";
            double delta = 0.3;

            // - 1 because of header line; -2 ??

            for (int i = 1; i < txt.Length; i++)
            {
                // Data is stored in column 1-3, column 0 is empty
                if (Math.Abs(longitude - Convert.ToDouble(Matrix[i][1])) < delta && Math.Abs(latitude - Convert.ToDouble(Matrix[i][2])) < delta)
                {
                    climateClass = Convert.ToString(Matrix[i][3]);
                }
            }
            KC = climateClass;

            return KC;
        }
    }
}