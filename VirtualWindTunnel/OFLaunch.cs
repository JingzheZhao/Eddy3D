using SlavaGu.ConsoleAppLauncher;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WindTunnel
{
    class OFLaunch
    {

        public static void Run(string command, string filepath)
        {
            string app = "docker";
            //string filepath = "/c/OF/";
            string volume_docker = "/home/openfoam/";
            string entrypoint = @"--entrypoint=""""";
            string container = "hfdresearch/swak4foamandpyfoam:latest-v4.1 ";
            string prep = @"source /opt/openfoam4/etc/bashrc; cd /home/openfoam; ";
            //string command = "blockMesh";
            var app_argument = string.Format("run -v {0}:{1} {2} {3} bash -c \"{4}{5}\"", filepath, volume_docker, entrypoint, container, prep, command);
            //Environment.SetEnvironmentVariable("PATH", @"C:\Program Files\Docker\Docker\Resources\bin");

            Console.WriteLine(ConsoleApp.Run(app, app_argument).Output.Trim());
            //Console.ReadKey();

        }

    }
}
