using ProtoBuf;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace EddyLib
{

    [ProtoContract]
    public class Weather
    {

        public Weather() { }

        [ProtoMember(1)]

        public string epwFilePath;
        [ProtoMember(2)]

        public double[] DryBulbTemp;
        [ProtoMember(3)]

        public double[] DewPointTemp;
        [ProtoMember(4)]

        public double[] RelativeHumidity;
        [ProtoMember(5)]

        public double[] Pressure;
        [ProtoMember(6)]

        public double[] WindSpeed;
        [ProtoMember(7)]

        public int[] WindDirection;
        [ProtoMember(8)]

        public double[] DirectNormalRadiation;
        [ProtoMember(9)]

        public double[] DiffuseHorizontalRadiation;
        [ProtoMember(10)]

        public double[] HorRadiation;
        [ProtoMember(11)]

        public double[] NormalRadiation;
        [ProtoMember(12)]

        public double[] SkyRadiation;
        [ProtoMember(13)]

        public double[] GHorRadiation;
        [ProtoMember(14)]

        public double[] TotalSkyCover;
        [ProtoMember(15)]

        public double[] OpaqSkyCover;
        [ProtoMember(16)]

        public string Location;
        [ProtoMember(17)]

        private double Latitude;
        [ProtoMember(18)]

        private double Longitude;
        [ProtoMember(19)]

        private double TimeZone;

        [ProtoMember(20)]

        public List<double> SolarElevation = new List<double>();
        [ProtoMember(21)]

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

                //epw.Year = csv.GetField<int>(0);
                //epw.Month = csv.GetField<int>(1);
                //epw.Day = csv.GetField<int>(2);
                //epw.Hour = csv.GetField<int>(3);
                //epw.Minute = csv.GetField<int>(4);
                //epw.UncertaintyFlag = csv.GetField<string>(5);
                //epw.DB = csv.GetField<decimal>(6);
                //epw.WB = csv.GetField<decimal>(7);
                //epw.RH = csv.GetField<decimal>(8);
                //epw.Pressure = csv.GetField<decimal>(9);
                //epw.HorRadiation = csv.GetField<decimal>(10);
                //epw.NormalRadiation = csv.GetField<decimal>(11);
                //epw.SkyRadiation = csv.GetField<decimal>(12);
                //epw.GHorRadiation = csv.GetField<decimal>(13);
                //epw.DirectNormalRadiation = csv.GetField<decimal>(14);
                //epw.DiffuseHorizontalRadiation = csv.GetField<decimal>(15);
                //epw.GHorIllumination = csv.GetField<decimal>(16);
                //epw.DirectNormalIllumination = csv.GetField<decimal>(17);
                //epw.DiffuseHorizontalIllumination = csv.GetField<decimal>(18);
                //epw.ZenithIllumination = csv.GetField<decimal>(19);
                //epw.WindDirection = csv.GetField<decimal>(20);
                //epw.WindSpeed = csv.GetField<decimal>(21);
                //epw.TotalSkyCover = csv.GetField<decimal>(22);
                //epw.OpaqSkyCover = csv.GetField<decimal>(23);
                //epw.Visibility = csv.GetField<decimal>(24);
                //epw.FieldCeilHeight = csv.GetField<decimal>(25);
                //epw.WeatherObserv = csv.GetField<decimal>(26);
                //epw.WeatherCodes = csv.GetField<decimal>(27);
                //epw.PrecipitationWater = csv.GetField<decimal>(28);
                //epw.AerosolOptical = csv.GetField<decimal>(29);
                //epw.SnowDepth = csv.GetField<decimal>(30);
                //epw.DaysSinceSnow = csv.GetField<decimal>(31);

                this.DryBulbTemp = epwNoHeader.Select(o => Double.Parse(o.Split(',')[6])).ToArray(); // Dry Bulb Temperature
                this.DewPointTemp = epwNoHeader.Select(o => Double.Parse(o.Split(',')[7])).ToArray(); // Dew Point Temperature
                this.RelativeHumidity = epwNoHeader.Select(o => Double.Parse(o.Split(',')[8])).ToArray(); // Relative Humidity
                this.Pressure = epwNoHeader.Select(o => Double.Parse(o.Split(',')[9])).ToArray(); // Barometric Pressure
                this.WindSpeed = epwNoHeader.Select(o => Double.Parse(o.Split(',')[21])).ToArray(); // WindSpeed
                this.WindDirection = Array.ConvertAll<double, int>(epwNoHeader.Select(o => Double.Parse(o.Split(',')[20])).ToArray(), x => (int)x); // Wind Direction, needs to be int in epw format
                this.DirectNormalRadiation = epwNoHeader.Select(o => Double.Parse(o.Split(',')[14])).ToArray(); // Direct Normal Radiation
                this.DiffuseHorizontalRadiation = epwNoHeader.Select(o => Double.Parse(o.Split(',')[15])).ToArray(); // Diffuse Horizontal Illuminance

                this.HorRadiation = epwNoHeader.Select(o => Double.Parse(o.Split(',')[10])).ToArray(); ;
                this.NormalRadiation = epwNoHeader.Select(o => Double.Parse(o.Split(',')[11])).ToArray(); ;
                this.SkyRadiation = epwNoHeader.Select(o => Double.Parse(o.Split(',')[12])).ToArray(); ;
                this.GHorRadiation = epwNoHeader.Select(o => Double.Parse(o.Split(',')[13])).ToArray(); ;

                this.DirectNormalRadiation = epwNoHeader.Select(o => Double.Parse(o.Split(',')[14])).ToArray(); // Direct Normal Radiation
                this.DiffuseHorizontalRadiation = epwNoHeader.Select(o => Double.Parse(o.Split(',')[15])).ToArray(); // Diffuse Horizontal

                this.TotalSkyCover = epwNoHeader.Select(o => Double.Parse(o.Split(',')[22])).ToArray(); // TotalSkyCover
                this.OpaqSkyCover = epwNoHeader.Select(o => Double.Parse(o.Split(',')[23])).ToArray(); // OpaqSkyCover // used for Sky Temp

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