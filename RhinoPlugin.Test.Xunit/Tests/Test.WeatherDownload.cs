using System.IO;
using System.Net;
using Xunit;

namespace RhinoPlugin.Test.Xunit
{
    [Collection("Rhino Collection")]
    public class WeatherDownload

    {
        public static string DownloadAhmedabadEPW()
        {
            string epw = @"C:\Eddy3D\Common\Weather\IND_Ahmedabad.426470_IWEC.epw";

            var epwURL = @"  https://energyplus-weather.s3.amazonaws.com/asia_wmo_region_2/IND/IND_Ahmedabad.426470_IWEC/IND_Ahmedabad.426470_IWEC.epw";

            var folder = @"C:\Eddy3D\Common\Weather\";
            if (!Directory.Exists(folder)) { Directory.CreateDirectory(folder); }
            if (!File.Exists(epw)) { DownLoadFile(epwURL, epw); }

            return epw;
        }

        public static string DownloadEPW()
        {
            //string epw = @"C:\Eddy3D\Common\Weather\USA_NY_New.York-LaGuardia.AP.725030_TMY3.epw";
            string epw = @"C:\Eddy3D\Common\Weather\USA_NY_New.York-J.F.Kennedy.Intl.AP.744860_TMY3.epw";

            // var epwURL = @"https://energyplus-weather.s3.amazonaws.com/north_and_central_america_wmo_region_4/USA/NY/USA_NY_New.York-LaGuardia.AP.725030_TMY3/USA_NY_New.York-LaGuardia.AP.725030_TMY3.epw";
            var epwURL = @"https://energyplus-weather.s3.amazonaws.com/north_and_central_america_wmo_region_4/USA/NY/USA_NY_New.York-J.F.Kennedy.Intl.AP.744860_TMY3/USA_NY_New.York-J.F.Kennedy.Intl.AP.744860_TMY3.epw";

            var folder = @"C:\Eddy3D\Common\Weather\";
            if (!Directory.Exists(folder)) { Directory.CreateDirectory(folder); }
            if (!File.Exists(epw)) { DownLoadFile(epwURL, epw); }

            return epw;
        }

        public static void DownLoadFile(string URL, string FilePath)
        {
            WebClient webClient = new WebClient();
            webClient.DownloadFile(URL, FilePath);
        }
    }
}