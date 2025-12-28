using System.IO;
using System.Net;
using Xunit;

namespace RhinoPlugin.Test.Xunit
{
    [Collection("Rhino Collection")]
    public class WeatherDownload

    {
        private const string WeatherDirectory = @"C:\Eddy3D\Common\Weather";

        public static string DownloadAhmedabadEPW()
        {
            var epwUrl = @"https://energyplus-weather.s3.amazonaws.com/asia_wmo_region_2/IND/IND_Ahmedabad.426470_IWEC/IND_Ahmedabad.426470_IWEC.epw";
            return DownloadWeatherFile("IND_Ahmedabad.426470_IWEC.epw", epwUrl);
        }

        public static string DownloadEPW()
        {
            var epwUrl = @"https://energyplus-weather.s3.amazonaws.com/north_and_central_america_wmo_region_4/USA/NY/USA_NY_New.York-J.F.Kennedy.Intl.AP.744860_TMY3/USA_NY_New.York-J.F.Kennedy.Intl.AP.744860_TMY3.epw";
            return DownloadWeatherFile("USA_NY_New.York-J.F.Kennedy.Intl.AP.744860_TMY3.epw", epwUrl);
        }

        private static string DownloadWeatherFile(string filename, string url)
        {
            Directory.CreateDirectory(WeatherDirectory);
            var filePath = Path.Combine(WeatherDirectory, filename);
            if (!File.Exists(filePath))
            {
                DownloadFile(url, filePath);
            }

            return filePath;
        }

        public static void DownloadFile(string url, string filePath)
        {
            using (var webClient = new WebClient())
            {
                webClient.DownloadFile(url, filePath);
            }
        }
    }
}
