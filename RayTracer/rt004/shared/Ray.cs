using OpenTK.Mathematics;

namespace rt004.shared {
    /// <summary>
    /// Represents a ray in 3D space, defined by an origin and a direction.
    /// </summary>
    public struct Ray {
        /// <summary>
        /// Gets the origin point of the ray.
        /// </summary>
        public Vector3 Origin { get; }

        /// <summary>
        /// Gets the direction vector of the ray (should be normalized).
        /// </summary>
        public Vector3 Direction { get; }

        /// <summary>
        /// Initializes a new instance of the <see cref="Ray"/> struct.
        /// </summary>
        /// <param name="origin">The origin point of the ray.</param>
        /// <param name="direction">The direction vector of the ray (should be normalized).</param>
        public Ray(Vector3 origin, Vector3 direction) {
            Origin = origin;
            Direction = direction;
        }

        /// <summary>
        /// Gets a point along the ray at distance <paramref name="t"/> from the origin.
        /// </summary>
        /// <param name="t">The distance from the origin.</param>
        /// <returns>The point at <paramref name="t"/> along the ray.</returns>
        public Vector3 GetPoint(float t) {
            return Origin + t * Direction;
        }

        /// <summary>
        /// Transforms the ray by the given transformation matrix.
        /// </summary>
        /// <param name="transformation">The transformation matrix to apply.</param>
        /// <returns>A new <see cref="Ray"/> with transformed origin and direction.</returns>
        public Ray Transform(Matrix4 transformation) {
            Vector3 newOrigin = Vector3.TransformPosition(Origin, transformation);
            Vector3 newDirection = Vector3.TransformNormal(Direction, transformation);
            return new Ray(newOrigin, newDirection.Normalized());
        }
    }

    /// <summary>
    /// Represents information about a ray-object intersection.
    /// </summary>
    public struct HitRecord {
        /// <summary>
        /// Gets or sets the distance along the ray where the hit occurred.
        /// </summary>
        public float T { get; set; }

        /// <summary>
        /// Gets or sets the point of intersection in world space.
        /// </summary>
        public Vector3 Point { get; set; }

        /// <summary>
        /// Gets or sets the surface normal at the intersection point.
        /// </summary>
        public Vector3 Normal { get; set; }

        /// <summary>
        /// Gets or sets the material of the intersected object.
        /// </summary>
        public Material Material { get; set; }

        /// <summary>
        /// Gets or sets the texture coordinates at the intersection point.
        /// </summary>
        public (float u, float v) TextureCoordinates { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether the hit is on the front face of the surface.
        /// </summary>
        public bool IsFrontFace { get; set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="HitRecord"/> struct.
        /// </summary>
        /// <param name="t">The distance along the ray where the hit occurred.</param>
        /// <param name="point">The point of intersection.</param>
        /// <param name="normal">The surface normal at the intersection point.</param>
        /// <param name="material">The material of the intersected object.</param>
        public HitRecord(float t, Vector3 point, Vector3 normal, Material material) {
            T = t;
            Point = point;
            Normal = normal;
            Material = material;
            TextureCoordinates = default;
            IsFrontFace = default;
        }
    }
}
