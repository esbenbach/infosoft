# Coverity Defect Updater
A simple executable that goes through unassigned defects from a Coverity Connect server and assigns ownership of the defects based on owner of the last "non-merge" changeset from TFS.
It requires access to both TFS and Coverity Connect. For TFS it works by targeting a specific branch (since branch path is not part of the defect), and works its way across the merge
history until it finds a changeset that is not a merge changeset.

It was created to make coverity TFS integration work "properly" since the built-in integration does not consider merges, and in addition works on a line by line annotation
so it takes a lot of extra time, in additions it could actually be considered wrong (but so could last person to change the file).

# Requirements
Access to TFS and a Coverity Connect server running API version 9.

# Usage
To use it, compile it and change the coverity connect hostname in the app.config file
Run the executable with the required command line parameters such as:

  CoverityDefectUpdater --user <CoverityConnectUser> --password <CoverityConnectPassword> -s <CoverityStreamName> -c <TFSProjectCollectionUrl> -p <TFSProjectName> -b <TFSBranchRoot>

And an example:

	CoverityDefectUpdater --user esben --password secret! -s MainStream -c http://tfs:8080/tfs/Infosoft -p AwesomeApp -b /Development/Main


There are two additional command line arguments:

	-l "Legacy", will assign defects marked with the Legacy attribute as well. Ordinarily these are excluded from the defect update.

	-r "simulate", will do a simulation run and get defects from coverity and owner from TFS but will not actually do anything (just write to log what it would have done).
