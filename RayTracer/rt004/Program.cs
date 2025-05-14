using System.Globalization;
using OpenTK.Mathematics;
using Util;
using CommandLine;
using System.Security.AccessControl;
using rt004.shared;
using System.Text.Json;

namespace rt004;

internal class Program {
    static void Main(string[] args) {
        Parser.Default.ParseArguments<Options>(args).WithParsed<Options>(o =>
            {
                // Setting  up our configuration based on config file and possible overrides.
                Config config = o.ConfigFile != null ? Config.Load(o.ConfigFile) : new Config();
                if(config == null) {
                    Console.WriteLine("Failed to load configuration. Exiting.");
                    return;
                }
                Console.WriteLine(JsonSerializer.Serialize(config, new JsonSerializerOptions { WriteIndented = true }));

                if (o.Height != null) config.height = o.Height.Value;
                if (o.Width != null) config.width = o.Width.Value;
                if (o.FileName != null) config.outputName = o.FileName;
                if (o.RenderType != null) config.renderType = o.RenderType.Value;
                Console.WriteLine($"Program is congigured with file {o.ConfigFile}.\n" +
                    $"With final overrides: Size: {config.width}x{config.height}, Output: {config.outputName}, RenderType: {config.renderType}");

                // Define Camera
                Camera camera = new PerspectiveCamera(
                    new Vector3(config.camera.position[0], config.camera.position[1], config.camera.position[2]),   // Camera Position
                    new Vector3(config.camera.lookAt[0], config.camera.lookAt[1], config.camera.lookAt[2]),         // Look At (Scene Center)
                    new Vector3(config.camera.up[0], config.camera.up[1], config.camera.up[2]),                     // Up Vector
                    config.camera.fieldOfView,                                                                      // Field of View
                    config.camera.width, config.camera.height                                                       // Image Resolution
                );

                // Define Materials
                Dictionary<string, Material> materials = new Dictionary<string, Material>();
                foreach (var mat in config.materials) {
                    materials[mat.name] = new Material(
                        new Vector3(mat.ambient[0], mat.ambient[1], mat.ambient[2]),
                        new Vector3(mat.diffuse[0], mat.diffuse[1], mat.diffuse[2]),
                        new Vector3(mat.specular[0], mat.specular[1], mat.specular[2]),
                        mat.shininess,
                        mat.reflectivity,
                        mat.transparency,
                        mat.refractiveIndex,
                        mat.isReflective,
                        mat.isTransparent
                    );
                }

                // Define Objects
                List<SceneObject> objects = new List<SceneObject>();
                foreach (var obj in config.objects) {
                    Material material = materials[obj.material];
                    Vector3 position = new Vector3(obj.position[0], obj.position[1], obj.position[2]);
                    Vector3 normal; ;
                    switch (obj.type) {
                        case "Sphere":
                            objects.Add(new Sphere(obj.radius, material, position));
                            break;
                        case "Plane":
                            normal = new Vector3(obj.normal[0], obj.normal[1], obj.normal[2]);
                            objects.Add(new Plane(normal, material, position));
                            break;
                        case "Cylinder":
                            normal = new Vector3(obj.normal[0], obj.normal[1], obj.normal[2]);
                            objects.Add(new Cylinder(normal, obj.radius, obj.height, material, position));
                            break;
                        default:
                            Console.WriteLine($"Unknown object type: {obj.type}");
                            break;
                    }
                }

                // Define Lights
                List<LightSource> lights = new List<LightSource>();
                foreach (var light in config.lights) {
                    Vector3 intensity = new Vector3(light.intensity[0], light.intensity[1], light.intensity[2]);
                    switch(light.type) {
                        case "DirectionalLight":
                            lights.Add(new DirectionalLight(intensity, new Vector3(light.position[0], light.position[1], light.position[2])));
                            break;
                        case "PointLight":
                            lights.Add(new PointLight(intensity, new Vector3(light.position[0], light.position[1], light.position[2])));
                            break;
                        case "AmbientLight":
                            lights.Add(new AmbientLight(intensity));
                            break;
                        default:
                            Console.WriteLine($"Unknown light type: {light.type}");
                            break;
                    }
                }

                // Create RayTracer
                RayTracer rayTracer;
                switch (config.renderType){
                    case 0:
                        rayTracer = new RayTracer(objects, lights, camera, 5, Vector3.Zero, false, false, false);
                        break;
                    case 1:
                        rayTracer = new RayTracer(objects, lights, camera, 5, Vector3.Zero, false, false, true);
                        break;
                    case 2:
                        rayTracer = new RayTracer(objects, lights, camera, 5, Vector3.Zero, true, false, true);
                        break;
                    default:
                        rayTracer = new RayTracer(objects, lights, camera, 5, Vector3.Zero, true, true, true);
                        break;
                }

                // Render Image
                var image = rayTracer.Render();

                MyUtil.SaveAsFloatImage(image, config.outputName);

            });
    }
    
        
        /*Parser.Default.ParseArguments<Options>(args).WithParsed<Options>(o =>
            {
                // Setting  up our configuration based on config file and possible overrides.
                Config config = o.ConfigFile != null ? Config.Load(o.ConfigFile) : new Config();

                if (o.Height != null) config.Height = o.Height.Value;
                if (o.Width != null) config.Width = o.Width.Value;
                if (o.FileName != null) config.FileName = o.FileName;
                Console.WriteLine($"Final Config: {config.Width}x{config.Height}, Output: {config.FileName}");

                GenerateFloatImage(config);
            });*/
    }

    /*static private void GenerateFloatImage(Config config) {
        // HDR image.
        FloatImage fi = new(config.Width, config.Height, 3);

        // Generating pattern
        static void GeneratePattern(int width, int height, FloatImage image, int depth) {
            if (depth == 0) return;
            for (int i = 0; i < width / 2; i++)
                for (int j = 0; j < height / 2; j++)
                    image.PutPixel(i, j, [0.7f, 0.3f, 0.3f]);
            for (int i = width / 2; i < width; i++)
                for (int j = 0; j < height / 2; j++)
                    image.PutPixel(i, j, [0.3f, 0.7f, 0.3f]);
            for (int i = 0; i < width / 2; i++)
                for (int j = height / 2; j < height; j++)
                    image.PutPixel(i, j, [0.3f, 0.3f, 0.7f]);
            GeneratePattern(width / 2, height / 2, image, depth - 1);
        }

        // Generating stuff into the image
        GeneratePattern(config.Width, config.Height, fi, 3);

        // Save the HDR image.
        if (config.FileName.EndsWith(".hdr"))
            fi.SaveHDR(config.FileName);     // HDR format is still buggy
        else
            fi.SavePFM(config.FileName);     // PFM format works well

        Console.WriteLine($"HDR image '{config.FileName}' is finished.");
    }
}*/

    /*// TODO: put anything interesting into the image
    // TODO: use command-line arguments [and config file?] to define image dimensions, output file name, etc.

    // Pilot: try to read two float numbers (3D camera rotation - pitch and yaw in degrees)
    // Test values: 10 30
    if (args.Length > 1 &&
        double.TryParse(args[0], NumberStyles.Float, CultureInfo.InvariantCulture, out double angleX) &&
        double.TryParse(args[1], NumberStyles.Float, CultureInfo.InvariantCulture, out double angleY))
    {
      // 3D intersection demo.
      double xMin = -1.0;
      double xMax = 1.0;
      double yMin = -(double)hei / wid;
      double yMax = -yMin;
      double inner = Math.Min(xMax, yMax);

      // AA box.
      Vector3d boxCorner = new(-inner * 0.6, -inner * 0.6, -inner * 0.6);
      Vector3d boxSize = new(inner * 1.2, inner * 1.2, inner * 1.2);

      // Triangles in the xz base plane.
      Vector3d[] Ab = { new(inner * -0.6, inner * -0.6, inner * -0.6), new(inner * -0.6, inner * -0.6, inner * -0.6) };
      Vector3d[] Bb = { new(inner *  0.6, inner * -0.6, inner * -0.6), new(inner *  0.6, inner * -0.6, inner *  0.6) };
      Vector3d[] Cb = { new(inner *  0.6, inner * -0.6, inner *  0.6), new(inner * -0.6, inner * -0.6, inner *  0.6) };

      // Triangle in the xy plane.
      Vector3d A = new(inner * -0.5, 0.0,          0.0);
      Vector3d B = new(inner *  0.4, inner * -0.4, 0.0);
      Vector3d C = new(inner *  0.1, inner *  0.4, 0.0);

      // Camera data.
      Vector3d p1  = new(0.0, 0.0, 1.0);     // orthographic ray direction (basic orientation = z) [vector]
      Vector3d P00 = new(xMin, yMax, -5.0);       // upper left corner of the screen [point]
      Vector3d dx  = new((xMax - xMin) / wid, 0.0, 0.0);   // horizontal pixel step [vector]
      Vector3d dy  = new(0.0, (yMin - yMax) / hei, 0.0);   // vertical pixel step [vector]

      // Camera rotation.
      Matrix4d m = Matrix4d.CreateRotationX(MathHelper.DegreesToRadians(angleX)) *     // pitch = "elevation" angle
                   Matrix4d.CreateRotationY(MathHelper.DegreesToRadians(-angleY));     // yaw   = "azimuth" angle
      P00 = Vector3d.TransformPosition(P00, m);
      p1  = Vector3d.TransformVector(p1, m);
      dx  = Vector3d.TransformVector(dx, m);
      dy  = Vector3d.TransformVector(dy, m);

      // Image synthesis.

      float[] boxColor  = { 0.0f, 0.2f, 0.2f };      // bounding box color
      float[] baseColor = { 0.3f, 0.3f, 0.2f };      // base rectangle color

      for (int y = 0; y < hei; y++)
      for (int x = 0; x < wid; x++)
      {
        // Single pixel [x, y]
        Vector3d P0 = P00 + x * dx + y * dy;
        float[]? color = null;

        // 1. bounding box.
        if (MathUtil.RayBoxIntersection(P0, p1, boxCorner, boxSize, out _))
          color = boxColor;

        // 2. base triangles.
        if (!double.IsInfinity(MathUtil.RayTriangleIntersection(P0, p1, Ab[0], Bb[0], Cb[0], out _)) ||
            !double.IsInfinity(MathUtil.RayTriangleIntersection(P0, p1, Ab[1], Bb[1], Cb[1], out _)))
          color = baseColor;

        // 3. triangle.
        if (!double.IsInfinity(MathUtil.RayTriangleIntersection(P0, p1, A, B, C, out var uv)))
        {
          // Intersection exists at (1 - uv.X - uv.Y) * A + uv.X * B + uv.Y * C).
          color = new[]
          {
            (float)(1.0 - uv.X - uv.Y),
            (float)uv.X,
            (float)uv.Y
          };
        }
        
        if (color != null)
          fi.PutPixel(x, y, color);
      }
    }
    else
    {
      // Example - putting one red pixel close to the upper left corner...
      float[] red = [1.0f, 0.1f, 0.1f];   // R, G, B
      fi.PutPixel(1, 1, red);
    }

    // Save the HDR image.
    if (fileName.EndsWith(".hdr"))
      fi.SaveHDR(fileName);     // HDR format is still buggy
    else
      fi.SavePFM(fileName);     // PFM format works well

    Console.WriteLine($"HDR image '{fileName}' is finished.");
  }
}*/
