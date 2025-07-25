using OpenTK.Mathematics;

namespace rt004.shared {
    /// <summary>
    /// Abstract base class for all light sources in the scene.
    /// </summary>
    public abstract class LightSource : SceneNode {
        /// <summary>
        /// Gets or sets the intensity (color and strength) of the light source.
        /// </summary>
        public Vector3 Intensity { get; set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="LightSource"/> class.
        /// </summary>
        /// <param name="name">The name of the light source.</param>
        /// <param name="intensity">The intensity (color and strength) of the light.</param>
        /// <param name="transform">Optional transform for the light source.</param>
        public LightSource(string name, Vector3 intensity, Transform? transform = null) : base(name, transform) {
            this.Intensity = intensity;
        }

        /// <summary>
        /// Gets the direction of the light at a given point in the scene.
        /// </summary>
        /// <param name="point">The point in the scene.</param>
        /// <returns>The direction vector from the point to the light source.</returns>
        public abstract Vector3 GetDirection(Vector3 point);

        /// <summary>
        /// Gets the intensity of the light at a given point in the scene.
        /// </summary>
        /// <param name="point">The point in the scene.</param>
        /// <returns>The intensity (color and strength) at the specified point.</returns>
        public virtual Vector3 GetIntensity(Vector3 point) {
            return Intensity; // Default intensity is constant
        }
    }

    /// <summary>
    /// Represents a point light source that emits light from a single position in space.
    /// </summary>
    public class PointLight : LightSource {

        /// <summary>
        /// Initializes a new instance of the <see cref="PointLight"/> class.
        /// </summary>
        /// <param name="name">The name of the light source.</param>
        /// <param name="intensity">The intensity (color and strength) of the light.</param>
        /// <param name="transform">Optional transform for the light source.</param>
        public PointLight(string name, Vector3 intensity, Transform? transform = null)
            : base(name, intensity, transform) {
        }

        /// <summary>
        /// Gets the normalized direction from the point in the scene to the light source.
        /// </summary>
        /// <param name="point">The point in the scene.</param>
        /// <returns>The normalized direction vector from the point to the light source.</returns>
        public override Vector3 GetDirection(Vector3 point) {
            return Vector3.Normalize(point - Transform.Position);
        }

        /// <summary>
        /// Gets the intensity of the light at a given point in the scene, using the inverse square law.
        /// </summary>
        /// <param name="point">The point in the scene.</param>
        /// <returns>The attenuated intensity at the specified point.</returns>
        public override Vector3 GetIntensity(Vector3 point) {
            float distance = Vector3.Distance(Transform.Position, point);
            float attenuation = 1.0f / (distance * distance);
            return Intensity * attenuation;
        }

        /// <summary>
        /// Gets the intensity of the light at a given point in the scene, using customizable attenuation factors.
        /// </summary>
        /// <param name="point">The point in the scene.</param>
        /// <param name="constantAttenuation">Constant attenuation factor.</param>
        /// <param name="linearAttenuation">Linear attenuation factor.</param>
        /// <param name="quadraticAttenuation">Quadratic attenuation factor.</param>
        /// <returns>The attenuated intensity at the specified point.</returns>
        public Vector3 GetIntensityWithAttenuation(
            Vector3 point,
            float constantAttenuation = 1.0f,
            float linearAttenuation = 0.0f,
            float quadraticAttenuation = 1.0f) {
            float distance = Vector3.Distance(Transform.Position, point);
            float attenuation = 1.0f / (constantAttenuation + linearAttenuation * distance + quadraticAttenuation * distance * distance);
            return Intensity * attenuation;
        }
    }

    /// <summary>
    /// Represents a directional light source (e.g., sunlight) with constant direction and intensity.
    /// </summary>
    public class DirectionalLight : LightSource {
        /// <summary>
        /// Gets the direction of the light.
        /// </summary>
        public Vector3 Direction { get; }

        /// <summary>
        /// Initializes a new instance of the <see cref="DirectionalLight"/> class.
        /// </summary>
        /// <param name="name">The name of the light source.</param>
        /// <param name="intensity">The intensity (color and strength) of the light.</param>
        /// <param name="direction">The direction of the light.</param>
        public DirectionalLight(string name, Vector3 intensity, Vector3 direction)
            : base(name, intensity) {
            this.Direction = direction;
        }

        /// <summary>
        /// Gets the normalized direction of the light (constant for all points).
        /// </summary>
        /// <param name="point">The point in the scene (ignored).</param>
        /// <returns>The normalized direction vector of the light.</returns>
        public override Vector3 GetDirection(Vector3 point) {
            return Vector3.Normalize(Direction);
        }

        /// <summary>
        /// Gets the intensity of the light at a given point in the scene (constant for all points).
        /// </summary>
        /// <param name="point">The point in the scene (ignored).</param>
        /// <returns>The intensity of the light.</returns>
        public override Vector3 GetIntensity(Vector3 point) {
            return Intensity;
        }
    }

    /// <summary>
    /// Represents an ambient light source that illuminates all objects equally from all directions.
    /// </summary>
    public class AmbientLight : LightSource {
        /// <summary>
        /// Initializes a new instance of the <see cref="AmbientLight"/> class.
        /// </summary>
        /// <param name="name">The name of the light source.</param>
        /// <param name="intensity">The intensity (color and strength) of the light.</param>
        public AmbientLight(string name, Vector3 intensity)
            : base(name, intensity) { }

        /// <summary>
        /// Gets the direction of the light at a given point in the scene (always zero for ambient light).
        /// </summary>
        /// <param name="point">The point in the scene (ignored).</param>
        /// <returns>Zero vector, as ambient light has no direction.</returns>
        public override Vector3 GetDirection(Vector3 point) {
            return Vector3.Zero;
        }

        /// <summary>
        /// Gets the intensity of the light at a given point in the scene (constant for all points).
        /// </summary>
        /// <param name="point">The point in the scene (ignored).</param>
        /// <returns>The intensity of the ambient light.</returns>
        public override Vector3 GetIntensity(Vector3 point) {
            return Intensity;
        }
    }
}