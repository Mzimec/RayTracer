
namespace rt004.shared {
    /// <summary>
    /// Generates 3D Perlin noise for procedural texturing and effects.
    /// </summary>
    public class PerlinNoise {
        private readonly int[] _permutation;

        /// <summary>
        /// Initializes a new instance of the <see cref="PerlinNoise"/> class with an optional seed.
        /// </summary>
        /// <param name="seed">Seed for random permutation. Use the same seed for repeatable noise.</param>
        public PerlinNoise(int seed = 0) {
            var rand = new Random(seed);
            _permutation = new int[512];
            var p = new int[256];
            for (int i = 0; i < 256; i++) p[i] = i;
            for (int i = 0; i < 256; i++) {
                int j = rand.Next(256);
                (p[i], p[j]) = (p[j], p[i]);
            }
            for (int i = 0; i < 512; i++) _permutation[i] = p[i % 256];
        }

        /// <summary>
        /// Fade function as defined by Ken Perlin. This eases coordinate values
        /// so that they will "ease" towards integral values. This smooths the final output.
        /// </summary>
        /// <param name="t">The input value.</param>
        /// <returns>The faded value.</returns>
        private static float Fade(float t) => t * t * t * (t * (t * 6 - 15) + 10);

        /// <summary>
        /// Linear interpolation between two values.
        /// </summary>
        /// <param name="a">Start value.</param>
        /// <param name="b">End value.</param>
        /// <param name="t">Interpolation factor.</param>
        /// <returns>Interpolated value.</returns>
        private static float Lerp(float a, float b, float t) => a + t * (b - a);

        /// <summary>
        /// Gradient function calculates dot product between a pseudorandom
        /// gradient vector and the vector from the input coordinate to the
        /// 8 surrounding points in its unit cube.
        /// </summary>
        /// <param name="hash">Hash value for gradient selection.</param>
        /// <param name="x">X offset.</param>
        /// <param name="y">Y offset.</param>
        /// <param name="z">Z offset.</param>
        /// <returns>Dot product result.</returns>
        private static float Grad(int hash, float x, float y, float z) {
            int h = hash & 15;
            float u = h < 8 ? x : y;
            float v = h < 4 ? y : h == 12 || h == 14 ? x : z;
            return ((h & 1) == 0 ? u : -u) + ((h & 2) == 0 ? v : -v);
        }

        /// <summary>
        /// Computes the Perlin noise value at the given 3D coordinates.
        /// </summary>
        /// <param name="x">X coordinate.</param>
        /// <param name="y">Y coordinate.</param>
        /// <param name="z">Z coordinate.</param>
        /// <returns>Noise value in the range [-1, 1].</returns>
        public float Noise(float x, float y, float z) {
            int X = (int)MathF.Floor(x) & 255;
            int Y = (int)MathF.Floor(y) & 255;
            int Z = (int)MathF.Floor(z) & 255;

            x -= MathF.Floor(x);
            y -= MathF.Floor(y);
            z -= MathF.Floor(z);

            float u = Fade(x);
            float v = Fade(y);
            float w = Fade(z);

            int A = _permutation[X] + Y;
            int AA = _permutation[A] + Z;
            int AB = _permutation[A + 1] + Z;
            int B = _permutation[X + 1] + Y;
            int BA = _permutation[B] + Z;
            int BB = _permutation[B + 1] + Z;

            return Lerp(
                Lerp(
                    Lerp(Grad(_permutation[AA], x, y, z),
                         Grad(_permutation[BA], x - 1, y, z), u),
                    Lerp(Grad(_permutation[AB], x, y - 1, z),
                         Grad(_permutation[BB], x - 1, y - 1, z), u), v),
                Lerp(
                    Lerp(Grad(_permutation[AA + 1], x, y, z - 1),
                         Grad(_permutation[BA + 1], x - 1, y, z - 1), u),
                    Lerp(Grad(_permutation[AB + 1], x, y - 1, z - 1),
                         Grad(_permutation[BB + 1], x - 1, y - 1, z - 1), u), v), w);
        }
    }
}