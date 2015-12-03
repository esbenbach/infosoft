# Web Site Creater
A simple powershell utility to help install web applications into IIS. 
Requires the WebAdministration powershell module to work.
It basically creates N applications in a single (specified) Web Site all pointing to the same copy of the actual application code.

This is nice when you need multiple installations of the same app where the only need is to differentiate in configuration. 
See the script file for details.

It was originally created for a very specific application scenario, but that scenario turned out to be not that specific after all.