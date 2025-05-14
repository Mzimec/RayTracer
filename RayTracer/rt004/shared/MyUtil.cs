using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Util;

namespace rt004.shared {
    internal class MyUtil {
        public static void SaveAsFloatImage(FloatImage image, string filename) {
            // Save the HDR image.
            if (filename.EndsWith(".hdr"))
                image.SaveHDR(filename);     // HDR format is still buggy
            else
                image.SavePFM(filename);     // PFM format works well
        }
    }
}
