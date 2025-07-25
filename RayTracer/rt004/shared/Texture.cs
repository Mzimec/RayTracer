using OpenTK.Mathematics;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;

namespace rt004.shared {
    /// <summary>
    /// Interface for texture coordinate wrap modes.
    /// </summary>
    public interface IWrapMode {
        /// <summary>
        /// Applies the wrap mode to a single texture coordinate.
        /// </summary>
        /// <param name="coord">The input coordinate (can be outside [0,1]).</param>
        /// <returns>The wrapped coordinate in [0,1] or as defined by the mode.</returns>
        float ApplyWrap(float coord);
    }

    /// <summary>
    /// Repeat wrap mode: repeats the texture infinitely.
    /// </summary>
    public class RepeatWM : IWrapMode {
        /// <inheritdoc/>
        public float ApplyWrap(float coord) {
            return coord - MathF.Floor(coord);
        }
    }

    /// <summary>
    /// Clamp to edge wrap mode: clamps coordinates to the [0,1] range.
    /// </summary>
    public class ClampToEdgeWM : IWrapMode {
        /// <inheritdoc/>
        public float ApplyWrap(float coord) {
            return Math.Clamp(coord, 0f, 1f);
        }
    }

    /// <summary>
    /// Clamp wrap mode: clamps coordinates to the [0,1] range (border mode).
    /// </summary>
    public class ClampWM : IWrapMode {
        /// <inheritdoc/>
        public float ApplyWrap(float coord) {
            return Math.Clamp(coord, 0f, 1f);
        }
    }

    /// <summary>
    /// Mirror repeat wrap mode: mirrors the texture at every integer boundary.
    /// </summary>
    public class MirrorRepeatWM : IWrapMode {
        /// <inheritdoc/>
        public float ApplyWrap(float coord) {
            float mirrored = MathF.Abs(coord % 2);
            return mirrored > 1 ? 2 - mirrored : mirrored;
        }
    }

    /// <summary>
    /// Border wrap mode: clamps coordinates to [0,1] for border handling.
    /// </summary>
    public class BorderWM : IWrapMode {
        /// <inheritdoc/>
        public float ApplyWrap(float coord) {
            return coord < 0 ? 0 : coord > 1 ? 1 : coord;
        }
    }

    /// <summary>
    /// Interface for 2D textures that can be sampled by UV or hit record.
    /// </summary>
    /// <typeparam name="T">The type of value returned by the texture (e.g., Vector3 for color).</typeparam>
    public interface ITexture<T> {
        /// <summary>
        /// Samples the texture at the given UV coordinates.
        /// </summary>
        /// <param name="u">U coordinate (horizontal, typically in [0,1]).</param>
        /// <param name="v">V coordinate (vertical, typically in [0,1]).</param>
        /// <returns>The sampled value.</returns>
        T Sample(float u, float v);

        /// <summary>
        /// Samples the texture using a hit record (typically uses hit.TextureCoordinates).
        /// </summary>
        /// <param name="hit">The hit record containing intersection details.</param>
        /// <returns>The sampled value.</returns>
        T Sample(HitRecord hit);
    }

    /// <summary>
    /// Abstract base class for 2D textures.
    /// </summary>
    /// <typeparam name="T">The type of value stored in the texture.</typeparam>
    public abstract class Texture<T> : ITexture<T> {
        /// <summary>
        /// The pixel data of the texture.
        /// </summary>
        protected T[,] _pixels;

        /// <summary>
        /// Gets or sets the width of the texture in pixels.
        /// </summary>
        protected int Width { get; set; }

        /// <summary>
        /// Gets or sets the height of the texture in pixels.
        /// </summary>
        protected int Height { get; set; }

        /// <summary>
        /// Gets or sets the wrap mode for the U coordinate.
        /// </summary>
        public IWrapMode WrapU { get; set; }

        /// <summary>
        /// Gets or sets the wrap mode for the V coordinate.
        /// </summary>
        public IWrapMode WrapV { get; set; }

        /// <summary>
        /// Samples the texture at the given UV coordinates.
        /// </summary>
        /// <param name="u">U coordinate.</param>
        /// <param name="v">V coordinate.</param>
        /// <returns>The sampled value.</returns>
        public abstract T Sample(float u, float v);

        /// <summary>
        /// Samples the texture using a hit record (default: uses hit.TextureCoordinates).
        /// </summary>
        /// <param name="hit">The hit record containing intersection details.</param>
        /// <returns>The sampled value.</returns>
        public virtual T Sample(HitRecord hit) => Sample(hit.TextureCoordinates.u, hit.TextureCoordinates.v);

        /// <summary>
        /// Initializes a new instance of the <see cref="Texture{T}"/> class.
        /// </summary>
        /// <param name="wu">Optional wrap mode for U coordinate (default: repeat).</param>
        /// <param name="wv">Optional wrap mode for V coordinate (default: repeat).</param>
        public Texture(IWrapMode? wu = null, IWrapMode? wv = null) {
            WrapU = wu ?? new RepeatWM();
            WrapV = wv ?? new RepeatWM();
            _pixels = null!;
        }
    }

    /// <summary>
    /// Abstract base class for 2D textures with Vector3 color data, loaded from an image file.
    /// </summary>
    public abstract class Vec3Texture : Texture<Vector3> {
        /// <summary>
        /// Loads a texture from an image file.
        /// </summary>
        /// <param name="filePath">Path to the image file.</param>
        /// <param name="wu">Optional wrap mode for U coordinate.</param>
        /// <param name="wv">Optional wrap mode for V coordinate.</param>
        public Vec3Texture(string filePath, IWrapMode? wu = null, IWrapMode? wv = null) : base(wu, wv) {
            using var image = Image.Load<Rgba32>(filePath);
            Width = image.Width;
            Height = image.Height;
            _pixels = new Vector3[Width, Height];
            for (int y = 0; y < Height; y++) {
                for (int x = 0; x < Width; x++) {
                    var pixel = image[x, y];
                    _pixels[x, y] = new Vector3(pixel.R / 255f, pixel.G / 255f, pixel.B / 255f);
                }
            }
        }
    }

    /// <summary>
    /// Texture loaded from a bitmap image file, providing color sampling.
    /// </summary>
    public class BitmapTexture : Vec3Texture {
        /// <summary>
        /// Loads a bitmap texture from an image file.
        /// </summary>
        /// <param name="filePath">Path to the image file.</param>
        /// <param name="wu">Optional wrap mode for U coordinate.</param>
        /// <param name="wv">Optional wrap mode for V coordinate.</param>
        public BitmapTexture(string filePath, IWrapMode? wu = null, IWrapMode? wv = null) : base(filePath, wu, wv) { }

        /// <inheritdoc/>
        public override Vector3 Sample(float u, float v) {
            float wrappedU = WrapU.ApplyWrap(u);
            float wrappedV = WrapV.ApplyWrap(v);

            // Convert UV to pixel coordinates ([0,0] is top-left)
            int x = (int)(wrappedU * (Width - 1));
            int y = (int)((1.0f - wrappedV) * (Height - 1)); // v = 0 is bottom

            // Clamp to valid range
            x = Math.Clamp(x, 0, Width - 1);
            y = Math.Clamp(y, 0, Height - 1);

            return _pixels[x, y];
        }
    }

    /// <summary>
    /// Procedural noise texture using Perlin noise and a custom generator function.
    /// </summary>
    public class NoiseTexture : Texture<Vector3> {
        private readonly PerlinNoise _noise;
        private readonly float _scale;
        private readonly Func<Vector3, float, Vector3> _generator;

        /// <summary>
        /// Initializes a new instance of the <see cref="NoiseTexture"/> class.
        /// </summary>
        /// <param name="generator">Function to generate color from position and noise value.</param>
        /// <param name="scale">Scale factor for the noise pattern.</param>
        /// <param name="seed">Seed for the Perlin noise generator.</param>
        public NoiseTexture(Func<Vector3, float, Vector3> generator, float scale = 1f, int seed = 0) {
            _noise = new PerlinNoise(seed);
            _scale = scale;
            _generator = generator;
        }

        /// <inheritdoc/>
        public override Vector3 Sample(float u, float v) {
            float x = WrapU.ApplyWrap(u) * _scale;
            float y = WrapV.ApplyWrap(v) * _scale;
            float z = 0f;
            return _generator(new Vector3(x, y, z), _noise.Noise(x, y, z));
        }

        /// <summary>
        /// Samples the noise texture using a world-space position.
        /// </summary>
        /// <param name="positionWS">The position in world space.</param>
        /// <returns>The generated color value.</returns>
        public Vector3 Sample(Vector3 positionWS) {
            Vector3 p = positionWS * _scale;
            return _generator(p, _noise.Noise(p.X, p.Y, p.Z));
        }

        /// <inheritdoc/>
        public override Vector3 Sample(HitRecord hit) {
            return Sample(hit.Point);
        }
    }
}