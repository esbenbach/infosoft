This is a simple powershell script created as a quick "fix" to solve a particular need to do some custom follow-up work on our customers Oracle databases to detect known defects that would cause data issues.

Read this first before using the script runner.

This powershell script requires:
1. Powershell 2.0
2. SQLPlus to be in the path of the one running it
3. A working oracle client installation

To use the script, fill in databases in the databases.csv file and create scripts in the scripts folder!.

The script goes through all the databases in the "databases.csv" file and for each database
it executes all the .sql scripts in the "Scripts" directory using sql plus (so the .sql files should be sqlplus compatible).

The output (if any) of the scripts is placed in a seperate output directory.
Each server is granted its own directory, and each script file has its own output log.
The output log is an html formatted file.