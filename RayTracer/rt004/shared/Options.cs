using CommandLine;

namespace Util;

/// <summary>
/// Represents command-line options for configuring the renderer.
/// </summary>
public class Options {
    /// <summary>
    /// Gets or sets the path to the configuration file.
    /// </summary>
    [Option('c', "config", Required = true, HelpText = "Path to the configuration file.")]
    public string? ConfigFile { get; set; }

    /// <summary>
    /// Gets or sets the maximum recursion depth for ray tracing.
    /// </summary>
    [Option('d', "depth", Required = false, HelpText = "Maximum recursion depth for ray tracing.")]
    public int? MaxDepth { get; set; }

    /// <summary>
    /// Gets or sets the minimum contribution for reflection/refraction.
    /// </summary>
    [Option('i', "min_contribution", Required = false, HelpText = "Minimum contribution for reflection/refraction.")]
    public float? MinContribution { get; set; }

    /// <summary>
    /// Gets or sets the output file name.
    /// </summary>
    [Option('o', "output", Required = false, HelpText = "Output file name.")]
    public string? FileName { get; set; }

    /// <summary>
    /// Gets or sets the samples per pixel for rendering.
    /// </summary>
    [Option('s', "spp", Required = false, HelpText = "Samples per pixel for rendering.")]
    public int? Spp { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether to enable path tracing mode.
    /// </summary>
    [Option('p', "path_tracing", Required = false, HelpText = "Enable path tracing mode.")]
    public bool? PathTracing { get; set; }

    /// <summary>
    /// Gets the size of tiles in the image for multithreading.
    /// </summary>
    [Option('t', "tile_size", Required = false, HelpText = "Size of the tiles for multithreading.")]
    public int? TileSize { get; set; }

    /// <summary>
    /// Gets value indicating whether the program runs on multiple threads.
    /// </summary>
    [Option('m', "multithreading", Required = false, HelpText = "Flag if the program runs on multiple threads")]
    public bool? Multithreading { get; set; }
}