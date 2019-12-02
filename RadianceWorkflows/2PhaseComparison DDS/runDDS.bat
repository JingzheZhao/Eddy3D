cd "F:\GoogleDrive\LAB\Patrick\2PhaseComparison DDS"


REM ###################################
REM Pre
REM ###################################


REM Convert epw to wea tape
REM -----------------------------------
epw2wea "C:\DIVA\WeatherData\USA_NY_New.York-LaGuardia.AP.725030_TMY3.epw" output/NYC.wea

REM Make the OCTREE
REM -----------------------------------
REM takes an array of rad files and combines them into one octree
oconv scene.rad > output/scene.oct
oconv sceneBlack.rad > output/sceneBlack.oct


REM ###################################
REM 1 Perform an annual daylight coefficient simulation.
REM ###################################


REM Create daylight coefficient matrix for R1 sky (145patches)
REM -----------------------------------
REM -I+ denotes that the simulation is being performed for calculating irradiance instead of radiance
REM The 48 in -y 48 is equal to the number of lines in the file sensors.pts
REM The number of processors assigned for the simulation can be set with -n 4
rfluxmtx -I+ -y 720 -lw 0.0001 -ab 5 -ad 10000 -n 16 - skyDomes\skyglowR1.rad -i output/scene.oct < sensors.pts > output/dc_r1.mtx

REM Generates SkyVector for whole year.
REM -----------------------------------
REM -m controls the sky subdivision
REM Use -O1 to switch to solar rad
REM The −d option may be used to produce a sun-only matrix, with no sky contributions. Alternatively, the −s option may be used to exclude any direct solar component from the output. 
gendaymtx -m 1 -O1 output/NYC.wea > output/NYC.smx


REM Create Illum
REM Illuminace Weights == 47.4 119.9 11.6  // For Radiation 0.265 0.670 0.065 ???
REM -----------------------------------
dctimestep output/dc_r1.mtx output/NYC.smx | rmtxop -fa -t -c 0.265 0.670 0.065 - > output/annualR_dc.ill


REM ###################################
REM 2 Perform an annual direct-only daylight coefficients simulation.
REM ###################################
 
rfluxmtx -I+ -y 720 -lw 0.0001 -ab 1 -ad 10000 -n 16 - skyDomes\skyglowR1.rad -i output/sceneBlack.oct < sensors.pts > output/dcd_r1.mtx

gendaymtx -m 1 -O1 -d output/NYC.wea > output/NYCd.smx

dctimestep output/dcd_r1.mtx output/NYCd.smx | rmtxop -fa -t -c 0.265 0.670 0.065 - > output/annualR_dcd.ill


REM ###################################
REM 3 Perform an annual sun-coefficients simulation.
REM ###################################
echo void light solar 0 0 3 1e6 1e6 1e6 > output/suns.rad
REM Create solar discs and corresponding modifiers for 2305 suns corresponding to a Reinhart MF:4 subdivision.
REM 0.533 solar disc size as angle
cnt 2305 | rcalc -e MF:4 -f C:\DIVA\Radiance\lib\reinsrc.cal -e Rbin=recno -o "solar source sun 0 0 4 ${Dx} ${Dy} ${Dz} 0.533" >> output/suns.rad

REM Put suns in scene...
oconv sceneBlack.rad output/suns.rad > output/sceneBlackSuns.oct

REM Calculate illuminance sun coefficients for illuminance calculations.
rcontrib -I+ -ab 1 -y 720 -n 16 -ad 256 -lw 1.0e-3 -dc 1 -dt 0 -dj 0 -faf -e MF:4 -f C:\DIVA\Radiance\lib\reinhart.cal -b rbin -bn Nrbins -m solar output/sceneBlackSuns.oct < sensors.pts > output/cdsDDS.mtx

REM -5 option indicates 5phase method mode - solar disc angele must follow that input
REM The -d option in the SMX messes it all up --  you can't include -d and -5 together.
gendaymtx -5 0.533 -m 4 -O1 output/NYC.wea >  output/sunM6.smx

dctimestep output/cdsDDS.mtx output/sunM6.smx | rmtxop -fa -t -c 0.265 0.670 0.065 - > output/annualR_dcdds.ill

REM ###################################
REM 4 Combine Results
REM ###################################
rmtxop output/annualR_dc.ill + -s -1 output/annualR_dcd.ill + output/annualR_dcdds.ill > output/annualR.ill


pause
 



