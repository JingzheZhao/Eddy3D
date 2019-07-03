//namespace CallOF
//{
//    internal class CallDocker
//    {
//        public string app;
//        public string volumeDocker;
//        public string entryPoint;
//        public string container;
//        public string sourceEnvironment;
//        public string logging;
//        public string app_argument;

//        public void SetVars(Options options)
//        {
//            this.app = "docker";
//            //string filepath = "/c/OF/";
//            this.volumeDocker = "/home/openfoam/";
//            this.entryPoint = @"-i --entrypoint=""""";  //-it didnt work --> the input device is not a TTY.  If you are using mintty, try prefixing the command with 'winpty'
//            this.container = "hfdresearch/swak4foamandpyfoam:latest-v4.1 ";
//            this.sourceEnvironment = @"source /opt/openfoam4/etc/bashrc; cd /home/openfoam; ";
//            this.logging = @"";
//            //string command = "blockMesh";
//            this.app_argument = string.Format("run -v \"{0}:{1}\" {2} {3} bash -c \"{4}{5}{6}\"", options.FilePath.Trim(), volumeDocker, entryPoint, container, sourceEnvironment, options.Command, logging);
//            //Environment.SetEnvironmentVariable("PATH", @"C:\Program Files\Docker\Docker\Resources\bin");

//        }

//    }
//}