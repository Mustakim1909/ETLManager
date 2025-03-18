using Common.Config;
using Common.DataAccess.MsSql;
using Common.Security;
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
                   services.Configure<Appsettings>(context.Configuration.GetSection("ETLAppSettings"));
                   services.Configure<List<WatcherConfig>>(context.Configuration.GetSection("WatcherConfigs"));
                   services.AddHostedService<ETLManagerWatcher>();
               });
    }
}