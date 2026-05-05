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

            string climateClass = "";
            double delta = 0.3;

            // Bolt: Replaced O(N) memory allocation (File.ReadAllLines + jagged array)
            // and slow Regex.Split with O(1) lazy iteration (File.ReadLines.Skip(1))
            // and fast String.Split with early return to avoid evaluating the rest of the file once found.
            foreach (var line in File.ReadLines(filePathKoeppen).Skip(1))
            {
                var parts = line.Split(new[] { ' ', '\t' }, StringSplitOptions.RemoveEmptyEntries);
                if (parts.Length < 3) continue;

                if (Math.Abs(longitude - double.Parse(parts[0], CultureInfo.InvariantCulture)) < delta &&
                    Math.Abs(latitude - double.Parse(parts[1], CultureInfo.InvariantCulture)) < delta)
                {
                    climateClass = parts[2];
                    break;
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
