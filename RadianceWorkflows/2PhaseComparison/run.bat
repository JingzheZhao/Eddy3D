cd "C:\Users\Timur\Desktop\2PhaseComparison"

REM Convert epw to wea tape
REM -----------------------------------
epw2wea "C:\DIVA\WeatherData\USA_NY_New.York-LaGuardia.AP.725030_TMY3.epw" output/NYC.wea

REM Generates SkyVector for whole year.
REM -----------------------------------
REM -m controls the sky subdivision
REM Use -O1 to switch to solar rad
REM The −d option may be used to produce a sun-only matrix, with no sky contributions. Alternatively, the −s option may be used to exclude any direct solar component from the output. 
gendaymtx -m 4 -O1 output/NYC.wea > output/NYC.smx


REM Make the OCTREE
REM -----------------------------------
REM takes an array of rad files and combines them into one octree
oconv scene.rad > output/scene.oct


REM Create Matrix
REM -----------------------------------
REM -I+ denotes that the simulation is being performed for calculating irradiance instead of radiance
REM The 48 in -y 48 is equal to the number of lines in the file sensors.pts
REM The number of processors assigned for the simulation can be set with -n 4
rfluxmtx -I+ -y 720 -lw 0.0001 -ab 5 -ad 10000 -n 12 - sky.rad -i output/scene.oct < sensors.pts > output/dc.mtx

REM Create Illum
REM -----------------------------------
REM ?????? 0.265 0.670 0.065 ????? geraten
dctimestep output/dc.mtx output/NYC.smx | rmtxop -fa -t -c 0.265 0.670 0.065 - > output/annualR.ill








