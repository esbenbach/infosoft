#
# This script requires powershell to be run as Admin in order to work
# 
# Before running the script you should have a look at the SiteConfigurations.psd1 file for group sites and configurations as well as setting the default site configuration Web.config
# 
# Example Site structure that this script will create:
#
# c:\wwwroot\root
#			\Version    <-- The actual web application that should be created
#			\SITENAME1\
#					  \Web.config
#					  \bin    -> ..\Version\bin   - A symlink to the application binary folder 
#					  \config -> ..\configKey1    - A symlink to configKey1
#			\SITENAME2
#			\SITENAME3
#           \configurations 
#						   \configKey1 <-- Here you place configurations shared by multiple installations but still stuf that should not be global (a symlink is created inside the title directories)
#						   \configKey1
#			Web.config  <-- A global shared configuration file (the script won't create this for you, its just an indication of the desired design)
# The associated SiteConfigurations.psd1 for various configuration data that drives the script.
#
# After the script has completed you need to do the following.
# 1. Create a config.xml file in each of the "configKey" folders with the references to the web service that the retail web client should use.
# 2. Copy various logo.jpg to the root of the "SITENAME" folders
# 3. Copy the default version Web.config to the root folder and remove runtime bindings as well as any serviceModel references that you have overridden in the config.xml file
# 4. Test that everything works.
#
[CmdletBinding()]
Param(
    [Parameter(Mandatory=$true, HelpMessage="Define the root where the directory structure will be created")]
    [string]$wwwrootPath,

	[Parameter(Mandatory=$false, HelpMessage="The port number the main site should bind to")]
	[int]$portNumber = 80,

	[Parameter(Mandatory=$false, HelpMessage="The default name of the main/root IIS site, defaults to RetailWeb")]
	[string]$rootSiteName = "RetailWeb",

	[Parameter(Mandatory=$false, HelpMessage="Prefix to put before the title code when creating the applications inside the main site")]
	[string]$appPrefix = "",

	[Parameter(Mandatory=$false, HelpMessage="Path to a directory containing the Retail Web application which will be copied to the IIS directory structure, if omitted this must be done manually afterwards", ValueFromPipeline=$true)]
	[ValidateScript({Test-Path $_ -PathType 'Container'})] 
	[string]$applicationDir = "",

	[Parameter(Mandatory=$false, HelpMessage="If this is set to true, it will attempt to autodetect which directories from the application that should be created as virtual directories, requires that the applicationDir was set as well")]
	[bool]$autoDetectVirtualDirectory = $false
)

Function Parse-Psd1
{
    [CmdletBinding()]
    Param (
        [Parameter(Mandatory = $true)]
        [Microsoft.PowerShell.DesiredStateConfiguration.ArgumentToConfigurationDataTransformation()]
        [hashtable] $data
    )
    return $data
}

Function Find-VirtualDirectories
{
	[CmdletBinding()]
    Param (
		[bool]$autoDetect,
		[string]$path,
		[string[]]$configuredDirectories
	)
	
	if(!$autoDetect)
	{
		return $configuredDirectories;
	}

	# Do auto detection
	$childDirectories = Get-ChildItem $path -Directory | where {$_.Name -ne "bin" }
	return $childDirectories;
}

# Use this when developing to make sure we stop at all errors
#$ErrorActionPreference = "Stop"

$watch = [Diagnostics.StopWatch]::StartNew();

Import-Module WebAdministration


if ($PSVersionTable.PSVersion.Major -ge 5)
{
    $configuration = Parse-Psd1 "$PSScriptRoot\SiteConfigurations.psd1"
}
else
{
    Import-LocalizedData -BaseDirectory $PSScriptRoot -FileName "SiteConfigurations.psd1" -BindingVariable configuration
}

$applicationFilePath = "$wwwrootPath\Version";

# Copy application if path is given, otherwise just create the app filepath
if($applicationDir -ne "")
{
	Copy-Item $applicationDir –destination $applicationFilePath -recurse -Force
}
else 
{
	New-Item $applicationFilePath -ItemType Directory;
}

# Create root app pool, and web site
New-WebAppPool $rootSiteName;
New-Website -Name $rootSiteName -HostHeader "" -Port $portNumber -PhysicalPath $wwwrootPath -ApplicationPool $rootSiteName

# Create all applications, directories and links
foreach($item in $configuration.SiteConfigurationMapping.GetEnumerator())
{
	$appName = $item.Key;
	$configName = $item.Value;
	$siteName = "$rootSiteName$appName";
	$siteFilePath = "$wwwrootPath\$appName";
	$applicationName = "$appPrefix$appName";
	
	if (!(Test-Path $siteFilePath))
	{
		New-Item "$siteFilePath" -ItemType Directory;
	}

	if(!(Test-Path $wwwrootPath\configurations\$configName))
	{
		New-Item "$wwwrootPath\configurations\$configName" -ItemType Directory;
	}
	
	if ((Get-WebApplication -Name $siteName) -eq $null)
	{
		New-WebAppPool $siteName;
		Set-ItemProperty "IIS:\apppools\$siteName" -Name $enable32Bit -Value "True"
		New-WebApplication -Name $applicationName -Site $rootSiteName -ApplicationPool $siteName -PhysicalPath $siteFilePath
	}

	cmd /c mklink /d "$siteFilePath\bin" "$applicationFilePath\bin";
	cmd /c mklink "$siteFilePath\Global.asax" "$applicationFilePath\Global.asax";
	cmd /c mklink /d "$siteFilePath\config" "$wwwrootPath\configurations\$configName";
	
	$directories = Find-VirtualDirectories $autoDetectVirtualDirectory $applicationFilePath $configuration.VirtualDirectories

	foreach($vdir in $directories)
	{
		New-WebVirtualDirectory -Site $rootSiteName -Application $applicationName -Name $vdir -PhysicalPath "$applicationFilePath\$vdir" -Force
	}

	# Create default web.config using provided example config, this is not very flexible and should probably be adopted if used for something other than the case it was developed for.
	$webconfigstring = $configuration.DefaultSiteWebConfig.Replace("{TITLECODEPLACEHOLDER}","$appName");
	$webconfigstring | Out-File -Encoding utf8 -FilePath "$siteFilePath\Web.config";
}

$watch.Stop();
Write-Host $watch.Elapsed;