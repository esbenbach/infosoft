# To run this script without signing it first:
#  Set-ExecutionPolicy  Unrestricted
# ------------------------------------------------------------------------------

# Script prerequisites
# Powershell 2.0
# Requires a working oracle client
# Requires SQLPLUS is in the system path

# PSScriptRoot is available in Powershell 3.0 remove this line if only running on PS3 instanses
$PSScriptRoot = Split-Path $MyInvocation.MyCommand.Path -Parent

# Import CSV list of databases
$databaseList = Import-Csv $PSScriptRoot/databases.csv

$newline = [environment]::newline
foreach ($line in $databaseList)
{
	$username = $line.Username
	$password = $line.Password
	$server = $line.Tnsname
	
	# Build SQL header
	$header = @"
-- Configure HTML Output
SET MARKUP HTML ON HEAD " -
 -
  body {font:10pt Arial,Helvetica,sans-serif; color:black; background:White;} -
  p {   font:10pt Arial,Helvetica,sans-serif; color:black; background:White;} -
        table,tr,td {font:10pt Arial,Helvetica,sans-serif; color:Black; background:#f7f7e7; -
        padding:0px 0px 0px 0px; margin:0px 0px 0px 0px; white-space:nowrap;} -
  th {  font:bold 10pt Arial,Helvetica,sans-serif; color:#336699; background:#cccc99; -
        padding:0px 0px 0px 0px;} -
  h1 {  font:16pt Arial,Helvetica,Geneva,sans-serif; color:#336699; background-color:White; -
        border-bottom:1px solid #cccc99; margin-top:0pt; margin-bottom:0pt; padding:0px 0px 0px 0px;} -
  h2 {  font:bold 10pt Arial,Helvetica,Geneva,sans-serif; color:#336699; background-color:White; -
        margin-top:4pt; margin-bottom:0pt;} a {font:9pt Arial,Helvetica,sans-serif; color:#663300; -
        background:#ffffff; margin-top:0pt; margin-bottom:0pt; vertical-align:top;} -
 -
" -
BODY "" -
TABLE "border='1' align='center' summary='Script output'" -
PREFORMAT OFF ENTMAP ON 
SET termout off feedback off trimspool on trimout on tab off underline off

"@
	
	# Build SQL footer
	$footer = @"
/
spool off
quit;

"@
	New-Item "$PSScriptRoot/Output/$server" -ItemType Directory -ErrorAction SilentlyContinue | Out-Null #Discards output
	
	foreach($scriptFile in Get-ChildItem "$PSScriptRoot/scripts/*.sql")
	{
		$filename = $scriptFile.BaseName
		# Create new script file
		$createdScriptPath = "$PSScriptRoot/temp/$server.$filename.sql"
		
		$script = "spool `"$PSScriptRoot/Output/$server/$filename.result.html`" $newline$header$newline"
		
		foreach($scriptLine in (Get-Content $scriptFile))
		{
			$script += "$scriptLine $newline"
		}
		
		$script += "$footer"
		$script | Out-File $createdScriptPath -Encoding "ASCII"
		
		sqlplus -S -L $username/$password`@$server `@$createdScriptPath
	}
}