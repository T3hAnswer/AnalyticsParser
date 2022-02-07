using Microsoft.Extensions.Hosting;
using System;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace AnalyticsParser
{
    public class Worker : BackgroundService
    {
        private readonly WorkerOptions _options;

        //need these two lines here to avoid garbage collector from killing the timers
        private static Timer renameTimer;
        private static Timer moveTimer;

        public Worker(WorkerOptions options)
        {
            this._options = options;
        }

        public string LastFile;

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {

            renameTimer = new Timer(
                e => FileEnumerate(_options.SourcePath),
                null,
                TimeSpan.Zero,
                TimeSpan.FromSeconds(30));


            moveTimer = new Timer(
                e => FileMover(_options.SourcePath, _options.TargetPath),
                null,
                TimeSpan.FromSeconds(15),
                TimeSpan.FromSeconds(45));

            await Task.Delay(100, stoppingToken);
        }

        /// <summary>
        /// renames the files not caught by FileSystemWatcher
        /// </summary>
        /// <param name="sourcePath"></param>
        private void FileEnumerate(string sourcePath)
        {
            string sourceDirectory = sourcePath;
            try
            {
                var txtFiles = Directory.EnumerateFiles(sourceDirectory, _options.FileFilter, SearchOption.TopDirectoryOnly);
                foreach (string currentFile in txtFiles)
                {
                    //changes filename if duplicate exists
                    int count = 1;
                    string fileNameOnly = Path.GetFileNameWithoutExtension(currentFile);
                    string extension = FileHandling(currentFile);
                    string path = Path.GetDirectoryName(currentFile);
                    string newFullPath = Path.ChangeExtension(currentFile, extension);

                    while (File.Exists(newFullPath))
                    {
                        string tempFileName = $"{fileNameOnly}_{count++}";
                        newFullPath = Path.Combine(path!, tempFileName + extension);
                    }
                    Directory.Move(currentFile, newFullPath);
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
            }
        }
        /// <summary>
        /// Searches sourcePath for any files with extension .complete and then moves them to TargetPath
        /// </summary>
        /// <param name="sourcePath"></param>
        /// <param name="targetPath"></param>
        private void FileMover(string sourcePath, string targetPath)
        {
            string sourceDirectory = sourcePath;
            try
            {
                var txtFiles = Directory.EnumerateFiles(sourceDirectory, "*.*", SearchOption.TopDirectoryOnly).Where(s => s.EndsWith(".complete") || s.EndsWith(".fail"));

                foreach (string currentFile in txtFiles)
                {
                    string archiveDirectory = targetPath;
                    int count = 1;
                    string fileNameOnly = Path.GetFileNameWithoutExtension(currentFile);
                    string extension = Path.GetExtension(currentFile);
                    DateTime dateTime = File.GetLastWriteTime(currentFile);
                    if (extension is ".fail")
                    {
                        archiveDirectory = _options.ToEmail;
                    }
                    FileInfo file =
                        new FileInfo(archiveDirectory + "/" + dateTime.ToString("dd-MM-yyyy") + "/" +
                                     fileNameOnly + extension);
                    file.Directory?.Create();

                    string newFullPath = file.FullName;

                    while (File.Exists(newFullPath))
                    {
                        string tempFileName = $"{fileNameOnly}_{count++}";
                        newFullPath = Path.GetDirectoryName(newFullPath);
                        newFullPath = Path.Combine(newFullPath!, tempFileName + extension);
                    }
                    Directory.Move(currentFile, newFullPath);
                }
            }
            catch (IOException)
            {

            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message + $"Destination or source folder not found");
                //Following part would help keep application running if someone accidentally renamed/deleted/moved the folders,
                //but any existing logs would obviously not carry over.
                Console.WriteLine("Attempting to recreate folders");
                Directory.CreateDirectory(targetPath);
                Directory.CreateDirectory(sourcePath);
            }
        }

        /// <summary>
        /// Method to handle the data in the files
        /// </summary>
        /// <param name="currentFile"></param>
        private string FileHandling(string currentFile)
        {
            string extension = ".complete";
            try
            {
                foreach (string line in File.ReadLines(currentFile))
                {
                    if (line.Contains("EMERGENCY ") | line.Contains("Faulty"))
                    {
                        extension = ".fail";
                        return extension;
                    }
                }
                return extension;
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                extension =_options.FileFilter;
                return extension;
            }
        }
    }

}
