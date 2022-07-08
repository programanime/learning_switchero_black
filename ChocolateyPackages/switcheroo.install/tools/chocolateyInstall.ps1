﻿$packageName = 'switcheroo.install'
$installerType = 'EXE'
$url = 'https://github.com/kvakulo/Switcheroo/releases/download/v0.9.2/switcheroo-setup.exe'
$silentArgs = '/VERYSILENT /SUPPRESSMSGBOXES /NORESTART'
$validExitCodes = @(0)

Install-ChocolateyPackage "$packageName" "$installerType" "$silentArgs" "$url" "$url64"  -validExitCodes $validExitCodes