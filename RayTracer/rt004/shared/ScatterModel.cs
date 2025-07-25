using OpenTK.Mathematics;

namespace rt004.shared {
    /// <summary>
    /// Interface for all scattering models, which define how rays interact with surfaces.
    /// </summary>
    public interface IScatterModel {
        /// <summary>
        /// Computes the scattered ray given an incoming ray and a hit record.
        /// </summary>
        /// <param name="rayIn">The incoming ray.</param>
        /// <param name="hit">The hit record at the intersection point.</param>
        /// <param name="random">Random number generator for stochastic models.</param>
        /// <returns>The scattered ray.</returns>
        Ray Scatter(Ray rayIn, HitRecord hit, Random random);

        /// <summary>
        /// Computes the intensity of the scattered ray.
        /// </summary>
        /// <param name="rayIn">The incoming ray.</param>
        /// <param name="rayOut">The outgoing (scattered) ray.</param>
        /// <param name="hit">The hit record at the intersection point.</param>
        /// <returns>The intensity as a <see cref="Vector3"/>.</returns>
        Vector3 GetScatterIntensity(Ray rayIn, Ray rayOut, HitRecord hit);

        /// <summary>
        /// Gets the probability density function (PDF) value for the given scattering event.
        /// </summary>
        /// <param name="rayIn">The incoming ray.</param>
        /// <param name="rayOut">The outgoing (scattered) ray.</param>
        /// <param name="hit">The hit record at the intersection point.</param>
        /// <returns>The PDF value.</returns>
        float GetPdf(Ray rayIn, Ray rayOut, HitRecord hit);

        /// <summary>
        /// Gets whether this model is used for direct lighting.
        /// </summary>
        bool IsDirect { get; }

        /// <summary>
        /// Gets whether this model is used for indirect lighting.
        /// </summary>
        bool IsIndirect { get; }
    }

    /// <summary>
    /// Composite scatter model that combines multiple scattering and emissive models.
    /// </summary>
    public class CompositeScatterModel {
        private IReadOnlyList<(IScatterModel model, float weight)> _models;
        private IReadOnlyList<IEmissiveModel> _emissiveModels;

        /// <summary>
        /// Gets whether this composite model contains any emissive models.
        /// </summary>
        public bool IsEmissive => _emissiveModels.Count > 0;

        /// <summary>
        /// Initializes a new instance of the <see cref="CompositeScatterModel"/> class.
        /// </summary>
        /// <param name="models">The list of scatter models and their weights.</param>
        /// <param name="eModels">The list of emissive models.</param>
        public CompositeScatterModel(IReadOnlyList<(IScatterModel, float)> models, IReadOnlyList<IEmissiveModel> eModels) {
            this._models = NormalizeWeights(models);
            this._emissiveModels = eModels;
        }

        /// <summary>
        /// Samples a scattered ray and its intensity from the composite model.
        /// </summary>
        /// <param name="rayIn">The incoming ray.</param>
        /// <param name="hit">The hit record at the intersection point.</param>
        /// <returns>A tuple containing the scattered ray and its intensity.</returns>
        public (Ray ray, Vector3 Intensity) Sample(Ray rayIn, HitRecord hit, bool isPtahTraced) {
            (IScatterModel? model, float weight) = GetRandomModel(Random.Shared, isPtahTraced);
            return ProccesModel(model, weight, rayIn, hit);
        }

        /// <summary>
        /// Returns all indirect scattered rays and their intensities.
        /// </summary>
        /// <param name="rayIn">The incoming ray.</param>
        /// <param name="hit">The hit record at the intersection point.</param>
        /// <returns>A list of tuples containing scattered rays and their intensities.</returns>
        public List<(Ray ray, Vector3 intensity)> Scatter(Ray rayIn, HitRecord hit, bool isPathTraced) {
            List<(Ray, Vector3)> rays = new();
            var indirectModels = new List<(IScatterModel model, float weight)>(_models);
            if (!isPathTraced) indirectModels = _models.Where(m => m.model.IsIndirect).ToList();
            foreach (var (model, weight) in indirectModels) rays.Add(ProccesModel(model, weight, rayIn, hit));
            return rays;
        }

        /// <summary>
        /// Computes the total direct scatter intensity for the given event.
        /// </summary>
        /// <param name="rayIn">The incoming ray.</param>
        /// <param name="rayOut">The outgoing (scattered) ray.</param>
        /// <param name="hit">The hit record at the intersection point.</param>
        /// <returns>The total intensity as a <see cref="Vector3"/>.</returns>
        public Vector3 GetScatterIntensity(Ray rayIn, Ray rayOut, HitRecord hit) {
            Vector3 totalIntensity = Vector3.Zero;
            float cosTheta = MathF.Max(0f, Vector3.Dot(rayOut.Direction, hit.Normal));
            foreach (var (model, weight) in _models.Where(m => m.model.IsDirect)) {
                totalIntensity += model.GetScatterIntensity(rayIn, rayOut, hit) * weight * cosTheta;
            }
            return totalIntensity;
        }

        /// <summary>
        /// Computes the total emission from all emissive models.
        /// </summary>
        /// <param name="rayIn">The incoming ray.</param>
        /// <param name="hit">The hit record at the intersection point.</param>
        /// <returns>The total emitted color as a <see cref="Vector3"/>.</returns>
        public Vector3 Emit(Ray rayIn, HitRecord hit) {
            Vector3 totalEmission = Vector3.Zero;
            foreach (var model in _emissiveModels) {
                totalEmission += model.Emit(rayIn, hit);
            }
            return totalEmission;
        }

        private IReadOnlyList<(IScatterModel, float)> NormalizeWeights(IReadOnlyList<(IScatterModel model, float weight)> models) {
            float total = models.Sum(m => m.weight);
            List<(IScatterModel model, float weight)> newModels = new List<(IScatterModel model, float weight)>();
            if (total > 0) {
                for (int i = 0; i < models.Count; i++) {
                    newModels.Add((models[i].model, models[i].weight / total));
                }
            }
            return newModels;
        }

        private (IScatterModel? model, float weight) GetRandomModel(Random random, bool isPathTraced) {
            var indirectModels = new List<(IScatterModel model, float weight)>(_models);
            if (!isPathTraced) indirectModels = _models.Where(m => m.model.IsIndirect).ToList();
            float totalWeight = indirectModels.Sum(m => m.weight);
            float randomValue = random.NextSingle();
            float cumulativeWeight = 0f;
            foreach (var m in indirectModels) {
                cumulativeWeight += m.weight / totalWeight;
                if (randomValue < cumulativeWeight) {
                    return m;
                }
            }
            // Fallback in case of rounding errors
            if (indirectModels.Count > 0) return indirectModels.Last();
            return (null, 0f); // No valid model found
        }

        private (Ray ray, Vector3 Intensity) ProccesModel(IScatterModel? model, float weight, Ray rayIn, HitRecord hit) {
            (Ray, Vector3) error = (new Ray(hit.Point, hit.Normal), Vector3.Zero);
            if (model is null) return error; // No model found
            Ray rayOut = model.Scatter(rayIn, hit, Random.Shared);
            if (rayOut.Direction == Vector3.Zero) return error; // Skip invalid rays
            float pdf = model.GetPdf(rayIn, rayOut, hit);
            if (pdf <= 0f) {
                return error;
            }

            // Final weight = (BRDF * cosTheta) / PDF * model váha
            // BRDF * cos(theta)
            float cosTheta = MathF.Max(0f, Vector3.Dot(-rayIn.Direction, hit.Normal));
            Vector3 f = model.GetScatterIntensity(rayIn, rayOut, hit);
            Vector3 intensity = (f * cosTheta / pdf) * weight;
            return (rayOut, intensity);
        }
    }

    /// <summary>
    /// Lambertian diffuse scattering model (ideal matte surface).
    /// </summary>
    public class LambertianDiffuse : IScatterModel {
        /// <inheritdoc/>
        public bool IsDirect => true;
        /// <inheritdoc/>
        public bool IsIndirect => false;

        public LambertianDiffuse(Vector3 albedo) {
        }

        public LambertianDiffuse(Material material) { }

        /// <inheritdoc/>
        public Ray Scatter(Ray rayIn, HitRecord hit, Random random) {
            Vector3 localDir = Vector3Extensions.RandomCosineDirection();
            Vector3Extensions.CreateONB(hit.Normal, out Vector3 tangent, out Vector3 bitangent);
            Vector3 worldDir = tangent * localDir.X + hit.Normal * localDir.Y + bitangent * localDir.Z;
            return new Ray(hit.Point * hit.Normal * Constants.Epsilon, Vector3.Normalize(worldDir));
        }

        /// <inheritdoc/>
        public Vector3 GetScatterIntensity(Ray rayIn, Ray rayOut, HitRecord hit) {
            return hit.Material.GetDiffuse(hit) / MathF.PI;
        }

        /// <inheritdoc/>
        public float GetPdf(Ray rayIn, Ray rayOut, HitRecord hit) {
            float cosine = Vector3.Dot(rayOut.Direction, hit.Normal);
            return cosine > 0 ? cosine / MathF.PI : 0;
        }
    }

    /// <summary>
    /// Perfect mirror-like reflection scattering model.
    /// </summary>
    public class PerfectReflection : IScatterModel {
        private readonly Vector3 _reflectance;
        /// <inheritdoc/>
        public bool IsDirect => false;
        /// <inheritdoc/>
        public bool IsIndirect => true;

        public PerfectReflection(Vector3 reflectance) {
            this._reflectance = reflectance;
        }

        public PerfectReflection(Material material) {
            _reflectance = material.Specular * material.Reflectivity;
        }

        /// <inheritdoc/>
        public Ray Scatter(Ray rayIn, HitRecord hit, Random random) {
            Vector3 dir = Vector3.Normalize(rayIn.Direction.Reflect(hit.Normal));
            float dot = Vector3.Dot(hit.Normal, dir);
            if (Vector3.Dot(dir, hit.Normal) <= 0) {
                return new(hit.Point, Vector3.Zero); // No valid reflection
            };
            return new Ray(hit.Point + hit.Normal * Constants.Epsilon, Vector3.Normalize(dir));
        }

        /// <inheritdoc/>
        public Vector3 GetScatterIntensity(Ray rayIn, Ray rayOut, HitRecord hit) {
            return _reflectance;
        }

        /// <inheritdoc/>
        public float GetPdf(Ray rayIn, Ray rayOut, HitRecord hit) {
            return 1.0f;
        }
    }

    /// <summary>
    /// Fuzzy (rough) reflection scattering model.
    /// </summary>
    public class FuzzyReflection : IScatterModel {
        private readonly float _fuzz;
        /// <inheritdoc/>
        public bool IsDirect => true;
        /// <inheritdoc/>
        public bool IsIndirect => false;

        public FuzzyReflection(float fuzz) {
            this._fuzz = Math.Clamp(fuzz, 0f, 1f);
        }

        public FuzzyReflection(Material material) {
            _fuzz = Math.Clamp(material.Fuzziness, 0f, 1f);
        }

        /// <inheritdoc/>
        public Ray Scatter(Ray rayIn, HitRecord hit, Random random) {
            Vector3 reflectedDir = rayIn.Direction.Reflect(hit.Normal);
            Vector3 fuzzed = reflectedDir + _fuzz * Vector3Extensions.RandomInUnitSphere();
            Vector3 dir = Vector3.Normalize(fuzzed);
            if (Vector3.Dot(dir, hit.Normal) <= 0) {
                return default; // No valid reflection
            }
            return new Ray(hit.Point + hit.Normal * Constants.Epsilon, dir);
        }

        /// <inheritdoc/>
        public Vector3 GetScatterIntensity(Ray rayIn, Ray rayOut, HitRecord hit) {
            return hit.Material.Specular / MathF.PI;
        }

        /// <inheritdoc/>
        public float GetPdf(Ray rayIn, Ray rayOut, HitRecord hit) {
            float cosine = Vector3.Dot(rayOut.Direction, hit.Normal);
            return cosine > 0 ? cosine / MathF.PI : 0;
        }
    }

    /// <summary>
    /// Dielectric (transparent) refraction scattering model.
    /// </summary>
    public class DielectricRefraction : IScatterModel {
        private readonly float _refractiveIndex;
        private readonly Vector3 _transmittance;
        private readonly Vector3 _specular;
        private bool _wasReflected = false;
        /// <inheritdoc/>
        public bool IsDirect => false;
        /// <inheritdoc/>
        public bool IsIndirect => true;

        public DielectricRefraction(float refractiveIndex, Vector3? transmittance = null, Vector3? specular = null) {
            this._refractiveIndex = refractiveIndex;
            this._transmittance = transmittance ?? Vector3.One;
            this._specular = specular ?? Vector3.One;
        }

        public DielectricRefraction(Material material) {
            this._refractiveIndex = material.RefractiveIndex;
            this._transmittance = material.Transmittance;
            this._specular = material.Specular * material.Reflectivity;
        }

        /// <inheritdoc/>
        public Ray Scatter(Ray rayIn, HitRecord hit, Random random) {
            Vector3 unitDirection = Vector3.Normalize(rayIn.Direction);
            float cosTheta = MathF.Min(Vector3.Dot(-unitDirection, hit.Normal), 1.0f);
            float sinTheta = MathF.Sqrt(1.0f - cosTheta * cosTheta);

            float eta = hit.IsFrontFace ? 1f / _refractiveIndex : _refractiveIndex;

            bool cannotRefract = eta * sinTheta > 1.0f;
            bool shouldReflect = cannotRefract; //|| Reflectance(cosTheta, eta) > random.NextSingle();

            Ray rayOut;

            if (shouldReflect) {
                rayOut = new Ray(hit.Point + hit.Normal * Constants.Epsilon, Vector3.Normalize(unitDirection.Reflect(hit.Normal)));
                _wasReflected = true;
            }
            else {
                Vector3 rOutPerp = eta * (unitDirection + cosTheta * hit.Normal);
                Vector3 rOutParallel = -MathF.Sqrt(MathF.Abs(1.0f - rOutPerp.LengthSquared)) * hit.Normal;
                rayOut = new Ray(hit.Point - hit.Normal * Constants.Epsilon, Vector3.Normalize(rOutPerp + rOutParallel));
                _wasReflected = false;
            }
            return rayOut;
        }

        /// <inheritdoc/>
        public Vector3 GetScatterIntensity(Ray rayIn, Ray rayOut, HitRecord hit) {
            if (_wasReflected) {
                return _specular;
            }

            if (hit.IsFrontFace) {
                return Vector3.One;
            }

            float distance = Vector3.Distance(rayIn.Origin, hit.Point);
            Vector3 intensity = new(
                MathF.Exp((_transmittance.X - 1f) * distance),
                MathF.Exp((_transmittance.Y - 1f) * distance),
                MathF.Exp((_transmittance.Z - 1f) * distance)
            );
            return intensity;
        }

        /// <inheritdoc/>
        public float GetPdf(Ray rayIn, Ray rayOut, HitRecord hit) {
            return 1.0f;
        }

        private static float Reflectance(float cosine, float refIdx) {
            float r0 = (1 - refIdx) / (1 + refIdx);
            r0 *= r0;
            return r0 + (1 - r0) * MathF.Pow(1 - cosine, 5);
        }
    }

    /// <summary>
    /// Phong specular reflection scattering model.
    /// </summary>
    public class PhongSpecularModel : IScatterModel {
        private readonly Vector3 _specularColor;
        private readonly float _shininess;
        /// <inheritdoc/>
        public bool IsDirect => true;
        /// <inheritdoc/>
        public bool IsIndirect => false;

        public PhongSpecularModel(Vector3 specularColor, float shininess) {
            _specularColor = specularColor;
            _shininess = shininess;
        }

        public PhongSpecularModel(Material material) {
            _specularColor = material.Specular;
            _shininess = material.Shininess;
        }

        /// <inheritdoc/>
        public Ray Scatter(Ray rayIn, HitRecord hit, Random random) {
            Vector3 inDir = Vector3.Normalize(rayIn.Direction);
            Vector3 perfectReflection = inDir.Reflect(hit.Normal);

            Vector3 sampledDir = SamplePhongLobe(perfectReflection, hit.Normal, _shininess, random);

            if (Vector3.Dot(sampledDir, hit.Normal) <= 0f) {
                return default;
            }

            return new Ray(hit.Point, sampledDir);
        }

        /// <inheritdoc/>
        public Vector3 GetScatterIntensity(Ray rayIn, Ray rayOut, HitRecord hit) {
            Vector3 inDir = Vector3.Normalize(rayIn.Direction);
            Vector3 outDir = Vector3.Normalize(rayOut.Direction);
            Vector3 perfectReflection = inDir.Reflect(hit.Normal);

            float cosAlpha = Vector3.Dot(outDir, perfectReflection);
            if (cosAlpha <= 0f) return Vector3.Zero;

            float brdfValue = ((_shininess + 2f) / (2f * MathF.PI)) * MathF.Pow(cosAlpha, _shininess);
            return _specularColor * brdfValue * GetPdf(rayIn, rayOut, hit);
        }

        /// <inheritdoc/>
        public float GetPdf(Ray rayIn, Ray rayOut, HitRecord hit) {
            Vector3 inDir = Vector3.Normalize(rayIn.Direction);
            Vector3 outDir = Vector3.Normalize(rayOut.Direction);
            Vector3 perfectReflection = inDir.Reflect(hit.Normal);

            float cosAlpha = Vector3.Dot(outDir, perfectReflection);
            if (cosAlpha <= 0f) return 0f;

            return ((_shininess + 1f) / (2f * MathF.PI)) * MathF.Pow(cosAlpha, _shininess);
        }

        private static Vector3 SamplePhongLobe(Vector3 reflectionDir, Vector3 normal, float shininess, Random random) {
            float u1 = random.NextSingle();
            float u2 = random.NextSingle();

            float phi = 2f * MathF.PI * u1;
            float cosTheta = MathF.Pow(u2, 1f / (shininess + 1f));
            float sinTheta = MathF.Sqrt(1f - cosTheta * cosTheta);

            Vector3 localDir = new(
                sinTheta * MathF.Cos(phi),
                sinTheta * MathF.Sin(phi),
                cosTheta
            );

            return ToWorld(localDir, reflectionDir);
        }

        private static Vector3 ToWorld(Vector3 local, Vector3 zAxis) {
            Vector3 z = Vector3.Normalize(zAxis);
            Vector3 up = MathF.Abs(z.Z) < 0.999f ? Vector3.UnitZ : Vector3.UnitX;
            Vector3 x = Vector3.Normalize(Vector3.Cross(up, z));
            Vector3 y = Vector3.Cross(z, x);
            return local.X * x + local.Y * y + local.Z * z;
        }
    }
}