using OpenTK.Mathematics;

namespace rt004.shared {
    /// <summary>
    /// Provides static methods for generating procedural textures such as marble and wood.
    /// </summary>
    public static class ProceduralGenerators {
        /// <summary>
        /// Generates a marble-like color based on position and noise value.
        /// </summary>
        /// <param name="p">The 3D position in space.</param>
        /// <param name="noiseValue">The noise value to modulate the pattern.</param>
        /// <returns>A <see cref="Vector3"/> representing the RGB color for marble texture.</returns>
        public static Vector3 Marble(Vector3 p, float noiseValue) {
            float value = 0.5f + 0.5f * MathF.Sin((p.X + noiseValue * 5f) * 10f);
            return new Vector3(value, value * 0.9f, value * 0.95f);
        }

        /// <summary>
        /// Generates a wood-like color based on position and noise value.
        /// </summary>
        /// <param name="p">The 3D position in space.</param>
        /// <param name="noiseValue">The noise value to modulate the pattern.</param>
        /// <returns>A <see cref="Vector3"/> representing the RGB color for wood texture.</returns>
        public static Vector3 Wood(Vector3 p, float noiseValue) {
            float rings = MathF.Sin((p.X * 10 + noiseValue * 5f));
            float value = 0.5f + 0.5f * rings;
            return new Vector3(value, value * 0.8f, value * 0.6f);
        }
    }
}