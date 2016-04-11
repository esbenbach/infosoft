namespace CoverityDefectUpdater
{
    using System;
    using BusinessLogic;
    using NDesk.Options.Extensions;
    using Serilog;
    using Serilog.Events;

    class Program
    {
        static int Main(string[] args)
        {
            var options = new RequiredValuesOptionSet();
            var simulation = options.AddSwitch("r|simulation", "Show what would happen without actually applying changes");
            var legacy = options.AddSwitch("l|legacy", "Assign owners to legacy defects");
            var streams = options.AddRequiredVariableList<string>("s|stream", "Assign ownership of defects in Stream (You can specify multiple instances of the s flag to have more streams)");
            var username = options.AddRequiredVariable<string>("user", "Coverity Connect Username");
            var password = options.AddRequiredVariable<string>("password", "Coverity Connect Password");
            var tfsCollectionUrl = options.AddRequiredVariable<string>("c|collection", "TFS Project Collection URL, such as http://yourtfs:8080/tfs/ProjectCollection");
            var tfsProjectName = options.AddRequiredVariable<string>("p|project", "TFS Project Name");
            var tfsBranchRoot = options.AddRequiredVariable<string>("b|branch", "TFS Branch Root Path relative to the project, such as /Main/Version1");

            var consoleManager = new ConsoleManager("Coverity Defect Updater", options, "?|help", "Show help message and exit");

            if (!consoleManager.TryParseOrShowHelp(Console.Out, args))
            {
                return -1;
            }

            ConfigureSerilog();

            var serviceFactory = new CoverityServiceFactory(username.Value, password.Value);
            var configurationClient = new CoverityConfigurationClient(serviceFactory);
            var defectsClient = new CoverityDefectsClient(serviceFactory);
            var tfsHistoryClient = new TfsHistoryClient(tfsCollectionUrl.Value);
            var defectUpdate = new DefectUpdater(configurationClient, defectsClient, tfsHistoryClient, simulation, legacy);

            foreach(var stream in streams)
            {
                defectUpdate.AssignUnassignedDefectsAsync(stream, tfsProjectName.Value, tfsBranchRoot.Value).GetAwaiter().GetResult();
            }

            return 0;
        }

        private static void ConfigureSerilog()
        {
            Log.Logger = new LoggerConfiguration()
                .MinimumLevel.Debug()
                .WriteTo.RollingFile("DebugLog.txt")
                .WriteTo.ColoredConsole(restrictedToMinimumLevel: LogEventLevel.Information)
                .CreateLogger();
        }
    }
}
