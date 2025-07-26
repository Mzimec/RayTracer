using OpenTK.Mathematics;
using Util;
using CommandLine;
using rt004.shared;
using System.Text.Json;

namespace rt004;

/// <summary>
/// Entry point for the ray tracing application.
/// Handles command-line parsing, configuration loading, scene setup, and rendering.
/// </summary>
internal class Program {
    /// <summary>
    /// Application entry point. Parses command-line arguments, loads configuration,
    /// sets up the scene, and performs rendering.
    /// </summary>
    /// <param name="args">Command-line arguments.</param>
    static void Main(string[] args) {
        Parser.Default.ParseArguments<Options>(args).WithParsed<Options>(o =>
        {
            // Setting up our configuration based on config file and possible overrides.
            Config config = o.ConfigFile != null ? Config.Load(o.ConfigFile) : new Config();
            if (config == null) {
                Console.WriteLine("Failed to load configuration. Exiting.");
                return;
            }
            Console.WriteLine(JsonSerializer.Serialize(config, new JsonSerializerOptions { WriteIndented = true }));

            config.OutputName = o.FileName ?? config.OutputName;
            config.MaxDepth = o.MaxDepth ?? config.MaxDepth;
            config.MinContribution = o.MinContribution ?? config.MinContribution;
            config.Spp = o.Spp ?? config.Spp;
            config.IsPathTraced = o.PathTracing ?? config.IsPathTraced;
            config.TileSize = o.TileSize ?? config.TileSize;
            config.Multithreading = o.Multithreading ?? config.Multithreading;

            Console.WriteLine($"Program is congigured with file {o.ConfigFile}.\n");

            Vector3 backgroundColor = new Vector3(
                config.BackgroundColor[0],
                config.BackgroundColor[1],
                config.BackgroundColor[2]
            );
            SceneGraph sceneGraph = config.GetSceneGraph(); // Get the scene graph from the config
            Dictionary<string, Material> materials = config.GetMaterials(); // Get the materials from the config

            // Create RayTracer
            RayTracer rayTracer = new RayTracer(sceneGraph, backgroundColor, config.MaxDepth, config.MinContribution, config.Spp, config.IsPathTraced);

            // Render Image
            var image = rayTracer.Render(config.TileSize, config.Multithreading);

            if (config.ToneMapping.Enabled) {
                var ldrImage = ToneMapper.ApplyToneMapping(image);
                string ldrOutputName = config.ToneMapping.OutputName ?? Path.ChangeExtension(config.OutputName, "_ldr.png");
                ImageSaver.SaveLdrAsPng(ldrImage, ldrOutputName);
            }

            MyUtil.SaveAsFloatImage(image, config.OutputName);

        });
    }
}