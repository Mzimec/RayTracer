using OpenTK.Mathematics;
using Util;

namespace rt004.shared {
    /// <summary>
    /// Represents a rectangular tile of the image for parallel rendering.
    /// </summary>
    public struct Tile {
        /// <summary>
        /// The starting X coordinate of the tile.
        /// </summary>
        public int StartX;
        /// <summary>
        /// The starting Y coordinate of the tile.
        /// </summary>
        public int StartY;
        /// <summary>
        /// The width of the tile.
        /// </summary>
        public int Width;
        /// <summary>
        /// The height of the tile.
        /// </summary>
        public int Height;

        /// <summary>
        /// Initializes a new instance of the <see cref="Tile"/> struct.
        /// </summary>
        /// <param name="startX">The starting X coordinate.</param>
        /// <param name="startY">The starting Y coordinate.</param>
        /// <param name="width">The width of the tile.</param>
        /// <param name="height">The height of the tile.</param>
        public Tile(int startX, int startY, int width, int height) {
            StartX = startX;
            StartY = startY;
            Width = width;
            Height = height;
        }
    }

    /// <summary>
    /// Main ray tracing engine for rendering scenes.
    /// </summary>
    public class RayTracer {
        private readonly SceneGraph _sceneGraph;
        private readonly int _maxDepth;
        private readonly float _minContribution;
        private readonly Vector3 _backgroundColor;
        private readonly int _spp;
        private readonly bool _isPathTraced;

        /// <summary>
        /// Initializes a new instance of the <see cref="RayTracer"/> class.
        /// </summary>
        /// <param name="graph">The scene graph to render.</param>
        /// <param name="backgroundColor">The background color.</param>
        /// <param name="maxDepth">Maximum recursion depth for rays.</param>
        /// <param name="minContribution">Minimum contribution threshold for rays.</param>
        /// <param name="spp">Samples per pixel.</param>
        public RayTracer(SceneGraph graph, Vector3 backgroundColor,
            int maxDepth = 30, float minContribution = 0.01f, int spp = 1, bool isPT = false) {

            this._sceneGraph = graph;
            this._backgroundColor = backgroundColor;
            this._maxDepth = maxDepth;
            this._minContribution = minContribution;
            this._spp = spp;
            this._isPathTraced = isPT;
        }

        /// <summary>
        /// Traces a ray through the scene and computes the resulting color.
        /// </summary>
        /// <param name="ray">The ray to trace.</param>
        /// <param name="depth">Current recursion depth.</param>
        /// <param name="contribution">Current contribution factor.</param>
        /// <returns>The computed color as a <see cref="Vector3"/>.</returns>
        public Vector3 TraceRay(Ray ray, int depth, float contribution) {

            if (depth > _maxDepth) return _backgroundColor;

            (SceneObject? obj, HitRecord hit) = FindClosestObject(ray);

            if (obj == null) return _backgroundColor;

            if (hit.Material.ScatterModel.IsEmissive)
                return hit.Material.ScatterModel.Emit(ray, hit);

            Vector3 color = Vector3.Zero;
            color += LightingContribution(ray, hit);

            (Ray rayOut, Vector3 intensity) = hit.Material.ScatterModel.Sample(ray, hit, _isPathTraced);
            if (intensity.Length > _minContribution) {
                Vector3 scatteredColor = TraceRay(rayOut, depth + 1, intensity.Length);
                color += scatteredColor * intensity;
            }
            return color;
        }

        /// <summary>
        /// Renders the scene to a floating-point image.
        /// </summary>
        /// <param name="tileSize">The size of each tile for parallel rendering.</param>
        /// <param name="useParallel">Whether to use parallel rendering.</param>
        /// <returns>The rendered image as a <see cref="FloatImage"/>.</returns>
        /// <exception cref="InvalidOperationException">Thrown if the camera is not set in the scene graph.</exception>
        public FloatImage Render(int tileSize = 16, bool useParallel = true) {
            if (_sceneGraph.Camera == null)
                throw new InvalidOperationException("Camera is not set in the scene graph.");

            int width = _sceneGraph.Camera.ImageWidth;
            int height = _sceneGraph.Camera.ImageHeight;
            FloatImage image = new FloatImage(width, height, 3);

            List<Tile> tiles = new();
            for (int x = 0; x < width; x += tileSize) {
                for (int y = 0; y < height; y += tileSize) {
                    int w = Math.Min(tileSize, width - x);
                    int h = Math.Min(tileSize, height - y);
                    tiles.Add(new Tile(x, y, w, h));
                }
            }

            if (useParallel) {
                List<Task> tasks = new();

                foreach (var tile in tiles) {
                    tasks.Add(Task.Run(() => {
                        RenderTile(tile, image, _sceneGraph.Camera);
                    }));
                }

                Task.WaitAll(tasks.ToArray());
            }
            else {
                foreach (var tile in tiles) {
                    RenderTile(tile, image, _sceneGraph.Camera);
                }
            }

            return image;
        }

        /// <summary>
        /// Computes the lighting contribution at a hit point.
        /// </summary>
        /// <param name="ray">The incoming ray.</param>
        /// <param name="hit">The hit record at the intersection point.</param>
        /// <returns>The lighting contribution as a <see cref="Vector3"/>.</returns>
        private Vector3 LightingContribution(Ray ray, HitRecord hit) {
            Vector3 lightContribution = Vector3.Zero;
            foreach (var (key, light) in _sceneGraph.Lights) {
                if (light is AmbientLight ambientLight) {
                    lightContribution += ambientLight.GetIntensity(hit.Point) * hit.Material.Ambient;
                }
                else {
                    Vector3 lightDir = light.GetDirection(hit.Point);
                    Vector3 viewDir = Vector3.Normalize(ray.Direction);
                    float lightDistance = 0;
                    bool isLightPositional = false;
                    if (light is PointLight positionalLight) {
                        isLightPositional = true;
                        lightDistance = Vector3.Distance(positionalLight.Transform.Position, hit.Point);
                    }
                    Vector3 shadowFactor = GetShadowFactor(hit, -lightDir, isLightPositional, lightDistance);
                    Vector3 lightIntensity = light.GetIntensity(hit.Point);
                    Ray rayOut = new Ray(hit.Point + hit.Normal * 0.5e-4f, -lightDir);
                    Vector3 scatterIntensity = hit.Material.ScatterModel.GetScatterIntensity(ray, new ScatterResult { RayOut = rayOut }, hit);

                    lightContribution += new Vector3(
                        lightIntensity.X * shadowFactor.X * scatterIntensity.X,
                        lightIntensity.Y * shadowFactor.Y * scatterIntensity.Y,
                        lightIntensity.Z * shadowFactor.Z * scatterIntensity.Z);
                }
            }
            return lightContribution;
        }

        /// <summary>
        /// Computes the shadow factor for a given hit point and light direction.
        /// </summary>
        /// <param name="hit">The hit record at the intersection point.</param>
        /// <param name="lightDir">The direction to the light.</param>
        /// <param name="isPositional">Whether the light is positional.</param>
        /// <param name="maxDistance">The maximum distance to check for occlusion.</param>
        /// <returns>The shadow factor as a <see cref="Vector3"/> (RGB transmittance).</returns>
        private Vector3 GetShadowFactor(HitRecord hit, Vector3 lightDir, bool isPositional, float maxDistance) {
            Vector3 transmittance = Vector3.One;

            Vector3 shadowOrigin = hit.Point + hit.Normal * 1e-4f;
            Ray shadowRay = new Ray(shadowOrigin, lightDir);

            while (true) {
                (SceneObject? closestObj, HitRecord closestHit) = FindClosestObject(shadowRay);

                if (closestHit.T >= maxDistance && isPositional) break;
                if (closestObj == null) break;
                if (closestObj.Material is null) {
                    shadowRay = new Ray(closestHit.Point - closestHit.Normal * Constants.Epsilon, shadowRay.Direction);
                    continue;
                }
                if (closestObj.Material.Transmittance == Vector3.Zero) {
                    transmittance = Vector3.Zero;
                    break;
                }

                Vector3 t = closestObj.Material.GetTransmitance(shadowRay, closestHit);

                transmittance *= t;

                if (transmittance.X <= 0.01f && transmittance.Y <= 0.01f && transmittance.Z <= 0.01f) {
                    transmittance = Vector3.Zero;
                    break;
                }

                Vector3 newOrigin = closestHit.Point - closestHit.Normal * Constants.Epsilon;
                shadowRay = new Ray(newOrigin, shadowRay.Direction);
            }

            return transmittance;
        }

        /// <summary>
        /// Finds the closest object intersected by a ray.
        /// </summary>
        /// <param name="ray">The ray to test for intersection.</param>
        /// <returns>
        /// A tuple containing the closest <see cref="SceneObject"/> (or null if none) and the corresponding <see cref="HitRecord"/>.
        /// </returns>
        private (SceneObject? obj, HitRecord hit) FindClosestObject(Ray ray) {
            HitRecord closestHit = default;
            closestHit.T = float.MaxValue;
            SceneObject? closestObj = null;
            foreach (var (key, obj) in _sceneGraph.Objects) {
                HitRecord tempHit = default;
                if (obj.Intersect(ray, ref tempHit)) {
                    if (tempHit.T < closestHit.T && tempHit.T > 0) {
                        closestHit = tempHit;
                        closestObj = obj;
                    }
                }
            }
            return (closestObj, closestHit);
        }

        /// <summary>
        /// Renders a single tile of the image.
        /// </summary>
        /// <param name="tile">The tile to render.</param>
        /// <param name="image">The image to write to.</param>
        /// <param name="camera">The camera to use for ray generation.</param>
        private void RenderTile(Tile tile, FloatImage image, Camera camera) {
            for (int x = tile.StartX; x < tile.StartX + tile.Width; x++) {
                for (int y = tile.StartY; y < tile.StartY + tile.Height; y++) {
                    Vector3 color = Vector3.Zero;
                    for (int i = 0; i < _spp; i++) {
                        Ray ray = camera.GenerateRay(x, y);
                        color += TraceRay(ray, 0, 1f);
                    }

                    color /= _spp;
                    color = Vector3.Clamp(color, Vector3.Zero, Vector3.One);

                    lock (image) {
                        image.PutPixel(x, y, new[] { color.X, color.Y, color.Z });
                    }
                }
            }
        }
    }
}