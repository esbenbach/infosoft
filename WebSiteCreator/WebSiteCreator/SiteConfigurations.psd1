# The SiteConfigurationMapping should be of the form "SITENAME" = "CONFIGURATIONNAME" 
# The sitename is typically the title code, and the configuration name typically denotes 
# which webservice the application should target, but in theory this could be anything.

@{
	SiteConfigurationMapping = 
	@{ 
		"IT" = "infosoftconfigtest"; 
	}

	# Basically this could have been whatever is in Version path minus bin dir, This will be ignored if auto-detect is set to true.
	#VirtualDirectories = @("Content", "Scripts", "Service References", "Views", "fonts")
	DefaultSiteWebConfig = @"
<?xml version="1.0"?>
<configuration>
  <configSections>
    <sectionGroup name="applicationSettings" type="System.Configuration.ApplicationSettingsGroup, System, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089">
      <section name="ReturnRegistration.Properties.Settings" type="System.Configuration.ClientSettingsSection, System, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089" requirePermission="false"/>
    </sectionGroup>
  </configSections>
  <applicationSettings>
    <ReturnRegistration.Properties.Settings>
      <setting name="Title" serializeAs="String">
        <value>{TITLECODEPLACEHOLDER}</value>
      </setting>
    </ReturnRegistration.Properties.Settings>
  </applicationSettings>
  <system.serviceModel>
    <client configSource="config/config.xml" />
  </system.serviceModel>
  <runtime>
    <assemblyBinding xmlns="urn:schemas-microsoft-com:asm.v1">
      <dependentAssembly>
        <assemblyIdentity name="Newtonsoft.Json" culture="neutral" publicKeyToken="30ad4fe6b2a6aeed"/>
        <bindingRedirect oldVersion="0.0.0.0-6.0.0.0" newVersion="6.0.0.0"/>
      </dependentAssembly>
      <dependentAssembly>
        <assemblyIdentity name="System.Web.Helpers" publicKeyToken="31bf3856ad364e35"/>
        <bindingRedirect oldVersion="1.0.0.0-3.0.0.0" newVersion="3.0.0.0"/>
      </dependentAssembly>
      <dependentAssembly>
        <assemblyIdentity name="System.Web.Mvc" publicKeyToken="31bf3856ad364e35"/>
        <bindingRedirect oldVersion="1.0.0.0-5.2.0.0" newVersion="5.2.0.0"/>
      </dependentAssembly>
      <dependentAssembly>
        <assemblyIdentity name="System.Web.Optimization" publicKeyToken="31bf3856ad364e35"/>
        <bindingRedirect oldVersion="1.0.0.0-1.1.0.0" newVersion="1.1.0.0"/>
      </dependentAssembly>
      <dependentAssembly>
        <assemblyIdentity name="System.Web.WebPages" publicKeyToken="31bf3856ad364e35"/>
        <bindingRedirect oldVersion="1.0.0.0-3.0.0.0" newVersion="3.0.0.0"/>
      </dependentAssembly>
      <dependentAssembly>
        <assemblyIdentity name="WebGrease" publicKeyToken="31bf3856ad364e35"/>
        <bindingRedirect oldVersion="0.0.0.0-1.5.2.14234" newVersion="1.5.2.14234"/>
      </dependentAssembly>
    </assemblyBinding>
  </runtime>
</configuration>
"@
}