using OpenTK.Mathematics;

namespace rt004.shared {
    public interface ILegacyScatterModel {
        IEnumerable<ScatterResult> Scatter(Ray ray, HitRecord hit, Vector3 pdf);
    }
    public struct ScatterResult {
        public Ray ScatteredRay { get; private set; }
        public Vector3 Attenuation { get; private set; }
        public float Pdf { get; private set; } // Probability Density Function value for the scattered ray
        public bool IsValid { get; private set; }

        public static ScatterResult None => new() { IsValid = false };
        public static ScatterResult Create(Ray scatteredRay, Vector3 attenuation, float pdf) =>
            new() { ScatteredRay = scatteredRay, Attenuation = attenuation, Pdf = pdf, IsValid = true };
    }


    public interface IReflectionModel {
        bool TryReflect(Ray ray, HitRecord hit, out Ray reflected, out Vector3 attenuation, out float pdf);
    }

    public interface IRefractionModel {
        bool TryRefract(Ray ray, HitRecord hit, out Ray refracted, out Vector3 attenuation, out float pdf);
    }

    public interface IDiffuseModel {
        bool TryDiffuse(Ray ray, HitRecord hit, out Ray scattered, out Vector3 attenuation, out float pdf);
    }
    public abstract class LegacyScatterModel : ILegacyScatterModel {
        protected readonly IDiffuseModel _diffuse;
        protected readonly IReflectionModel _reflection;
        protected readonly IRefractionModel _refraction;
        protected readonly Random _rng;

        protected LegacyScatterModel(IDiffuseModel diffuse, IReflectionModel reflection, IRefractionModel refraction, Random? rng = null) {
            this._diffuse = diffuse;
            this._reflection = reflection;
            this._refraction = refraction;
            this._rng = rng ?? Random.Shared;
        }
        public abstract IEnumerable<ScatterResult> Scatter(Ray ray, HitRecord hit, Vector3 pdf);
    }
    public class MonteCarloScatterModel : LegacyScatterModel {
        public MonteCarloScatterModel(IDiffuseModel diffuse, IReflectionModel reflection, IRefractionModel refraction, Random? rng = null)
            : base(diffuse, reflection, refraction, rng) { }

        public override IEnumerable<ScatterResult> Scatter(Ray ray, HitRecord hit, Vector3 p) {
            Vector3 distribution = p.ToDistribution(); // Normalize the PDF to ensure it sums to 1
            float r = _rng.NextSingle();
            if (distribution.X > r && _refraction.TryRefract(ray, hit, out var refracted, out var attenRefract, out var pdfRefr)) {
                yield return ScatterResult.Create(refracted, attenRefract, pdfRefr);
                yield break;
            }
            if (distribution.X + distribution.Y > r && _reflection.TryReflect(ray, hit, out var reflected, out var attenReflect, out var pdfRefl)) {
                yield return ScatterResult.Create(reflected, attenReflect, pdfRefl);
                yield break;
            }
            if (_diffuse.TryDiffuse(ray, hit, out var scattered, out var attenDiffuse, out var pdfDif)) {
                yield return ScatterResult.Create(scattered, attenDiffuse, pdfDif);
                yield break;
            }
        }
    }

    public class MultiScatterModel : LegacyScatterModel {
        public MultiScatterModel(IDiffuseModel diffuse, IReflectionModel reflection, IRefractionModel refraction, Random? rng = null)
            : base(diffuse, reflection, refraction, rng) { }
        public override IEnumerable<ScatterResult> Scatter(Ray ray, HitRecord hit, Vector3 p) {
            float r = _rng.NextSingle();
            if (_refraction.TryRefract(ray, hit, out var refracted, out var attenRefract, out var pdfRefr)) {
                yield return ScatterResult.Create(refracted, attenRefract, pdfRefr);
            }
            if (_reflection.TryReflect(ray, hit, out var reflected, out var attenReflect, out var pdfRefl)) {
                yield return ScatterResult.Create(reflected, attenReflect, pdfRefr);
            }
            if (_diffuse.TryDiffuse(ray, hit, out var scattered, out var attenDiffuse, out var pdfFif)) {
                yield return ScatterResult.Create(scattered, attenDiffuse, pdfFif);
            }
        }
    }
}
