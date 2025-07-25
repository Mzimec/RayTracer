using OpenTK.Mathematics;

namespace rt004.shared {
    /// <summary>
    /// Represents an interface for emissive material models.
    /// Emissive models define how surfaces emit light in the scene.
    /// </summary>
    public interface IEmissiveModel {
        /// <summary>
        /// Computes the emitted radiance for a given ray and hit record.
        /// </summary>
        /// <param name="incomingRay">The incoming ray that hit the surface.</param>
        /// <param name="hit">The hit record containing intersection details.</param>
        /// <returns>
        /// The emitted color as a <see cref="Vector3"/> (RGB).
        /// </returns>
        Vector3 Emit(Ray incomingRay, HitRecord hit);
    }

    /// <summary>
    /// A simple constant emission model that emits a fixed color.
    /// </summary>
    public class ConstantEmissionModel : IEmissiveModel {
        private readonly Material _material;

        /// <summary>
        /// Initializes a new instance of the <see cref="ConstantEmissionModel"/> class.
        /// </summary>
        /// <param name="material">The material associated with this emission model.</param>
        public ConstantEmissionModel(Material material) {
            _material = material;
        }

        /// <summary>
        /// Returns the constant emission color for the surface.
        /// </summary>
        /// <param name="incomingRay">The incoming ray that hit the surface.</param>
        /// <param name="hit">The hit record containing intersection details.</param>
        /// <returns>
        /// The emitted color as a <see cref="Vector3"/> (RGB).
        /// </returns>
        public Vector3 Emit(Ray incomingRay, HitRecord hit) {
            // Example: return a constant emission color (could be a property of _material)
            return _material.Diffuse; // Replace with actual emission property if available
        }
    }
}