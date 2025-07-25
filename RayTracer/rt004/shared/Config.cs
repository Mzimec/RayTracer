using rt004.shared;
using OpenTK.Mathematics;
using System.Text.Json;

namespace Util;

/// <summary>
/// Utility methods for configuration handling.
/// </summary>
public static class ConfigUtil {
    /// <summary>
    /// Loads a nullable <see cref="Vector3"/> from a float array.
    /// </summary>
    /// <param name="array">The array to convert. Must have length 3.</param>
    /// <returns>A <see cref="Vector3"/> if the array is valid; otherwise, null.</returns>
    /// <exception cref="ArgumentException">Thrown if the array length is not 3.</exception>
    public static Vector3? LoadNullableVector(float[]? array) {
        if (array == null || array.Length != 3) return null;
        if (array.Length != 3) throw new ArgumentException("Vector3 needs array of lenght 3 to inicialize");
        return new Vector3(array[0], array[1], array[2]);
    }
}

/// <summary>
/// Represents the main configuration for the renderer.
/// </summary>
public class Config {
    /// <summary>
    /// Gets or sets the output file name.
    /// </summary>
    public string OutputName { get; set; } = "render.pfm";

    /// <summary>
    /// Gets or sets the background color as an array of three floats.
    /// </summary>
    public float[] BackgroundColor { get; set; } = new float[] { 0, 0, 0 };

    /// <summary>
    /// Gets or sets the maximum recursion depth for ray tracing.
    /// </summary>
    public int MaxDepth { get; set; } = 30;

    /// <summary>
    /// Gets or sets the minimum contribution threshold for rays.
    /// </summary>
    public float MinContribution { get; set; } = 0.01f;

    /// <summary>
    /// Gets or sets the samples per pixel.
    /// </summary>
    public int Spp { get; set; } = 4;

    /// <summary>
    /// Flag for PathTracing option
    /// </summary>
    public bool IsPathTraced { get; set; } = false;

    /// <summary>
    /// Gets or sets the tone mapping configuration.
    /// </summary>
    public ToneMappingConfig ToneMapping { get; set; } = new ToneMappingConfig();

    /// <summary>
    /// Gets or sets the list of material configurations.
    /// </summary>
    public List<MaterialConfig> Materials { get; set; } = new List<MaterialConfig>();

    /// <summary>
    /// Gets or sets the scene graph configuration.
    /// </summary>
    public SceneGraphConfig? SceneGraph { get; set; }

    /// <summary>
    /// Loads a configuration from a JSON file.
    /// </summary>
    /// <param name="filePath">The path to the configuration file.</param>
    /// <returns>The loaded <see cref="Config"/> instance.</returns>
    /// <exception cref="Exception">Thrown if the file does not exist or cannot be read.</exception>
    public static Config Load(string filePath) {
        if (!File.Exists(filePath)) throw new Exception($"Config file '{filePath}' not found. Using default settings.");
        try {
            string json = File.ReadAllText(filePath);
            Console.WriteLine(json);
            return JsonSerializer.Deserialize<Config>(json) ?? new Config();
        }
        catch (Exception ex) {
            throw new Exception($"Error reading config file: {ex.Message}. Using default settings.");
        }
    }

    /// <summary>
    /// Checks if a material with the given name exists and returns it.
    /// </summary>
    /// <param name="materialName">The name of the material.</param>
    /// <param name="material">The found material, or null if not found.</param>
    /// <returns>True if the material exists; otherwise, false.</returns>
    public bool IsMaterialValid(string materialName, out Material? material) {
        material = Materials.FirstOrDefault(mat => mat.Name == materialName)?.ToMaterial();
        return material is not null;
    }

    /// <summary>
    /// Gets all materials defined in the configuration.
    /// </summary>
    /// <returns>A dictionary mapping material names to <see cref="Material"/> instances.</returns>
    public Dictionary<string, Material> GetMaterials() {
        Dictionary<string, Material> materials = new Dictionary<string, Material>();
        foreach (var mat in Materials) materials[mat.Name] = mat.ToMaterial();
        return materials;
    }

    /// <summary>
    /// Builds the scene graph from the configuration.
    /// </summary>
    /// <returns>The constructed <see cref="SceneGraph"/>.</returns>
    /// <exception cref="Exception">Thrown if the scene graph or root node is not defined.</exception>
    public SceneGraph GetSceneGraph() {
        if (SceneGraph == null) throw new Exception("Scene Graph is not defined in the config file.");
        if (SceneGraph.Root == null) throw new Exception("Root node is required in the scene graph configuration.");
        SceneGraph graph = new SceneGraph();
        SceneNode rootNode = SceneGraph.Root.ToSceneNode(this, graph);
        graph.Root.SceneGraph = graph; // Set the scene graph reference for the root node
        return graph;
    }
}

/// <summary>
/// Represents the configuration for the scene graph.
/// </summary>
public class SceneGraphConfig {
    /// <summary>
    /// Gets or sets the root node configuration.
    /// </summary>
    public required SceneNodeConfig Root { get; set; }
}

/// <summary>
/// Represents the configuration for a scene node.
/// </summary>
public class SceneNodeConfig {
    /// <summary>
    /// Gets or sets the name of the node.
    /// </summary>
    public required string Name { get; set; }

    /// <summary>
    /// Gets or sets the type of the node (e.g., "Mesh", "Light", "Camera").
    /// </summary>
    public required string Type { get; set; }

    /// <summary>
    /// Gets or sets the subtype of the node (e.g., shape type).
    /// </summary>
    public string? Subtype { get; set; }

    /// <summary>
    /// Gets or sets the transform configuration.
    /// </summary>
    public TransformConfig? Transform { get; set; }

    /// <summary>
    /// Gets or sets the child nodes.
    /// </summary>
    public List<SceneNodeConfig>? Children { get; set; }

    /// <summary>
    /// Gets or sets the material name.
    /// </summary>
    public string? Material { get; set; }

    /// <summary>
    /// Gets or sets the field of view (for cameras).
    /// </summary>
    public float FieldOfView { get; set; }

    /// <summary>
    /// Gets or sets the image width (for cameras).
    /// </summary>
    public int Width { get; set; }

    /// <summary>
    /// Gets or sets the image height (for cameras).
    /// </summary>
    public int Height { get; set; }

    /// <summary>
    /// Gets or sets the intensity (for lights).
    /// </summary>
    public float[]? Intensity { get; set; }

    /// <summary>
    /// Gets or sets the direction (for directional lights).
    /// </summary>
    public float[]? Direction { get; set; }

    /// <summary>
    /// Converts this configuration to a <see cref="SceneNode"/>.
    /// </summary>
    /// <param name="config">The main configuration object.</param>
    /// <param name="graph">The scene graph to which the node will belong.</param>
    /// <returns>The constructed <see cref="SceneNode"/>.</returns>
    /// <exception cref="Exception">Thrown if the node type is unknown or required data is missing.</exception>
    public SceneNode ToSceneNode(Config config, SceneGraph graph) {
        switch (Type) {
            case "Mesh":
                return ToMesh(config, graph);
            case "Light":
                return ToLightSource(graph);
            case "Camera":
                return ToCamera(graph);
            case "InnerNode":
                return ToInnerNode(config, graph);
            default:
                throw new Exception($"Unknown scene node type: {Type}. Expected 'Mesh', 'Light', 'Camera', or 'InnerNode'.");
        }
    }

    private SceneObject ToMesh(Config config, SceneGraph graph) {
        Material? material = null;
        if (Material is not null && !config.IsMaterialValid(Material, out material))
            throw new Exception($"Material '{Material}' not found in the configuration.");
        SceneObject obj;
        switch (Subtype) {
            case "Sphere":
                obj = new SceneObject(Name, new Sphere(), Transform?.ToTransform(), material);
                break;
            case "Cylinder":
                obj = new SceneObject(Name, new Cylinder(), Transform?.ToTransform(), material);
                break;
            case "Plane":
                obj = new SceneObject(Name, new Plane(), Transform?.ToTransform(), material);
                break;
            default:
                throw new Exception($"Unknown shape type: {Subtype}");
        }
        obj.SceneGraph = graph; // Set the scene graph reference for the object
        return obj;
    }

    private LightSource ToLightSource(SceneGraph graph) {
        if (Subtype == null) throw new Exception("Subtype is required for Light type.");
        Vector3 intensity = Intensity != null ? new Vector3(Intensity[0], Intensity[1], Intensity[2]) : throw new Exception("Intenisty is required for Light Source");
        LightSource lightSource;
        switch (Subtype) {
            case "Point":
                lightSource = new PointLight(Name, intensity, Transform?.ToTransform());
                break;
            case "Directional":
                Vector3 direction = Direction != null ? new Vector3(Direction[0], Direction[1], Direction[2]) : throw new Exception("Direction is required for Directional Light");
                lightSource = new DirectionalLight(Name, intensity, direction);
                break;
            case "Ambient":
                lightSource = new AmbientLight(Name, intensity);
                break;
            default:
                throw new Exception($"Unknown light type: {Subtype}");
        }
        lightSource.SceneGraph = graph; // Set the scene graph reference for the light source
        return lightSource;
    }

    private Camera ToCamera(SceneGraph graph) {
        if (FieldOfView <= 0 || Width <= 0 || Height <= 0) {
            throw new Exception("FieldOfView, Width, and Height must be positive values for Camera.");
        }
        Camera camnera = new PerspectiveCamera(Name, FieldOfView, Width, Height, Transform?.ToTransform());
        camnera.SceneGraph = graph; // Set the scene graph reference for the camera
        return camnera;
    }

    private InnerNode ToInnerNode(Config config, SceneGraph graph) {
        Material? material = null;
        if (Material is not null && !config.IsMaterialValid(Material, out material))
            throw new Exception($"Material '{Material}' not found in the configuration.");
        InnerNode node = new InnerNode(Name, Transform?.ToTransform(), material);
        node.SceneGraph = graph; // Set the scene graph reference for the inner node
        foreach (var child in Children ?? new List<SceneNodeConfig>()) {
            node.AddChild(child.ToSceneNode(config, graph));
        }
        return node;
    }
}

/// <summary>
/// Represents the configuration for a material.
/// </summary>
public class MaterialConfig {
    /// <summary>
    /// Gets or sets the material name.
    /// </summary>
    public required string Name { get; set; }

    /// <summary>
    /// Gets or sets the scatter model names.
    /// </summary>
    public string[]? ScatterModels { get; set; }

    /// <summary>
    /// Gets or sets the weights for scatter models.
    /// </summary>
    public float[]? Weights { get; set; }

    /// <summary>
    /// Gets or sets the emissive model names.
    /// </summary>
    public string[]? EmissiveModels { get; set; }

    /// <summary>
    /// Gets or sets the ambient color.
    /// </summary>
    public float[]? Ambient { get; set; }

    /// <summary>
    /// Gets or sets the diffuse color.
    /// </summary>
    public float[]? Diffuse { get; set; }

    /// <summary>
    /// Gets or sets the specular color.
    /// </summary>
    public float[]? Specular { get; set; }

    /// <summary>
    /// Gets or sets the transmittance color.
    /// </summary>
    public float[]? Transmittance { get; set; }

    /// <summary>
    /// Gets or sets the shininess factor.
    /// </summary>
    public float Shininess { get; set; }

    /// <summary>
    /// Gets or sets the reflectivity factor.
    /// </summary>
    public float Reflectivity { get; set; }

    /// <summary>
    /// Gets or sets the transparency factor.
    /// </summary>
    public float Transparency { get; set; }

    /// <summary>
    /// Gets or sets the refractive index.
    /// </summary>
    public float RefractiveIndex { get; set; }

    /// <summary>
    /// Gets or sets the fuzziness factor.
    /// </summary>
    public float Fuzziness { get; set; }

    /// <summary>
    /// Gets or sets the diffuse texture file path.
    /// </summary>
    public string? DiffuseTexture { get; set; }

    /// <summary>
    /// Gets or sets the normal texture file path.
    /// </summary>
    public string? NormalTexture { get; set; }

    /// <summary>
    /// Gets or sets the noise texture file path.
    /// </summary>
    public string? NoiseTexture { get; set; }

    /// <summary>
    /// Converts this configuration to a <see cref="Material"/>.
    /// </summary>
    /// <returns>The constructed <see cref="Material"/>.</returns>
    /// <exception cref="Exception">Thrown if scatter or emissive models are invalid.</exception>
    public Material ToMaterial() {
        Material material = new Material(
            Name,
            new List<(IScatterModel, float)>(), // No scatter models defined in the config
            new List<IEmissiveModel>(), // No emissive models defined in the config
            ConfigUtil.LoadNullableVector(Ambient),
            ConfigUtil.LoadNullableVector(Diffuse),
            ConfigUtil.LoadNullableVector(Specular),
            ConfigUtil.LoadNullableVector(Transmittance),
            Shininess,
            Reflectivity,
            Transparency,
            RefractiveIndex,
            Fuzziness,
            DiffuseTexture != null ? new BitmapTexture(DiffuseTexture) : null,
            NormalTexture != null ? new BitmapTexture(NormalTexture) : null,
            NoiseTexture != null ? ToNoiseTexture(NoiseTexture) : null
        );
        List<IScatterModel> models = new List<IScatterModel>();
        List<float> weights = new List<float>();
        List<float> indirectWeights = new List<float>();
        if (ScatterModels != null && Weights != null) {
            if (ScatterModels.Length != Weights.Length) {
                throw new Exception("Scatter models and weights must have the same length.");
            }
            for (int i = 0; i < ScatterModels.Length; i++) {
                IScatterModel model = ToScatterModel(ScatterModels[i], material);
                if (model == null) {
                    throw new Exception($"Unknown scatter model: {ScatterModels[i]}");
                }
                models.Add(model);
                weights.Add(Weights[i]);
            }
        }

        List<IEmissiveModel> emissiveModels = new List<IEmissiveModel>();
        if (EmissiveModels != null) {
            foreach (var modelType in EmissiveModels) {
                IEmissiveModel emissiveModel = ToEmissiveModel(modelType, material);
                if (emissiveModel == null) {
                    throw new Exception($"Unknown emissive model: {modelType}");
                }
                emissiveModels.Add(emissiveModel);
            }
        }

        material.ScatterModel = new CompositeScatterModel(models.Zip(weights, (model, weight) => (model, weight)).ToList(),
            emissiveModels);
        return material;
    }

    private IScatterModel ToScatterModel(string modelType, Material material) {
        return modelType switch
        {
            "LambertianDiffuse" => new LambertianDiffuse(material),
            "PerfectReflection" => new PerfectReflection(material),
            "FuzzyReflection" => new FuzzyReflection(material),
            "DielectricRefraction" => new DielectricRefraction(material),
            "PhongSpecularModel" => new PhongSpecularModel(material),
            _ => throw new Exception($"Unknown scatter model: {modelType}")
        };
    }

    private IEmissiveModel ToEmissiveModel(string modelType, Material material) {
        return modelType switch
        {
            "ConstantEmission" => new ConstantEmissionModel(material),
            _ => throw new Exception($"Unknown emissive model: {modelType}")
        };
    }

    private NoiseTexture ToNoiseTexture(string s) {
        switch (s) {
            case "Wood": return new NoiseTexture(ProceduralGenerators.Wood, scale: 3.0f);
            case "Marble": return new NoiseTexture(ProceduralGenerators.Marble, scale: 3.0f);
            default: throw new Exception($"Unknown noise texture type: {s}");
        }
    }
}

/// <summary>
/// Represents the configuration for a transform.
/// </summary>
public class TransformConfig {
    /// <summary>
    /// Gets or sets the position as an array of three floats.
    /// </summary>
    public required float[] Position { get; set; }

    /// <summary>
    /// Gets or sets the rotation as Euler angles in degrees.
    /// </summary>
    public required float[] Rotation { get; set; }

    /// <summary>
    /// Gets or sets the scale as an array of three floats.
    /// </summary>
    public required float[] Scale { get; set; }

    /// <summary>
    /// Converts this configuration to a <see cref="Transform"/>.
    /// </summary>
    /// <returns>The constructed <see cref="Transform"/>.</returns>
    public Transform ToTransform() {
        return new Transform(
            new Vector3(Position[0], Position[1], Position[2]),
            new Vector3(Rotation[0], Rotation[1], Rotation[2]),
            new Vector3(Scale[0], Scale[1], Scale[2])
        );
    }
}

/// <summary>
/// Represents the configuration for tone mapping.
/// </summary>
public class ToneMappingConfig {
    /// <summary>
    /// Gets or sets a value indicating whether tone mapping is enabled.
    /// </summary>
    public bool Enabled { get; set; } = false;

    /// <summary>
    /// Gets or sets the exposure factor for tone mapping.
    /// </summary>
    public float Exposure { get; set; } = 1.0f;

    /// <summary>
    /// Gets or sets the optional output name for the tone-mapped image.
    /// </summary>
    public string? OutputName { get; set; } = null;
}