# Rotate and Resize Image

# Project Description

C# .Net console application to resize down an existing image or folder of images, keeping the following EXIF attributes:  
1. Longitude
1. Lattitude
1. Camera Model
1. Camera Manufacturer
1. Date Taken

# Usage

## Individual File Processing

> RotateAndResizeImage.exe file
> -i, --InputFile         Required. The name of the image file to be rotated (if needed) and resized (if needed).
> -o, --OutputFile        Required. The name of the image file to be saved into.
> -f, --ForceOverwrite    (Default: false) Will replace the target file if it already exists.
> -h, --HorizontalSize    (Default: 0) The Horizontal Size to scale to (or 0 for Auto)
> -v, --VerticalSize      (Default: 0) The Vertical Size to scale to (or 0 for Auto)
> -d, --DPI               (Default: 264) The DPI to apply to the output scaled image.
> -l, --LogLevel          (Default: Warning) The level of output from the logger (None, Critical, Error, Warning, Information, Debug, Trace).

## Folder Processing

> RotateAndResizeImage.exe folder
> -s, --SourceFolder      Required. The name of the image file to be rotated (if needed) and resized (if needed).
> -t, --TargetFolder      Required. The name of the image file to be saved into.
> -h, --HorizontalSize    (Default: 0) The Horizontal Size to scale to (or 0 for Auto)
> -v, --VerticalSize      (Default: 0) The Vertical Size to scale to (or 0 for Auto)
> -d, --DPI               (Default: 264) The DPI to apply to the output scaled image.
> -l, --LogLevel          (Default: Warning) The level of output from the logger (None, Critical, Error, Warning, Information, Debug, Trace).


# nuget packages used

This project makes use of the following nuget packages.  
1. [CommandLineParser](https://www.nuget.org/packages/CommandLineParser/2.9.1)
1. [Microsoft.Extensions.Logging.Console](https://www.nuget.org/packages/Microsoft.Extensions.Logging.Console/8.0.0)
1. [Microsoft.Extensions.Logging.Debug](https://www.nuget.org/packages/Microsoft.Extensions.Logging.Debug/8.0.0)
1. [PhotoSauce.MagicScaler](https://www.nuget.org/packages/PhotoSauce.MagicScaler/0.14.2)

