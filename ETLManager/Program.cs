using Common.Config;
using Common.DataAccess.MsSql;
using Common.Security;
using ETL.Service.ETLDemoFactorySetting;
using ETL.Service.Model;
using ETL.Service.Repo.Interface;
using ETLManager.Service.Model;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Serilog;
using Serilog.Events;
using System.Configuration;


namespace ETLManager
{
    internal class Program
    {
        public static QueryHelper _queryHelper = null;

        public static void Main(string[] args)
        {
            string logDirectory = Path.Combine(AppContext.BaseDirectory, "log");
            //Console.WriteLine(logDirectory);
            string logFilePath = Path.Combine(logDirectory, "TraceFile.txt");

            // Ensure the log directory exists
            if (!Directory.Exists(logDirectory))
            {
                Directory.CreateDirectory(logDirectory);
            }

            Log.Logger = new LoggerConfiguration()
                .MinimumLevel.Override("Microsoft.AspNetCore", LogEventLevel.Warning)
                .MinimumLevel.Override("Microsoft", LogEventLevel.Warning)
                .MinimumLevel.Override("System", LogEventLevel.Warning)
                .Enrich.FromLogContext()
                .WriteTo.File(logFilePath, rollingInterval: RollingInterval.Day, retainedFileCountLimit: null)
                .CreateLogger();

            var configuration = new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile("appsettings.json")
                .Build();


            //Log.Logger = new LoggerConfiguration()
            //    .ReadFrom.Configuration(configuration)
            //    .CreateLogger();

            try
            {
                Log.Information("Application starting up");
                Console.WriteLine("Application starting up");   

                //var host = CreateHostBuilder(args);

                //await host.RunConsoleAsync();

                CreateHostBuilder(args).Build().Run();

            }
            catch (Exception ex)
            {
                Log.Fatal(ex, "Application start-up failed");
                Console.WriteLine($"{ex} Application start-up failed");
            }   
            finally
            {
                Log.CloseAndFlush();
            }

        }


        public static IHostBuilder CreateHostBuilder(string[] args) =>
           Host.CreateDefaultBuilder(args)
            .UseWindowsService()
               .ConfigureServices((context, services) =>
               {
                   var config = new DbConfig();
                   var section = context.Configuration.GetSection("DatabaseSettings");
                   config = section.Get<DbConfig>();
                   var appSetting = new ETLAppSettings();
                   var appSettingSection = context.Configuration.GetSection("ETLAppSettings");
                   appSetting = appSettingSection.Get<ETLAppSettings>();
                   services.Configure<Appsettings>(context.Configuration.GetSection("ETLAppSettings"));
                   services.Configure<List<WatcherConfig>>(context.Configuration.GetSection("WatcherConfigs"));
                   services.AddHostedService<ETLManagerWatcher>();

                   services.AddScoped<IETLManagerService>(x =>
                   {


                       var configuration = context.Configuration;
                       var dbConfig = configuration.GetSection("DatabaseSettings").Get<DbConfig>();
                       var appSetting = configuration.GetSection("ETLAppSettings").Get<ETLAppSettings>();
                       //string connection = configuration.GetConnectionString("Database");
                       var encPassword = string.Empty;
                       var password = string.Empty;
                       var dbtype = dbConfig.DataProvider.ToLower().Trim();
                       if (dbtype == "sqlserver")
                       {
                           encPassword = dbConfig.ConnectionString.Split(';')[3].Substring(10);
                       }
                       else
                       {
                           encPassword = dbConfig.ConnectionString.Split(';')[3].Substring(9);
                       }
                           password = SecurityHelper.DecryptWithEmbedKey(encPassword);
                       //if (dbConfig.PasswordUtility != null && dbConfig.PasswordUtility.ToLower() == "old")
                       //{
                       //    password = SecurityHelper.DecryptWithEmbedKey(encPassword, 15);
                       //}
                       //else
                       //{
                       //    password = SecurityHelper.DecryptWithEmbedKey(encPassword);
                       //}

                       dbConfig.ConnectionString = dbConfig.ConnectionString.Replace(encPassword, password);

                       return ETLDemoServiceFactory.GetETLDemoService(dbConfig);
                   });
               });
    }
}