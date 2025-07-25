using OpenTK.Mathematics;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp;
using Util;

namespace rt004.shared {
    /// <summary>
    /// Provides static methods for tone mapping high dynamic range (HDR) colors and images to low dynamic range (LDR).
    /// </summary>
    public static class ToneMapper {
        /// <summary>
        /// Applies logarithmic tone mapping to a single HDR color.
        /// </summary>
        /// <param name="hdrColor">The input HDR color as a <see cref="Vector3"/> (RGB).</param>
        /// <param name="a">The tone mapping strength parameter (default: 1.0).</param>
        /// <returns>The tone-mapped LDR color as a <see cref="Vector3"/> in [0,1].</returns>
        public static Vector3 ToneMap(Vector3 hdrColor, float a = 1.0f) {
            // Convert RGB to luminance (approximate with Rec.709)
            float luminance = 0.2126f * hdrColor.X + 0.7152f * hdrColor.Y + 0.0722f * hdrColor.Z;

            if (luminance <= 0f)
                return Vector3.Zero;

            float mappedLuminance = MathF.Log(1f + a * luminance) / MathF.Log(1f + a);
            float scale = mappedLuminance / luminance;

            Vector3 ldrColor = hdrColor * scale;
            return Vector3.Clamp(ldrColor, Vector3.Zero, Vector3.One);
        }

        /// <summary>
        /// Applies tone mapping to an entire HDR image, producing an LDR image.
        /// </summary>
        /// <param name="hdrImage">The input HDR image as a <see cref="FloatImage"/>.</param>
        /// <param name="a">The tone mapping strength parameter (default: 0.1).</param>
        /// <returns>The tone-mapped LDR image as a <see cref="FloatImage"/>.</returns>
        public static FloatImage ApplyToneMapping(FloatImage hdrImage, float a = 0.1f) {
            int width = hdrImage.Width;
            int height = hdrImage.Height;
            int channels = hdrImage.Channels;

            var result = new FloatImage(width, height, channels);
            var hdrPixel = new float[channels];
            var ldrPixel = new float[channels];

            for (int y = 0; y < height; y++) {
                for (int x = 0; x < width; x++) {
                    if (!hdrImage.GetPixel(x, y, hdrPixel))
                        continue;

                    var hdr = new Vector3(hdrPixel[0], hdrPixel[1], hdrPixel[2]);
                    var ldr = ToneMap(hdr, a);

                    ldrPixel[0] = ldr.X;
                    ldrPixel[1] = ldr.Y;
                    ldrPixel[2] = ldr.Z;

                    result.PutPixel(x, y, ldrPixel);
                }
            }

            return result;
        }
    }

    /// <summary>
    /// Provides static methods for saving images to disk.
    /// </summary>
    public static class ImageSaver {
        /// <summary>
        /// Saves a floating-point LDR image as a PNG file.
        /// </summary>
        /// <param name="image">The input image as a <see cref="FloatImage"/>.</param>
        /// <param name="path">The output file path.</param>
        public static void SaveLdrAsPng(FloatImage image, string path) {
            using var img = new Image<Rgba32>(image.Width, image.Height);
            int channels = image.Channels;
            var pixel = new float[channels];

            for (int y = 0; y < image.Height; y++) {
                for (int x = 0; x < image.Width; x++) {
                    if (!image.GetPixel(x, y, pixel))
                        continue;

                    byte r = (byte)(Math.Clamp(pixel[0], 0f, 1f) * 255);
                    byte g = (byte)(Math.Clamp(pixel[1], 0f, 1f) * 255);
                    byte b = (byte)(Math.Clamp(pixel[2], 0f, 1f) * 255);

                    img[x, y] = new Rgba32(r, g, b, 255); // full alpha
                }
            }

            img.SaveAsPng(path);
        }
    }
}