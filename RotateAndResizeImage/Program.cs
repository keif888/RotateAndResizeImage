/*

    Copyright (C) 2024  Keith Martin

    This program is free software: you can redistribute it and/or modify
    it under the terms of the GNU Affero General Public License as
    published by the Free Software Foundation, either version 3 of the
    License, or (at your option) any later version.

    This program is distributed in the hope that it will be useful,
    but WITHOUT ANY WARRANTY; without even the implied warranty of
    MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
    GNU Affero General Public License for more details.

    You should have received a copy of the GNU Affero General Public License
    along with this program.  If not, see <https://www.gnu.org/licenses/>.

*/

using CommandLine;
using CommandLine.Text;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using PhotoSauce.MagicScaler;
using RotateAndResizeImage;


internal class Program
{
#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.
    public static ServiceProvider ServiceProvider { get; set; }
    private static ILogger _logger;
    private static ServiceCollection services;
#pragma warning restore CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.

    private static int Main(string[] args)
    {
        int result = 0;

        Console.WriteLine("Initialising!!!");

        // Create the logger
        services = new ServiceCollection();

        // Create the command line parser
        var parser = new Parser(with => { with.EnableDashDash = true; with.IgnoreUnknownArguments = true; });

        // Parse the command line arguments
        var parserResult = parser.ParseArguments<FileCommandOptions, FolderCommandOptions>(args);

        // Process the results of the parsing
        parserResult
            .WithParsed<FileCommandOptions>(opt => { result = ReconfigureLogging(opt); })  // File command verb used
            .WithParsed<FolderCommandOptions>(opt => { result = ReconfigureLogging(opt); }) // Folder command verb used
            .WithNotParsed(x =>  // Help or bad input
            {
                var helpText = HelpText.AutoBuild(parserResult, h =>
                {
                    h.AutoHelp = true;     // hides --help
                    h.AutoVersion = true;  // hides --version
                    return HelpText.DefaultParsingErrorsHandler(parserResult, h);
                }, e => e, true);
                Console.WriteLine(helpText);
                result = -1;
            });

        // Parsing was ok, so execute the request
        if (result == 0)
        {
            parserResult
                .WithParsed<FileCommandOptions>(opt => { result = RunOptionsAndReturnExitCode(opt); })  // Process the individual file
                .WithParsed<FolderCommandOptions>(opt => { result = RunOptionsAndReturnExitCode(opt); });  // Process the folder
        }

        // Shut down the logger
        if (ServiceProvider != null)
        {
            try
            {
                ILoggerFactory loggerFactory = ServiceProvider.GetRequiredService<ILoggerFactory>();
                loggerFactory.Dispose();
            }
            catch (Exception)
            {
                // Who cares, we are trying to get out of here anyway.
            }
            ServiceProvider.Dispose();
        }
        return result;
    }


    /// <summary>
    /// Execute the individual file resize and rotate requested.
    /// </summary>
    /// <param name="o">FileCommandOptions object as parsed from command line</param>
    /// <returns>0 on success -1 on any errors.</returns>
    public static int RunOptionsAndReturnExitCode(FileCommandOptions o)
    {
        using (_logger.BeginScope(CurrentMethodName()))
        {
            // Check that the input file name is provided
            if (!string.IsNullOrWhiteSpace(o.InputFileName))
            {
                _logger.LogDebug("Ensure that file exist");
                if (!File.Exists(o.InputFileName))
                {
                    _logger.LogError("The InputFile {InputFileName} does not exist.", o.InputFileName);
                    return -1;
                }

                _logger.LogDebug("Check if the target file exists");
                if (File.Exists(o.OutputFileName) && !o.ForceOverwrite)
                {
                    _logger.LogError("The OutputFileName {OutputFileName} exists and ForceOverwrite is not enabled.", o.OutputFileName);
                    return -1;
                }
                else
                {
                    _logger.LogDebug("Delete the target file if required");
                    if (File.Exists(o.OutputFileName))
                    {
                        try
                        {
                            File.Delete(o.OutputFileName);
                        }
                        catch (Exception exc)
                        {
                            _logger.LogError(exc, "Exception thrown when deleting OuputFileName {OutputFileName}.", o.OutputFileName);
                            return -1;
                        }
                    }
                }

                return ProcessImage(o.InputFileName, o.OutputFileName, o.HorizontalSize, o.VerticalSize, o.DPI);
            }
            else
            {
                _logger.LogError("SNAFU.  Some how we got into file processing without a file name to process.");
                return -1;
            }
        }
    }


    /// <summary>
    /// Resize and rotate a single image.
    /// </summary>
    /// <param name="inputFileName">file name to resize</param>
    /// <param name="outputFileName">file name to write to.  If this already exists a .000 (or higher number) will be created instead.</param>
    /// <param name="horizontalSize"></param>
    /// <param name="verticalSize"></param>
    /// <param name="dPI"></param>
    /// <returns></returns>
    private static int ProcessImage(string inputFileName, string outputFileName, int horizontalSize, int verticalSize, int dPI)
    {
        _logger.LogDebug("Check if the target file exists");
        if (File.Exists(outputFileName))
        {
            try
            {
                outputFileName = IncrementFileName(outputFileName);
            }
            catch (Exception exc)
            {
                _logger.LogError(exc, "Exception whilst incrementing file name.");
                return -1;
            }
        }

        _logger.LogInformation("Processing file {inputFileName} to {outputFileName}", inputFileName, outputFileName);

        ImageFileInfo inputInfo = ImageFileInfo.Load(inputFileName);
        if (inputInfo == null)
        {
            _logger.LogError("Unable to process inputfileName {inputFileName} using ImageFileInfo.Load", inputFileName);
            return -1;
        }
        else
        {
            if (inputInfo.Frames.Count == 0)
            {
                _logger.LogWarning("Unable to process file {inputFileName} as it has no frames.", inputFileName);
                return -1;
            }
            if (inputInfo.Frames.Count > 1)
            {
                _logger.LogWarning("Unable to process file {inputFileName} as it has multiple frames.", inputFileName);
                return -1;
            }
            bool isTargetLandscape = horizontalSize > verticalSize;
            bool isSourceLandscape = inputInfo.Frames[0].Width > inputInfo.Frames[0].Height;

            if ((isTargetLandscape && isSourceLandscape) || (!isTargetLandscape && isSourceLandscape))
            {
                if (inputInfo.Frames[0].Width <= horizontalSize && inputInfo.Frames[0].Height <= verticalSize)
                {
                    File.Copy(inputFileName, outputFileName);
                }
                else
                {
                    if (horizontalSize > inputInfo.Frames[0].Width) horizontalSize = 0;
                    if (verticalSize > inputInfo.Frames[0].Height) verticalSize = 0;
                    MagicImageProcessor.ProcessImage(inputFileName, outputFileName, new ProcessImageSettings { Width = horizontalSize, Height = verticalSize, DpiX = dPI, DpiY = dPI, MetadataNames = [@"/app1/ifd/gps/{ushort=2}", @"/app1/ifd/gps/{ushort=4}", @"/app1/ifd/{ushort=271}", @"/app1/ifd/{ushort=272}", @"/app1/ifd/exif/{ushort=37385}", "/app1/ifd/exif/{ushort=36867}"] });
                }
                DateTime lastWritten = File.GetLastWriteTime(inputFileName);
                File.SetCreationTime(outputFileName, lastWritten);
                File.SetLastWriteTime(outputFileName, lastWritten);
            }
            else if ((!isTargetLandscape && !isSourceLandscape)||(isTargetLandscape && !isSourceLandscape))
            {
                if (inputInfo.Frames[0].Height <= horizontalSize && inputInfo.Frames[0].Width <= verticalSize)
                {
                    File.Copy(inputFileName, outputFileName);
                }
                else
                {
                    if (horizontalSize > inputInfo.Frames[0].Height) horizontalSize = 0;
                    if (verticalSize > inputInfo.Frames[0].Width) verticalSize = 0;
                    MagicImageProcessor.ProcessImage(inputFileName, outputFileName, new ProcessImageSettings { Width = verticalSize, Height = horizontalSize, DpiX = dPI, DpiY = dPI, MetadataNames = [@"/app1/ifd/gps/{ushort=2}", @"/app1/ifd/gps/{ushort=4}", @"/app1/ifd/{ushort=271}", @"/app1/ifd/{ushort=272}", @"/app1/ifd/exif/{ushort=37385}", "/app1/ifd/exif/{ushort=36867}"] });
                }
                DateTime lastWritten = File.GetLastWriteTime(inputFileName);
                File.SetCreationTime(outputFileName, lastWritten);
                File.SetLastWriteTime(outputFileName, lastWritten);
            }
        }
        return 0;
    }

    /// <summary>
    /// Given a file name, put an incrementer before the extension, and keep incrementing until you do not find a file by that name.
    /// </summary>
    /// <param name="outputFileName">The file name to increment</param>
    /// <returns>The incremented file name</returns>
    /// <exception cref="Exception">Generic exception thrown if there is no available increment between 000 and 999 inclusive</exception>
    private static string IncrementFileName(string outputFileName)
    {
        string? folderName = Path.GetDirectoryName(outputFileName);
        if (folderName == null)
        {
            _logger.LogError("Unable to get folder name from file name {outputFileName}.", outputFileName);
            throw new Exception("Incrementation Error");
        }
        string startOfName = Path.GetFileNameWithoutExtension(outputFileName);
        string fileExtension = Path.GetExtension(outputFileName);
        int increment = 0;
        string newFileName = String.Format("{0}\\{1}.{2:D3}{3}", folderName, startOfName, increment, fileExtension);
        while (File.Exists(newFileName))
        {
            newFileName = String.Format("{0}\\{1}.{2:D3}{3}", folderName, startOfName, ++increment, fileExtension);
            if (increment > 999)
            {
                _logger.LogError("Unable to increment file name {outputFileName} as there are already 999 increments.", outputFileName);
                throw new Exception("Incrementation Error");
            }
        }
        return newFileName;
    }


    /// <summary>
    /// Where the input is a folder, find all files in that folder that meet the mask, and rotate/resize them.
    /// </summary>
    /// <param name="o">FolderCommandOptions input</param>
    /// <returns>0 on success or -1 on failure.</returns>
    public static int RunOptionsAndReturnExitCode(FolderCommandOptions o)
    {
        int result = 0;
        using (_logger.BeginScope(CurrentMethodName()))
        {
            string? sourceFolder = Path.GetDirectoryName(o.InputFolderPathAndMask);
            if (sourceFolder == null)
            {
                _logger.LogError("The InputFolder {InputFolderPathAndMask} is not valid.", o.InputFolderPathAndMask);
                return -1;
            }

            string? folderMask = Path.GetFileName(o.InputFolderPathAndMask);
            if (folderMask == null)
            {
                _logger.LogError("The InputFolder's Mask {InputFolderPathAndMask} is not valid.", o.InputFolderPathAndMask);
                return -1;
            }
            if (folderMask.Length < 3)
            {
                _logger.LogError("The InputFolder's Mask {InputFolderPathAndMask} is not valid (to short).", o.InputFolderPathAndMask);
                return -1;
            }
            string? pathExtension = Path.GetExtension(o.InputFolderPathAndMask);
            if (string.IsNullOrEmpty(pathExtension))
            {
                _logger.LogError("The InputFolder's Mask {InputFolderPathAndMask} is not valid (missing).", o.InputFolderPathAndMask);
                return -1;
            }

            try
            {
                var fileNames = Directory.EnumerateFiles(sourceFolder, folderMask, SearchOption.TopDirectoryOnly);
                foreach (string inputfileName in fileNames)
                {
                    string targetFileName = o.OutputFolderPath + @"\" + Path.GetFileName(inputfileName);
                    int tempResult = ProcessImage(inputfileName, targetFileName, o.HorizontalSize, o.VerticalSize, o.DPI);
                    if (tempResult < result) { result = tempResult; }
                }
            }
            catch (Exception ex) { _logger.LogError(ex, "Exception whilst processing images"); }
        }
        return result;
    }


    #region Logging

    /// <summary>
    /// Ensures that the logging is done as per the command line requests
    /// </summary>
    /// <param name="opt"></param>
    /// <returns></returns>
    private static int ReconfigureLogging(FileCommandOptions opt)
    {
        if (!Enum.TryParse(opt.LogLevel, true, out LogLevel logLevel))
            logLevel = LogLevel.Warning;
        return ReconfigureLogging(logLevel);
    }

    /// <summary>
    /// Ensures that the logging is done as per the command line requests
    /// </summary>
    /// <param name="opt"></param>
    /// <returns></returns>
    private static int ReconfigureLogging(FolderCommandOptions opt)
    {
        if (!Enum.TryParse(opt.LogLevel, true, out LogLevel logLevel))
            logLevel = LogLevel.Warning;
        return ReconfigureLogging(logLevel);
    }
    /// <summary>
    /// Ensures that the logging is done as per the command line requests
    /// </summary>
    /// <param name="opt"></param>
    /// <returns></returns>
    private static int ReconfigureLogging(LogLevel logLevel)
    {
        ServiceProvider = ConfigureServices(logLevel);
        if (ServiceProvider.GetService(typeof(ILogger)) is not ILogger logger)
        { throw new Exception("Unable to instanciate logging service provider."); }
        else
        { _logger = logger; }
        return 0;
    }

    /// <summary>
    /// Configures the logging settings, so that debug etc. can be avaialble.
    /// </summary>
    /// <param name="opt"></param>
    /// <returns></returns>
    public static ServiceProvider ConfigureServices(LogLevel logLevel)
    {
        //services.Clear();
        if (logLevel == LogLevel.Debug)
        {
            services.AddLogging(loggingBuilder => loggingBuilder
              .AddSimpleConsole(options =>
              {
                  options.IncludeScopes = true;
                  options.SingleLine = false;
                  options.TimestampFormat = "yyyy-MM-dd HH:mm:ss.ffffff ";
                  options.ColorBehavior = Microsoft.Extensions.Logging.Console.LoggerColorBehavior.Enabled;
              })
              .AddDebug()
              .SetMinimumLevel(logLevel)
              );
        }
        else
        {
            services.AddLogging(loggingBuilder => loggingBuilder
            .AddSimpleConsole(options =>
            {
                options.IncludeScopes = true;
                options.SingleLine = false;
                options.TimestampFormat = "yyyy-MM-dd HH:mm:ss.ffffff ";
                options.ColorBehavior = Microsoft.Extensions.Logging.Console.LoggerColorBehavior.Enabled;
            })
             .SetMinimumLevel(logLevel)
            );
        }
        services.AddSingleton(sp => sp.GetRequiredService<ILoggerFactory>().CreateLogger("RotateAndResizeImage"));
        return services.BuildServiceProvider();
    }
    #endregion

    public static string CurrentMethodName([System.Runtime.CompilerServices.CallerMemberName] string memberName = "")
    {
        return memberName;
    }

}

