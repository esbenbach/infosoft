


[CmdletBinding()]
Param(
    [Parameter(Mandatory=$true, HelpMessage = "The Template Version folder that contains various default files such as the DevExpress translations")]
    [string]$templateFolder,

    [Parameter(Mandatory=$true, HelpMessage = "The TFS Drop Folder, typically the nightly build root")]
    [string]$dropFolder,

    [Parameter(Mandatory=$true, HelpMessage= "The location we the release should be created/copied to")]
    [string]$targetFolder,

    [Parameter(Mandatory=$false, HelpMessage = "An optional version number/name/tag for the created zip package")]
    [string]$versionTag
)

$clientFolderName = "Infosoft Client";
$schedulerFolderName = "Infosoft Job Scheduler";
$userWsFolderName = "Infosoft User Webservice";
$wsFolderName = "Infosoft Webservice";
$retailWebClientFolderName = "Infosoft Retail Web Client";

# Currently the IceLib template folder name includes the version number, we should possibly get that out of the way at some point
$iceLibTemplateFolderName = "IceLib.CIC.Files.4.6.17";

$pathseparator = [System.IO.Path]::DirectorySeparatorChar;

$supportedLanguages = @("da", "no", "sv", "en");

Function Create-ReleaseDirectories
{
    
    Write-Host ("Creating directory structure in $targetFolder");
    
    $directories = @($clientFolderName, $schedulerFolderName, $userWsFolderName, $wsFolderName, $retailWebClientFolderName, "Setup", "Webservice Log", "IceLib Files");

    $directories | ForEach-Object { New-Item -Type Directory -Path (Join-Path -Path $targetFolder -ChildPath $_) -Force };
}


Function Copy-TemplateToRelease
{
    Write-Host ("Copying template data into $targetFolder");

    $applicationTargets = @($clientFolderName, $schedulerFolderName, $userWsFolderName, $wsFolderName, $retailWebClientFolderName);
    
    Write-Host ("Copying core/base DLL files");

    foreach ($file in Get-Item "$templateFolder/*.dll")
    {
        Copy-Item $file -Destination (Join-Path -Path $targetFolder -ChildPath "$target\$clientFolderName\");
        Copy-Item $file -Destination (Join-Path -Path $targetFolder -ChildPath "$target\$schedulerFolderName\");
        Copy-Item $file -Destination (Join-Path -Path $targetFolder -ChildPath "$target\$userWsFolderName\bin\");
        Copy-Item $file -Destination (Join-Path -Path $targetFolder -ChildPath "$target\$wsFolderName\bin\");
    }

    Write-Host ("Copying translation files");

    foreach ($directory in Get-ChildItem $templateFolder -Directory)
    {
        if ($directory.Name -ne "Tilleggskomponenter")
        {
            foreach ($target in $applicationTargets)
            {
                Copy-Item "$templateFolder\$directory" -Destination (Join-Path -Path $targetFolder -ChildPath "$target\$directory") -Recurse
            }
        }
    }

    Write-Host ("Copying Icelib component files");
    Copy-Item "$templateFolder/Tilleggskomponenter/$iceLibTemplateFolderName/*" -Destination (Join-Path -Path $targetFolder -ChildPath "IceLib Files/");
}

Function Clean-Directory
{
    Param(
        [parameter(Mandatory=$true)]
        [string]
        $directory
    )
    
    $foldersToRemove = @("de", "es", "ja", "ru", "Log");
    $filesToRemove = @("*.xml", "*.XML", "*.config", "*.pdb", "*.pssym", "*.psproj");
    $filesToNeverRemove = @("logging.config", "web.config", "Infosystems.exe.config", "Infosoft.Scheduler.Service.exe.config");
    
    foreach($dir in Get-ChildItem $directory -Directory)
    {
        if ($foldersToRemove -contains $dir.Name)
        {
            Remove-Item $dir.FullName -Recurse -Force
        }
    }

    foreach ($filter in $filesToRemove)
    {
        foreach ($matchedFile in Get-ChildItem $directory/$filter)
        {
            if ($filesToNeverRemove -notcontains $matchedFile.Name)
            {
                Remove-Item $matchedFile -Force
            }
        }
    }
}

function Zip-Directory {
    Param(
      [Parameter(Mandatory=$True)][string]$DestinationFileName,
      [Parameter(Mandatory=$True)][string]$SourceDirectory,
      [Parameter(Mandatory=$False)][string]$CompressionLevel = "Optimal",
      [Parameter(Mandatory=$False)][switch]$IncludeParentDir
    )
    Add-Type -AssemblyName System.IO.Compression.FileSystem
    $CompressionLevel    = [System.IO.Compression.CompressionLevel]::$CompressionLevel  
    [System.IO.Compression.ZipFile]::CreateFromDirectory($SourceDirectory, $DestinationFileName, $CompressionLevel, $IncludeParentDir)
}

function CreateZip-Release
{
    Write-Host "Creating Zip Package"

    if (!$versionTag)
    {
        $releaseName = "Version-$versionTag-" + $(Get-Date -Format ddMMyyyy) + ".zip";
    }
    else
    {
        $releaseName = "Version-" + $(Get-Date -Format ddMMyyyy) + ".zip";
    }

    Zip-Directory -DestinationFileName "$targetFolder/../$releaseName" -SourceDirectory $targetFolder -CompressionLevel "Optimal"
}

Create-ReleaseDirectories;

Write-Host ("Copying Drop Output to $targetFolder");

$sourceMapping = @{
    $clientFolderName = (Join-Path -Path $dropFolder -ChildPath "x86/Release/InfoSystems");
    $schedulerFolderName = (Join-Path -Path $dropFolder -ChildPath "x86/Release/JobScheduler");
    $userWsFolderName = (Join-Path -Path $dropFolder -ChildPath "_PublishedWebSites/IS.UserRegister.WebHost");
    $wsFolderName = (Join-Path -Path $dropFolder -ChildPath "_PublishedWebSites/WebServices");
	$retailWebClientFolderName = (Join-Path -Path $dropFolder -ChildPath "_PublishedWebSites/IS.Retail.Client.Web");
    "Webservice Log" = (Join-Path -Path $dropFolder -ChildPath "WSDL");
    "Setup" = (Join-Path -Path $dropFolder -ChildPath "x86/Release/Deployment");
};

foreach($map in $sourceMapping.GetEnumerator())
{
    Get-Childitem $map.Value | ForEach-Object { Copy-Item $_.FullName -Destination "$targetFolder/$($map.Name)" -Recurse -Force; }
    Clean-Directory "$targetFolder/$($map.Name)";
}

Copy-TemplateToRelease;

CreateZip-Release;