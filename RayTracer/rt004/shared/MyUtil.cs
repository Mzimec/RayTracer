using Util;

namespace rt004.shared {
    /// <summary>
    /// Utility methods for saving images in different HDR formats.
    /// </summary>
    internal class MyUtil {
        /// <summary>
        /// Saves a <see cref="FloatImage"/> to disk in either HDR or PFM format, based on the file extension.
        /// </summary>
        /// <param name="image">The floating-point image to save.</param>
        /// <param name="filename">The output file name. Use ".hdr" for HDR format, otherwise PFM is used.</param>
        public static void SaveAsFloatImage(FloatImage image, string filename) {
            // Save the HDR image.
            if (filename.EndsWith(".hdr"))
                image.SaveHDR(filename);     // HDR format is still buggy
            else
                image.SavePFM(filename);     // PFM format works well
        }
    }
}