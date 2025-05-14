using System;
using System.Collections.Generic;
using System.Linq;
using OpenTK.Mathematics;
using System.Text;
using System.Threading.Tasks;

namespace rt004.shared {
    public abstract class Camera : ITransform {
        protected Vector3 forward;
        protected Vector3 right;
        protected Vector3 up;

        public float FieldOfView { get; }
        public int ImageWidth { get; }
        public int ImageHeight { get; }

        public Vector3 Position { get; set; }
        public Quaternion Rotation { get; set; }
        public Quaternion Scale { get; set; }

        protected float aspectRatio;
        protected float scale;

        public Camera(Vector3 position, Vector3 lookAt, Vector3 upVector, float fov, int width, int height) {
            Position = position;
            forward = Vector3.Normalize(lookAt - position);
            right = Vector3.Normalize(Vector3.Cross(forward, upVector));
            up = Vector3.Cross(right, forward);

            FieldOfView = fov;
            ImageWidth = width;
            ImageHeight = height;

            aspectRatio = (float)width / height;
            scale = (float)Math.Tan(fov * 0.5 * Math.PI / 180);
        }

        public abstract (Vector3 origin, Vector3 direction) GenerateRay(float x, float y);
    }

    public class PerspectiveCamera : Camera {

        public PerspectiveCamera(Vector3 position, Vector3 lookAt, Vector3 up, float fov, int width, int height)
        : base(position, lookAt, up, fov, width, height) { }

        public override (Vector3 origin, Vector3 direction) GenerateRay(float x, float y) {
            // Convert pixel coordinates to normalized device coordinates [-1, 1]
            float px = (2.0f * x / ImageWidth - 1.0f) * aspectRatio * scale;
            float py = (1.0f - 2.0f * y / ImageHeight) * scale;

            // Compute ray direction
            Vector3 direction = Vector3.Normalize(forward + px * right + py * up);

            return (Position, direction);
        }
    }
}
