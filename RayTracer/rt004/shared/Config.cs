using System.Numerics;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Util;

public class Config {
    public int width { get; set; }  = 200;
    public int height { get; set; } = 800;
    public string outputName { get; set; } = "render.pfm";
    public float[] backgroundColor { get; set; } = new float[] { 0, 0, 0 };

    public int renderType { get; set; } = 3;

    public CameraConfig camera { get; set; } = new CameraConfig();
    public List<MaterialConfig> materials { get; set; } = new List<MaterialConfig>();
    public List<ObjectConfig> objects { get; set; } = new List<ObjectConfig>();
    public List<LightConfig> lights { get; set; } = new List<LightConfig>();

    public static Config Load(string filePath) {
        if (!File.Exists(filePath)) {
            Console.WriteLine($"Config file '{filePath}' not found. Using default settings.");
            return new Config();
        }
        try {
            string json = File.ReadAllText(filePath);
            Console.WriteLine(json);
            return JsonSerializer.Deserialize<Config>(json) ?? new Config();
        }
        catch (Exception ex) {
            Console.WriteLine($"Error reading config file: {ex.Message}. Using default settings.");
            return new Config();
        }
    }
}

public class CameraConfig {
    public float[] position { get; set; }
    public float[] lookAt { get; set; }
    public float[] up { get; set; }
    public float fieldOfView { get; set; }
    public int width { get; set; }
    public int height { get; set; }
}

public class MaterialConfig {
    public required string name { get; set; }
    public required float[] ambient { get; set; }
    public required float[] diffuse { get; set; }
    public required float[] specular { get; set; }
    public float shininess { get; set; }
    public float reflectivity { get; set; }
    public float transparency { get; set; }
    public float refractiveIndex { get; set; }
    public bool isReflective { get; set; }
    public bool isTransparent { get; set; }
}

public class ObjectConfig {
    public required string type { get; set; }
    public required string material { get; set; }
    public required float[] position { get; set; }
    public float radius { get; set; } // For Sphere
    public float height { get; set; } // For Cylinder
    public required float[] normal { get; set; } // For Plane
}

public class LightConfig {
    public required string type { get; set; }
    public required float[] intensity { get; set; }
    public required float[] position { get; set; } // For PointLight
}

