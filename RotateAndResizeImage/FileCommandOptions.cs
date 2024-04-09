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

using CommandLine.Text;
using CommandLine;

namespace RotateAndResizeImage
{
    [Verb("file", HelpText = "Individual file processing")]
    public class FileCommandOptions
    {

        [Option('i', "InputFile", SetName = "FileOptions", Required = true, HelpText = "The name of the image file to be rotated (if needed) and resized (if needed).")]
#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.
        public string InputFileName { get; set; }
#pragma warning restore CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.

        [Option('o', "OutputFile", SetName = "FileOptions", Required = true, HelpText = "The name of the image file to be saved into.")]
#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.
        public string OutputFileName { get; set; }
#pragma warning restore CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.

        [Option('f', "ForceOverwrite", Required = false, HelpText = "Will replace the target file if it already exists.", Default = false)]
        public bool ForceOverwrite { get; set; }

        [Option('h', "HorizontalSize", Required = false, HelpText = "The Horizontal Size to scale to (or 0 for Auto)", Default = 0)]
        public int HorizontalSize { get; set; } = 0;

        [Option('v', "VerticalSize", Required = false, HelpText = "The Vertical Size to scale to (or 0 for Auto)", Default = 0)]
        public int VerticalSize { get; set; } = 0;

        [Option('d', "DPI", Required = false, HelpText = "The DPI to apply to the output scaled image.", Default = 264)]
        public int DPI { get; set; } = 264;

        [Option('l', "LogLevel", Required = false, HelpText = "The level of output from the logger (None, Critical, Error, Warning, Information, Debug, Trace).", Default = "Warning")]
#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.
        public string LogLevel { get; set; }
#pragma warning restore CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.

        [Usage(ApplicationAlias = "RotateAndResizeImage")]
        public static IEnumerable<Example> Examples
        {
            get
            {
                return [
            new Example("Shrink file to iPad 5 size", new FileCommandOptions { InputFileName = "source.jpg", OutputFileName = "target.jpg", HorizontalSize = 2048 })
          ];
            }
        }


    }
}