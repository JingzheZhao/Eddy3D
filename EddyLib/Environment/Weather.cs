using ProtoBuf;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;

namespace EddyLib
{
    [ProtoContract]
    public class Weather
    {
        private const int EpwHeaderLines = 8;
        private const int EpwHourlyRecords = 8760;

        public Weather()
        {
        }

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
        public double Latitude;

        [ProtoMember(18)]
        public double Longitude;

        [ProtoMember(19)]
        public double TimeZone;

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
            if (string.IsNullOrWhiteSpace(filePath))
            {
                throw new ArgumentException("EPW file path is required.", nameof(filePath));
            }

            epwFilePath = filePath;

            // load weather data -----------------
            string[] epwData = File.ReadAllLines(filePath);
            if (epwData.Length <= EpwHeaderLines)
            {
                throw new InvalidDataException("EPW file does not contain header and hourly data.");
            }

            // get header data
            string[] headerFields = epwData[0].Split(',');

            Location = System.Text.RegularExpressions.Regex.Replace((headerFields[1]), @"\s+", "");
            Latitude = ParseDoubleField(headerFields, 6);
            Longitude = ParseDoubleField(headerFields, 7);
            TimeZone = ParseDoubleField(headerFields, 8);

            // get hourly data
            string[] epwNoHeader = epwData.Skip(EpwHeaderLines).Take(EpwHourlyRecords).ToArray();

            DryBulbTemp = new double[epwNoHeader.Length];
            DewPointTemp = new double[epwNoHeader.Length];
            RelativeHumidity = new double[epwNoHeader.Length];
            Pressure = new double[epwNoHeader.Length];
            WindSpeed = new double[epwNoHeader.Length];
            WindDirection = new int[epwNoHeader.Length];
            DirectNormalRadiation = new double[epwNoHeader.Length];
            DiffuseHorizontalRadiation = new double[epwNoHeader.Length];
            HorRadiation = new double[epwNoHeader.Length];
            NormalRadiation = new double[epwNoHeader.Length];
            SkyRadiation = new double[epwNoHeader.Length];
            GHorRadiation = new double[epwNoHeader.Length];
            TotalSkyCover = new double[epwNoHeader.Length];
            OpaqSkyCover = new double[epwNoHeader.Length];

            var yr = new double[epwNoHeader.Length];
            var mo = new double[epwNoHeader.Length];
            var dy = new double[epwNoHeader.Length];
            var hr = new double[epwNoHeader.Length];

            for (int i = 0; i < epwNoHeader.Length; i++)
            {
                var fields = epwNoHeader[i].Split(',');

                DryBulbTemp[i] = ParseDoubleField(fields, 6); // Dry Bulb Temperature
                DewPointTemp[i] = ParseDoubleField(fields, 7); // Dew Point Temperature
                RelativeHumidity[i] = ParseDoubleField(fields, 8); // Relative Humidity
                Pressure[i] = ParseDoubleField(fields, 9); // Barometric Pressure
                HorRadiation[i] = ParseDoubleField(fields, 10);
                NormalRadiation[i] = ParseDoubleField(fields, 11);
                SkyRadiation[i] = ParseDoubleField(fields, 12);
                GHorRadiation[i] = ParseDoubleField(fields, 13);
                DirectNormalRadiation[i] = ParseDoubleField(fields, 14); // Direct Normal Radiation
                DiffuseHorizontalRadiation[i] = ParseDoubleField(fields, 15); // Diffuse Horizontal
                WindDirection[i] = (int)ParseDoubleField(fields, 20); // Wind Direction
                WindSpeed[i] = ParseDoubleField(fields, 21); // WindSpeed
                TotalSkyCover[i] = ParseDoubleField(fields, 22); // TotalSkyCover
                OpaqSkyCover[i] = ParseDoubleField(fields, 23); // OpaqSkyCover

                yr[i] = ParseDoubleField(fields, 0);
                mo[i] = ParseDoubleField(fields, 1);
                dy[i] = ParseDoubleField(fields, 2);
                hr[i] = ParseDoubleField(fields, 3);
            }

            Console.WriteLine("Calculating: Solar Geometry");
            SolarElevation = new List<double>(epwNoHeader.Length);
            SolarAzi = new List<double>(epwNoHeader.Length);

            var sg = new SolarGeometry();

            for (int i = 0; i < yr.Length; i++)
            {
                double _el = sg.solarelevation(Latitude, Longitude, yr[i], mo[i], dy[i], hr[i], 0, 0, TimeZone, 0);
                double _az = sg.solarazimuth(Latitude, Longitude, yr[i], mo[i], dy[i], hr[i], 0, 0, TimeZone, 0);

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

        public string ClassifyClimateZone(string epwFilePath, string workingDirToSaveCSV)
        {
            string line1Weather = File.ReadLines(epwFilePath).First(); // gets the first line from file.
            double longitude = double.Parse(line1Weather.Split(',')[6], CultureInfo.InvariantCulture);
            double latitude = double.Parse(line1Weather.Split(',')[7], CultureInfo.InvariantCulture);

            string filePathKoeppen = Path.Combine(workingDirToSaveCSV ?? string.Empty, "Koeppen.csv");
            if (!string.IsNullOrWhiteSpace(workingDirToSaveCSV))
            {
                Directory.CreateDirectory(workingDirToSaveCSV);
            }

            if (!File.Exists(filePathKoeppen))
            {
                Utilities.DownLoadFile("http://www.rforscience.com/wpmain/wp-content/uploads/2014/06/Koeppen-Geiger-ASCII.txt", filePathKoeppen);
            }

            string[] txt = File.ReadAllLines(filePathKoeppen);

            // Stupid formatting of this file creates 4 columns
            int columnsCnt = 4;
            var matrix = ArrayHelper.CreateJaggedMatrix(txt.Length, columnsCnt);

            for (int i = 1; i < txt.Length; i++)
            {
                var line = System.Text.RegularExpressions.Regex.Split(txt[i], @"\s{1,}");
                for (int c = 0; c < columnsCnt; c++)
                {
                    // Data is stored in column 1-3, column 0 is empty
                    matrix[i][c] = line[c];
                }
            }

            string climateClass = "";
            double delta = 0.3;

            // - 1 because of header line; -2 ??

            for (int i = 1; i < txt.Length; i++)
            {
                // Data is stored in column 1-3, column 0 is empty
                if (Math.Abs(longitude - Convert.ToDouble(matrix[i][1], CultureInfo.InvariantCulture)) < delta
                    && Math.Abs(latitude - Convert.ToDouble(matrix[i][2], CultureInfo.InvariantCulture)) < delta)
                {
                    climateClass = Convert.ToString(matrix[i][3], CultureInfo.InvariantCulture);
                }
            }

            return climateClass;
        }

        private static double ParseDoubleField(string[] fields, int index)
        {
            return double.Parse(fields[index], CultureInfo.InvariantCulture);
        }
    }
}
