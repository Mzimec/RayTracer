using OpenTK.Mathematics;

namespace rt004.shared {
    /// <summary>
    /// Represents a material with physical properties for rendering.
    /// </summary>
    public class Material {
        /// <summary>
        /// Gets or sets the composite scatter model for this material.
        /// </summary>
        public CompositeScatterModel ScatterModel { get; set; }

        /// <summary>
        /// Gets the material name, useful for identification.
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// Gets the ambient color of the material.
        /// </summary>
        public Vector3 Ambient { get; }

        /// <summary>
        /// Gets the diffuse color of the material.
        /// </summary>
        public Vector3 Diffuse { get; }

        /// <summary>
        /// Gets the specular color of the material.
        /// </summary>
        public Vector3 Specular { get; }

        /// <summary>
        /// Gets the transmittance color for transparent materials, if applicable.
        /// </summary>
        public Vector3 Transmittance { get; }

        /// <summary>
        /// Gets the shininess factor of the material.
        /// </summary>
        public float Shininess { get; }

        /// <summary>
        /// Gets the reflectivity factor of the material.
        /// </summary>
        public float Reflectivity { get; }

        /// <summary>
        /// Gets the transparency factor of the material.
        /// </summary>
        public float Transparency { get; }

        /// <summary>
        /// Gets the refractive index for transparency.
        /// </summary>
        public float RefractiveIndex { get; }

        /// <summary>
        /// Gets or sets the fuzziness factor for rough surfaces.
        /// </summary>
        public float Fuzziness { get; set; }

        /// <summary>
        /// Gets or sets the optional diffuse texture for the material.
        /// </summary>
        public ITexture<Vector3>? DiffuseTexture { get; set; }

        /// <summary>
        /// Gets or sets the optional normal texture for the material, used for bump mapping.
        /// </summary>
        public ITexture<Vector3>? NormalTexture { get; set; }

        /// <summary>
        /// Gets or sets the optional emissive texture for the material, used for self-illumination.
        /// </summary>
        public ITexture<Vector3>? NoiseTexture { get; set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="Material"/> class.
        /// </summary>
        /// <param name="name">The material name.</param>
        /// <param name="models">Scatter models and their weights.</param>
        /// <param name="eModels">Emissive models.</param>
        /// <param name="ambient">Ambient color.</param>
        /// <param name="diffuse">Diffuse color.</param>
        /// <param name="specular">Specular color.</param>
        /// <param name="transmittance">Transmittance color.</param>
        /// <param name="shininess">Shininess factor.</param>
        /// <param name="reflectivity">Reflectivity factor.</param>
        /// <param name="transparency">Transparency factor.</param>
        /// <param name="refractiveIndex">Refractive index.</param>
        /// <param name="fuzziness">Fuzziness factor.</param>
        /// <param name="dt">Optional diffuse texture.</param>
        /// <param name="nt">Optional normal texture.</param>
        /// <param name="not">Optional noise texture.</param>
        /// <exception cref="ArgumentException">Thrown if the material name is null or empty.</exception>
        public Material(string name, IReadOnlyList<(IScatterModel, float)>? models = null, IReadOnlyList<IEmissiveModel>? eModels = null,
            Vector3? ambient = null, Vector3? diffuse = null, Vector3? specular = null, Vector3? transmittance = null,
            float shininess = 0f, float reflectivity = 0f, float transparency = 0f, float refractiveIndex = 1f, float fuzziness = 0f,
            ITexture<Vector3>? dt = null, ITexture<Vector3>? nt = null, ITexture<Vector3>? not = null) {

            if (string.IsNullOrWhiteSpace(name)) {
                throw new ArgumentException("Material name cannot be null or empty", nameof(name));
            }
            Name = name;

            IReadOnlyList<(IScatterModel, float)> scModels = models ?? new List<(IScatterModel, float)>();
            ScatterModel = new CompositeScatterModel(scModels, eModels ?? new List<IEmissiveModel>());

            Ambient = ambient ?? Vector3.Zero;
            Diffuse = diffuse ?? Vector3.Zero;
            Specular = specular ?? Vector3.Zero;
            Transmittance = transmittance ?? Vector3.Zero;
            Shininess = shininess;
            Reflectivity = reflectivity;
            Transparency = transparency;
            RefractiveIndex = refractiveIndex;
            Fuzziness = fuzziness;
            DiffuseTexture = dt;
            NormalTexture = nt;
            NoiseTexture = not;
        }

        /// <summary>
        /// Gets the diffuse color at the hit point, optionally modulated by textures.
        /// </summary>
        /// <param name="hit">The hit record containing intersection details.</param>
        /// <returns>The diffuse color as a <see cref="Vector3"/>.</returns>
        public Vector3 GetDiffuse(HitRecord hit) {
            Vector3 diffuse = DiffuseTexture == null ? Diffuse : DiffuseTexture.Sample(hit); // RGB in range [0,1]
            if (NoiseTexture != null) {
                Vector3 noise = NoiseTexture.Sample(hit);
                diffuse *= noise; // Modulate diffuse color with noise texture
            }
            return diffuse;
        }

        /// <summary>
        /// Gets the normal vector at the hit point, optionally using a normal map.
        /// </summary>
        /// <param name="hit">The hit record containing intersection details.</param>
        /// <returns>The normal vector in world space.</returns>
        public Vector3 GetNormal(HitRecord hit) {
            if (NormalTexture == null)
                return hit.Normal;

            // 1. Sample the normal map (in tangent space)
            Vector3 sampled = NormalTexture.Sample(hit); // RGB in [0,1]

            // 2. Convert to [-1, 1] space
            Vector3 normalTS = 2.0f * sampled - Vector3.One;

            // 3. Get the TBN matrix
            Vector3Extensions.CreateONB(hit.Normal, out Vector3 tangent, out Vector3 bitangent);
            Matrix3 tbn = new Matrix3(
                tangent.X, bitangent.X, hit.Normal.X,
                tangent.Y, bitangent.Y, hit.Normal.Y,
                tangent.Z, bitangent.Z, hit.Normal.Z
            );

            // 4. Transform to world space
            Vector3 normalWS = Vector3.Normalize(tbn * normalTS);
            return normalWS;
        }

        /// <summary>
        /// Determines whether two materials are equal by name.
        /// </summary>
        /// <param name="left">The first material.</param>
        /// <param name="right">The second material.</param>
        /// <returns>True if the names are equal; otherwise, false.</returns>
        public static bool operator ==(Material left, Material right) {
            return left.Name == right.Name;
        }

        /// <summary>
        /// Determines whether two materials are not equal by name.
        /// </summary>
        /// <param name="left">The first material.</param>
        /// <param name="right">The second material.</param>
        /// <returns>True if the names are not equal; otherwise, false.</returns>
        public static bool operator !=(Material left, Material right) {
            return !(left == right);
        }

        /// <inheritdoc/>
        public override bool Equals(object? obj) {
            if (obj is Material other) {
                return Name == other.Name;
            }
            return false;
        }

        /// <inheritdoc/>
        public override int GetHashCode() {
            return Name?.GetHashCode() ?? 0;
        }
    }

    /// <summary>
    /// Extension methods for the <see cref="Material"/> class.
    /// </summary>
    public static class MaterialExtension {
        /// <summary>
        /// Gets the transmittance (attenuation) of the material for a given ray and hit.
        /// </summary>
        /// <param name="material">The material instance.</param>
        /// <param name="ray">The ray passing through the material.</param>
        /// <param name="hit">The hit record at the intersection point.</param>
        /// <returns>
        /// The transmittance as a <see cref="Vector3"/> (RGB), where 1 means fully transparent and 0 means fully opaque.
        /// </returns>
        public static Vector3 GetTransmitance(this Material material, Ray ray, HitRecord hit) {
            if (hit.IsFrontFace) return Vector3.One; // If the hit is on the front face, return full intensity
            float distance = Vector3.Distance(ray.Origin, hit.Point);
            Vector3 intensity = new(
                MathF.Exp((material.Transmittance.X - 1f) * distance),
                MathF.Exp((material.Transmittance.Y - 1f) * distance),
                MathF.Exp((material.Transmittance.Z - 1f) * distance)
            );
            return intensity;
        }
    }

    /// <summary>
    /// Interface for objects that have a material.
    /// </summary>
    public interface IHasMaterial {
        /// <summary>
        /// Gets the material assigned to the object.
        /// </summary>
        Material? Material { get; }

        /// <summary>
        /// Gets the optional material override for the object.
        /// </summary>
        Material? MaterialOverride { get; }

        /// <summary>
        /// Updates the material of the object.
        /// </summary>
        /// <param name="material">The new material to assign, or null to use the default.</param>
        void UpdateMaterial(Material? material = null);
    }
}