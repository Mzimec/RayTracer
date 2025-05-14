using OpenTK.Mathematics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace rt004.shared {
    public abstract class LightSource {
        public Vector3 Intensity { get; set; }

        public LightSource(Vector3 intensity) {
            this.Intensity = intensity;
        }

        public abstract Vector3 GetDirection(Vector3 point);

        public virtual Vector3 GetIntensity(Vector3 point) {
            return Intensity; // Default intensity is constant
        }
    }

    public class PointLight : LightSource, ITransform {
        public Vector3 Position { get; set; }
        public Quaternion Rotation { get; set; }
        public Quaternion Scale { get; set; }

        public PointLight(Vector3 intensity, Vector3 position)
            : base(intensity) {

            this.Position = position;
        }

        public override Vector3 GetDirection(Vector3 point) {
            // Direction from point to light source
            return Vector3.Normalize(Position - point);
        }

        // Calculate light intensity at a point based on distance (Inverse Square Law)
        public override Vector3 GetIntensity(Vector3 point) {
            float distance = Vector3.Distance(Position, point);

            // Apply inverse square law
            float attenuation = 1.0f / (distance * distance); // Or use (constant + linear + quadratic attenuation)

            return Intensity * attenuation; // Scaling the intensity based on the distance
        }

        // Optionally, use more complex attenuation if needed (for example: quadratic attenuation)
        public Vector3 GetIntensityWithAttenuation(Vector3 point, float constantAttenuation = 1.0f, float linearAttenuation = 0.0f, float quadraticAttenuation = 1.0f) {
            float distance = Vector3.Distance(Position, point);

            // Inverse square law with more customizable attenuation factors
            float attenuation = 1.0f / (constantAttenuation + linearAttenuation * distance + quadraticAttenuation * distance * distance);
            return Intensity * attenuation;
        }
    }

    public class DirectionalLight : LightSource {
        public Vector3 Direction { get; set; }

        public DirectionalLight(Vector3 direction, Vector3 intensity)
            : base(intensity) {

            this.Direction = direction;
        }

        public override Vector3 GetDirection(Vector3 point) {
            // Direction is constant for a directional light
            return Vector3.Normalize(Direction);
        }

        public override Vector3 GetIntensity(Vector3 point) {
            // Intensity is constant regardless of distance
            return Intensity;
        }
    }

    public class AmbientLight : LightSource {
        public AmbientLight(Vector3 intensity)
            : base(intensity) { }

        public override Vector3 GetDirection(Vector3 point) {
            return Vector3.Zero; // Ambient light does not have a direction
        }

        public override Vector3 GetIntensity(Vector3 point) {
            return Intensity; // Ambient light intensity is constant
        }
    }

}
