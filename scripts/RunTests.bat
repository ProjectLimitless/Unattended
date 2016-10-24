@echo on
echo Creating output directory
mkdir ..\TestResults
echo Running NUnit tests
nunit3-console.exe ..\Unattended.Test\bin\Debug\Unattended.Test.dll --result=..\TestResults\Unit.xml