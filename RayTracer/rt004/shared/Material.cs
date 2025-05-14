using System;
using System.Collections.Generic;
using System.Linq;
using OpenTK.Mathematics;
using System.Text;
using System.Threading.Tasks;

namespace rt004.shared {
    public struct Material {
        public bool IsReflective { get; } // Reflection flag
        public bool IsTransparent { get; }// Transparency flag
        public Vector3 Ambient { get; } // Ambient color
        public Vector3 Diffuse { get; } // Diffuse color
        public Vector3 Specular { get; } // Specular color
        public float Shininess { get; } // 
        public float Reflectivity { get; } // Reflectivity factor
        public float Transparency { get; } // Transparency factor
        public float RefractiveIndex { get; } // Refractive index for transparency

        public Material(Vector3 ambient, Vector3 diffuse, Vector3 specular, 
            float shininess, float reflectivity = 0.0f, float transparency = 0.0f, float refractiveIndex = 1.0f,
            bool isReflective = false, bool isTransparent = false) {
            Ambient = ambient;
            Diffuse = diffuse;
            Specular = specular;
            Shininess = shininess;
            IsReflective = isReflective;
            IsTransparent = isTransparent;
            Reflectivity = reflectivity;
            Transparency = transparency;
            RefractiveIndex = refractiveIndex;
        }
    }
}
