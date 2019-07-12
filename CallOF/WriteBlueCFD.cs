//namespace CallOF
//{
//    internal class WriteBlueCFD
//    {
//        public string output;

//        public void SetVars(string command, string workingDir, string installationPath = @"C:\OpenFOAM\")
//        {
//            this.output = string.Format(@"cd {0}
//call setvars.bat
//set PATH =% HOME %\msys64\usr\bin;% PATH %
//cd ""{1}""
//{2}", installationPath, workingDir, command);

// }

//    }
//}