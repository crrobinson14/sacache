DEL /s /q .\TestResults

DEL /s /q .\GeneratedReports
MKDIR ".\GeneratedReports"

IF EXIST ".\SACache.trx" DEL ".\SACache.trx%"

.\packages\OpenCover.4.6.519\tools\OpenCover.Console.exe -register:user -target:"C:\Program Files\Microsoft Visual Studio 14.0\Common7\IDE\MSTest.exe" -targetargs:"/testcontainer:\".\SACacheTest\bin\Debug\SACacheTest.dll\" /resultsFile:\"SACache.trx\"" -filter:"+[SACache*]* -[SACacheTest]* -mergebyhash -skipautoprops -output:".\GeneratedReports\SACacheReport.xml"

.\packages\ReportGenerator.2.4.5.0\tools\ReportGenerator.exe -reports:"results.xml" -targetdir:"GeneratedReports"

start GeneratedReports\index.htm
