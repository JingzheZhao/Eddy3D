
REM SET ilrepack=$(SolutionDir)packages\ILMerge.3.0.40\tools\net452\ILMerge.exe
SET ilrepack=$(SolutionDir)packages\ILRepack.2.0.18\tools\ILRepack.exe

SET build_eddylib=$(SolutionDir)EddyLib\bin
SET build_eddy=$(SolutionDir)Eddy\bin
SET build_batchrunner=$(SolutionDir)CallBatchRunner\bin

SET post_build_ilmerge=$(SolutionDir)Build\Ilmerge
SET post_build_dotfuscated=$(SolutionDir)Build\Dotfuscated
SET post_build=$(SolutionDir)Build
SET installation_folder=C:\Eddy3D

SET cappl=$(SolutionDir)packages\ConsoleAppLauncher.1.0.6354.408\lib\net40\ConsoleAppLauncher.dll
SET cliapp=$(SolutionDir)packages\CommandLineParser.1.9.71\lib\net45\CommandLine.dll

SET rhc=$(SolutionDir)packages\RhinoCommon.6.26.20147.6511\lib\net45\RhinoCommon.dll
SET gh=$(SolutionDir)packages\Grasshopper.6.26.20147.6511\lib\net45\Grasshopper.dll

SET medallionShell=$(SolutionDir)packages\MedallionShell.1.6.2\lib\net45\MedallionShell.dll
SET bson=$(SolutionDir)packages\Newtonsoft.Json.Bson.1.0.2\lib\net45\Newtonsoft.Json.Bson.dll
SET json=$(SolutionDir)packages\Newtonsoft.Json.12.0.3\lib\net45\Newtonsoft.Json.dll
SET numerics=$(SolutionDir)packages\MathNet.Numerics.4.11.0\lib\net461\MathNet.Numerics.dll
SET protobuf=$(SolutionDir)packages\protobuf-net.3.0.73\lib\net461\protobuf-net.dll





if $(ConfigurationName)==Debug (
echo robocopy "%build_eddy%" "%installation_folder%" /E
robocopy "%build_eddy%" "%installation_folder%" /E
)
if $(ConfigurationName)==Release (


echo ""
echo "***"
echo "COPY TO DOTFUSCATOR"
echo "***"
echo ""

REM xcopy "$(ProjectDir)ApplicationFiles" "$(TargetDir)ApplicationFiles" 



robocopy "%build_eddy%" "%post_build_dotfuscated%"  /E
robocopy "%build_eddylib%"  "%post_build_dotfuscated%"  /E


echo ""
echo "***"
echo "DOTFUSCATE"
echo "***"
echo ""


echo "C:\Program Files (x86)\Microsoft Visual Studio\2019\Community\Common7\IDE\Extensions\PreEmptiveSolutions\DotfuscatorCE\dotfuscatorCLI.exe" "%post_build%\Dotfuscator.xml"
"C:\Program Files (x86)\Microsoft Visual Studio\2019\Community\Common7\IDE\Extensions\PreEmptiveSolutions\DotfuscatorCE\dotfuscatorCLI.exe" "%post_build%\Dotfuscator.xml"

echo ""
echo "***"
echo "COPY TO ILMERGE"
echo "***"
echo ""

copy "%protobuf%" "%post_build_ilmerge%" /Y
copy "%post_build_dotfuscated%\Eddy.gha" "%post_build_ilmerge%" /Y
copy "%post_build_dotfuscated%\EddyLib.dll" "%post_build_ilmerge%" /Y


REM copy "%build_eddy%\Eddy.gha" "%post_build_ilmerge%"
REM copy "%build_calloc%\CallOC.exe" "%post_build_ilmerge%"
REM copy "%build_callprobes%\CallProbes.exe" "%post_build_ilmerge%"
REM copy "%build_callray%\CallRay.exe" "%post_build_ilmerge%"
REM copy "%build_callof%\CallOF.exe" "%post_build_ilmerge%"
REM copy "%build_calloc%\CallOC.exe" "%post_build_dotfuscated%"
REM copy "%build_batchrunner%\CallBatchRunner.exe" "%post_build_dotfuscated%"
REM copy "%build_callprobes%\CallProbes.exe" "%post_build_dotfuscated%"
REM copy "%build_callray%\CallRay.exe" "%post_build_dotfuscated%"
REM copy "%build_callof%\CallOF.exe" "%post_build_dotfuscated%"

echo ""
echo "***"
echo "ILMERGE"
echo "***"
echo ""

REM "%gh%" "%rhc%"

"%ilrepack%" /out:"%post_build_ilmerge%\EddyLib.dll" "%cliapp%" "%cappl%" "%post_build_ilmerge%\EddyLib.dll" "%medallionShell%" "%json%" "%bson%"  "%numerics%" "%protobuf%"

echo ""
echo "***"
echo "COPY TO %installation_folder%"
echo "***"
echo ""



copy "%post_build_dotfuscated%\Eddy.gha" "%installation_folder%" /Y
copy "%post_build_ilmerge%\Eddy.gha" "%installation_folder%" /Y
copy "%post_build_ilmerge%\EddyLib.dll" "%installation_folder%" /Y
copy "%build_batchrunner%\CallBatchRunner.exe" "%installation_folder%" /Y
copy "%protobuf%" "%installation_folder%" /Y
)
