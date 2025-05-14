using CommandLine;

namespace Util;

public class Options {
    [Option("config", Required = true, HelpText = "Path to the configuration file.")]
    public string? ConfigFile { get; set; }

    [Option("width", Required = false, HelpText = "Image width.")]
    public int? Width { get; set; }

    [Option("height", Required = false, HelpText = "Image height.")]
    public int? Height { get; set; }

    [Option("output", Required = false, HelpText = "Output file name.")]
    public string? FileName { get; set; }

    [Option("render_type", Required = false, HelpText = "Render type: 0 for basic, 1 with shadcasting, 2 with reflections, 3+ with refractions.")]
    public int? RenderType { get; set; }
}
