using Common.DataAccess.PostgreSql;
using ETL.Service.Repo.Interface;
using ETLManager.Service.Model;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using Serilog;
using System.Diagnostics;

namespace ETLManager
{
    public class ETLManagerWatcher : IHostedService
    {
        private readonly List<FileSystemWatcher> _watchers;
        private readonly Appsettings _appsettings;
        private readonly List<WatcherConfig> _watcherConfigs;
        private static readonly object _logLock = new object();
        private readonly IETLManagerService _eTLManagerService;

        public ETLManagerWatcher(IOptions<Appsettings> appsettings, IOptions<List<WatcherConfig>> watcherConfigs,IETLManagerService eTLManagerService)
        {
            _appsettings = appsettings.Value;
            _eTLManagerService = eTLManagerService;
            _watchers = new List<FileSystemWatcher>();
            _watcherConfigs = watcherConfigs.Value;
            // Initialize the watchers based on the path and domain configurations
            foreach (var config in _watcherConfigs)
            {
                var fullPath1 = Path.Combine(config.Path, config.DomainName);
                var watcher = new FileSystemWatcher()
                {
                    Path = fullPath1,
                    IncludeSubdirectories = true,
                    NotifyFilter = NotifyFilters.FileName | NotifyFilters.LastWrite | NotifyFilters.Size
                };
                watcher.Created += OnCreated;
                watcher.InternalBufferSize = 65536;
                watcher.IncludeSubdirectories = true;
                _watchers.Add(watcher);
            }
        }

        private void LogThreadSafe(string message)
        {
            lock (_logLock)
            {
                Console.WriteLine(message);
                Log.Information(message); // Assuming you are using Serilog for logging
            }
        }
        public async Task StartAsync(CancellationToken cancellationToken)
        {
            try
            {
                LogThreadSafe("Start");

                // Check and ensure that directories exist before watching  
                foreach (var watcher in _watchers)
                {
                    if (!Directory.Exists(watcher.Path))
                    {
                        LogThreadSafe($"WatcherPath:{watcher.Path} does not exist.");
                        continue;
                    }
                }

                // Start the watchers
                foreach (var watcher in _watchers)
                {
                    watcher.EnableRaisingEvents = true;
                }

                // Create one task per domain
                var domainTasks = new List<Task>();

                foreach (var config in _watcherConfigs)
                {
                    await _eTLManagerService.GetConnectionString(config.DomainName);

                    // Create a single task for each domain
                    domainTasks.Add(Task.Run(async () =>
                    {
                        var taskName = $"Processing domain: {config.DomainName}";
                        LogThreadSafe($"Started task: {taskName}");

                        // Process files for the domain
                        await ProcessDomainFiles(config.Path, config.DomainName, cancellationToken);
                    }));
                }
                // Wait for all domain tasks t  o complete
                await Task.WhenAll(domainTasks);
                
            }
            catch (Exception ex)
            {
                LogThreadSafe($"An error occurred while starting the watcher: {ex.Message}");
                throw;
            }
        }
        
        private async Task ProcessDomainFiles(string SourcePath, string domainName, CancellationToken cancellationToken)
        {
            try
            {
                LogThreadSafe($"Processing domain: {domainName}");

                List<string> files = new List<string>();
                var paths = await _eTLManagerService.GetPath();
                foreach (var path in paths)
                {
                    string fullPath = Path.Combine(SourcePath, domainName, path.FinPutSource);
                    if (!Directory.Exists(fullPath))
                    {
                        return;
                    }
                    // Get all files from the current domain path
                    var file = Directory.GetFiles(fullPath, "*.csv");
                    files.AddRange(file);
                }              

                var filesToProcess = new List<string>();
                // Process the files in this domain sequentially or asynchronously without thread for each file
                foreach (var file in files)
                {
                    var filename = file.Replace(" ", "_");

                    // Move the file if it doesn't already exist
                    if (!File.Exists(filename))
                    {
                        File.Move(file, filename);
                    }
                    filesToProcess.Add(filename);
                    // Process each file (No thread created here, only sequential file processing)
                    
                }
                try
                {
                    // Process all the files at once by passing the list to RetriveCsv
                    int batchSize = 10;
                    for (int i = 0; i < filesToProcess.Count; i += batchSize)
                    {
                        var batch = filesToProcess.Skip(i).Take(batchSize).ToList();
                        var result = await RetriveCsv(filesToProcess, CancellationToken.None);
                    }
                    //var result = await RetriveCsv(filesToProcess, CancellationToken.None);
                 }
                catch (Exception ex)
                {
                    LogThreadSafe($"Error processing files  {files}: {ex.Message}");
                    throw;
                }
            }
            catch (Exception ex)
            {
                LogThreadSafe($"Error processing domain {domainName}: {ex.Message}");
                throw;
            }
        }


        public async Task<bool> RetriveCsv(List<string> ResivefilePaths, CancellationToken cancellationToken)
        {
            List<string> filePaths = new List<string>();
            //Move files in InProcess
            foreach (var file in ResivefilePaths)
            {
                if (!file.Contains("Temp_Process"))
                {
                    string newPath = Path.Combine(Path.GetDirectoryName(file), "Temp_Process");
                    if (!Directory.Exists(newPath))
                    {
                        Directory.CreateDirectory(newPath);
                    }
                    var newFilePath = Path.Combine(newPath, $"{Path.GetFileNameWithoutExtension(file)}_WIP{Path.GetExtension(file)}");
                    File.Move(file, newFilePath);
                    Console.WriteLine($"New File Path : {newFilePath}");
                    filePaths.Add(newFilePath);
                }
            }


            // Log the files being fetched
            foreach (var filePath in filePaths)
            {
                LogThreadSafe($"File Fetched: {filePath}");
            }

            // Ensure the files are not validated before proceeding
            if (!filePaths.Any(fp => fp.ToLower().Contains("validated")))
            {
                try
                {
                    string exeFilePath = _appsettings.ETLPath;
                    if (exeFilePath == null)
                    {
                        LogThreadSafe("ETL path is null");
                        return false;
                    }

                    if (!File.Exists(exeFilePath))
                    {
                        LogThreadSafe("ETL path does not exist, please check the configuration file path");
                        return false;
                    }

                    if (_appsettings.Operatingsystem.ToLower().Contains("win"))
                    {
                        try
                        {
                            // Join all file paths into a single string separated by spaces
                            string filePathsArgument = JsonConvert.SerializeObject(filePaths);
                            filePathsArgument = "[" + filePathsArgument.Substring(1, filePathsArgument.Length - 2) + " ]";
                            string tempFilePath = Path.Combine(Path.GetTempPath(), "filePaths.txt");
                            File.WriteAllText(tempFilePath, filePathsArgument);

                            var startInfo = new ProcessStartInfo
                            {
                                FileName = exeFilePath,
                                Arguments = $"\"{tempFilePath}\"",  // Pass all file paths as arguments
                                UseShellExecute = false,
                                CreateNoWindow = true,
                                RedirectStandardOutput = true,
                                RedirectStandardError = true,
                            };

                            using (var process = new Process())
                            {
                                process.StartInfo = startInfo;
                                process.OutputDataReceived += (sender, e) =>
                                {
                                    if (e.Data != null) LogThreadSafe(e.Data);
                                };
                                process.ErrorDataReceived += (sender, e) =>
                                {
                                    if (e.Data != null) LogThreadSafe($"ERROR: {e.Data}");
                                };

                                process.Start();
                                process.BeginOutputReadLine();
                                process.BeginErrorReadLine();
                                process.WaitForExit();
                                return true;
                            }
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine($"Failed to open {ex.Message}");
                            return false;
                        }
                    }
                    else if (_appsettings.Operatingsystem.ToLower().Contains("lin"))
                    {                       
                        // Join all file paths into a single string separated by spaces
                        string filePathsArgument = string.Join(" ", filePaths);

                        var startInfo = new ProcessStartInfo
                        {
                            FileName = "dotnet",
                            Arguments = $"{exeFilePath} {filePathsArgument}",  // Pass all file paths as arguments
                            UseShellExecute = false,
                            CreateNoWindow = true,
                            RedirectStandardOutput = true,  // Capture standard output
                            RedirectStandardError = true,   // Capture errors
                            WorkingDirectory = Directory.GetCurrentDirectory()
                        };

                        try
                        {
                            using (var process = new Process())
                            {
                                process.StartInfo = startInfo;
                                process.OutputDataReceived += (sender, e) =>
                                {
                                    if (e.Data != null)
                                    {
                                        Console.WriteLine(e.Data); // Print output to console
                                    }
                                };

                                process.ErrorDataReceived += (sender, e) =>
                                {
                                    if (e.Data != null)
                                    {
                                        Console.WriteLine($"ERROR: {e.Data}"); // Print error to console
                                    }
                                };

                                process.Start();
                                process.BeginOutputReadLine();
                                process.BeginErrorReadLine();

                                process.WaitForExit();

                                if (process.ExitCode != 0)
                                {
                                    Console.WriteLine($"Process failed with exit code {process.ExitCode}");
                                }
                                else
                                {
                                    Console.WriteLine("Process completed successfully.");
                                }
                            }
                            return true;
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine($"Error starting process: {ex.Message}");
                            return false;
                        }
                    }
                    return true;
                }
                catch (Exception ex)
                {
                    LogThreadSafe($"An error occurred while trying to run the executable: {ex.Message}");
                    return false;
                }
            }
            else
            {
                return false;
            }
        }

        private async void OnCreated(object sender, FileSystemEventArgs e)
        {
            // Handle the file creation event
            if(!e.FullPath.Contains("Temp_Process") && !e.FullPath.Contains("Processed"))
            {
                if (e.FullPath.Contains("Input\\Source"))
                {
                    string directoryPath = Path.GetDirectoryName(e.FullPath);
                    var pathlist = directoryPath.Split(Path.DirectorySeparatorChar).ToList();
                    var domainname = pathlist[pathlist.Count - 4];
                    // Get all the files that match the filter in the directory
                    var files = Directory.GetFiles(directoryPath, _appsettings.WatcherFilter);

                    // List to hold files to be passed to RetriveCsv
                    var filesToProcess = new List<string>();

                    foreach (var file in files)
                    {
                        var filename = file.Replace(" ", "_");

                        // Move the file if not already moved
                        if (!File.Exists(filename))
                        {
                            File.Move(file, filename);
                        }

                        // Add the file to the list of files to process
                        filesToProcess.Add(filename);
                    }

                    try
                    {
                        // Process all the files at once by passing the list to RetriveCsv
                        int batchSize = 10;
                        for (int i = 0; i < filesToProcess.Count; i += batchSize)
                        {
                            var batch = filesToProcess.Skip(i).Take(batchSize).ToList();
                            var result = await RetriveCsv(filesToProcess, CancellationToken.None);
                        }

                    }
                    catch (Exception ex)
                    {
                        LogThreadSafe($"Error processing files in directory {directoryPath}: {ex.Message}");
                        throw;
                    }
                }
            }
            
        }
        public Task StopAsync(CancellationToken cancellationToken)
        {
            // Stop the watcher
            foreach (var watcher in _watchers)
            {
                watcher.EnableRaisingEvents = false;
            }

            return Task.CompletedTask;
        }
       
    }

}
