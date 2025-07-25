using OpenTK.Mathematics;

namespace rt004.shared {
    /// <summary>
    /// Provides mathematical constants used throughout the ray tracer.
    /// </summary>
    public static class Constants {
        /// <summary>
        /// Small value for floating-point comparisons.
        /// </summary>
        public const float Epsilon = 1e-4f;
    }

    /// <summary>
    /// Extension methods and utilities for <see cref="Vector3"/>.
    /// </summary>
    public static class Vector3Extensions {
        private static readonly Random _rng = new();

        /// <summary>
        /// Converts a vector of angles from radians to degrees.
        /// </summary>
        /// <param name="radians">The vector in radians.</param>
        /// <returns>The vector in degrees.</returns>
        public static Vector3 RadiansToDegrees(this Vector3 radians) {
            return new Vector3(
                MathHelper.RadiansToDegrees(radians.X),
                MathHelper.RadiansToDegrees(radians.Y),
                MathHelper.RadiansToDegrees(radians.Z)
            );
        }

        /// <summary>
        /// Converts a vector of angles from degrees to radians.
        /// </summary>
        /// <param name="degrees">The vector in degrees.</param>
        /// <returns>The vector in radians.</returns>
        public static Vector3 DegreesToRadians(this Vector3 degrees) {
            return new Vector3(
                MathHelper.DegreesToRadians(degrees.X),
                MathHelper.DegreesToRadians(degrees.Y),
                MathHelper.DegreesToRadians(degrees.Z)
            );
        }

        /// <summary>
        /// Reflects a vector about a given normal.
        /// </summary>
        /// <param name="vector">The incident vector.</param>
        /// <param name="normal">The normal to reflect about.</param>
        /// <returns>The reflected vector.</returns>
        public static Vector3 Reflect(this Vector3 vector, Vector3 normal) {
            return vector - 2 * Vector3.Dot(vector, normal) * normal;
        }

        /// <summary>
        /// Generates a random point inside a unit sphere.
        /// </summary>
        /// <returns>A random <see cref="Vector3"/> inside the unit sphere.</returns>
        public static Vector3 RandomInUnitSphere() {
            while (true) {
                var p = new Vector3(
                    _rng.NextSingle() * 2 - 1,
                    _rng.NextSingle() * 2 - 1,
                    _rng.NextSingle() * 2 - 1
                );
                if (p.LengthSquared < 1) return p;
            }
        }

        /// <summary>
        /// Generates a random unit vector (uniformly distributed on the sphere).
        /// </summary>
        /// <returns>A random unit <see cref="Vector3"/>.</returns>
        public static Vector3 RandomUnitVector() => Vector3.Normalize(RandomInUnitSphere());

        /// <summary>
        /// Returns the sum of the vector's components.
        /// </summary>
        /// <param name="vector">The input vector.</param>
        /// <returns>The sum of X, Y, and Z components.</returns>
        public static float Sum(this Vector3 vector) {
            return vector.X + vector.Y + vector.Z;
        }

        /// <summary>
        /// Normalizes the vector using the L1 norm (sum of absolute values).
        /// </summary>
        /// <param name="vector">The input vector.</param>
        /// <returns>The L1-normalized vector, or <see cref="Vector3.Zero"/> if the sum is zero.</returns>
        public static Vector3 L1Normlize(this Vector3 vector) {
            float sum = vector.Sum();
            if (sum == 0) return Vector3.Zero;
            return vector / sum;
        }

        /// <summary>
        /// Converts a non-negative vector to a probability distribution (L1-normalized).
        /// </summary>
        /// <param name="vector">The input vector (all components must be non-negative).</param>
        /// <returns>The normalized distribution vector.</returns>
        /// <exception cref="ArgumentException">Thrown if any component is negative.</exception>
        public static Vector3 ToDistribution(this Vector3 vector) {
            if (vector.X < 0 || vector.Y < 0 || vector.Z < 0) {
                throw new ArgumentException("Vector components must be non-negative for distribution conversion.");
            }
            return vector.L1Normlize();
        }

        /// <summary>
        /// Generates a random direction vector with cosine-weighted distribution (for diffuse reflection).
        /// </summary>
        /// <param name="random">Optional random number generator. If null, uses <see cref="Random.Shared"/>.</param>
        /// <returns>A random <see cref="Vector3"/> direction.</returns>
        public static Vector3 RandomCosineDirection(Random? random = null) {
            if (random == null) random = Random.Shared;
            float r1 = random.NextSingle();
            float r2 = random.NextSingle();
            float y = MathF.Sqrt(1 - r2);

            float phi = 2 * MathF.PI * r1;
            float x = MathF.Cos(phi) * MathF.Sqrt(r2);
            float z = MathF.Sin(phi) * MathF.Sqrt(r2);
            return new Vector3(x, y, z);
        }

        /// <summary>
        /// Creates an orthonormal basis (ONB) from the given vector.
        /// </summary>
        /// <param name="vector">The input vector (used as the "up" direction).</param>
        /// <param name="tangent">Output tangent vector (orthogonal to input).</param>
        /// <param name="bitangent">Output bitangent vector (orthogonal to both input and tangent).</param>
        public static void CreateONB(this Vector3 vector, out Vector3 tangent, out Vector3 bitangent) {
            Vector3 up = vector;
            Vector3 helper = Math.Abs(up.Y) < 0.999f ? Vector3.UnitY : Vector3.UnitX;
            tangent = Vector3.Normalize(Vector3.Cross(helper, up));
            bitangent = Vector3.Cross(up, tangent);
        }
    }
}