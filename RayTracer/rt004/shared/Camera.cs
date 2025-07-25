using OpenTK.Mathematics;

namespace rt004.shared {
    /// <summary>
    /// Abstract base class for all camera types in the scene.
    /// Handles orientation and basic camera properties.
    /// </summary>
    public abstract class Camera : SceneNode {
        protected Vector3 forward;
        protected Vector3 right;
        protected Vector3 up;
        protected float aspectRatio;
        protected float scale;

        /// <summary>
        /// Gets the camera's field of view in degrees.
        /// </summary>
        public float FieldOfView { get; }
        /// <summary>
        /// Gets the width of the output image in pixels.
        /// </summary>
        public int ImageWidth { get; }
        /// <summary>
        /// Gets the height of the output image in pixels.
        /// </summary>
        public int ImageHeight { get; }

        /// <summary>
        /// Generates a ray from the camera through the specified pixel coordinates.
        /// </summary>
        /// <param name="x">The x-coordinate of the pixel.</param>
        /// <param name="y">The y-coordinate of the pixel.</param>
        /// <returns>A ray originating from the camera through the pixel.</returns>
        public Camera(string name, float fov, int width, int height, Transform? transform = null) : base(name, transform){
            this.Transform.OnRotationChanged += UpdateDirections;
            UpdateDirections(); // Initialize forward, right, and up vectors

            this.FieldOfView = fov;
            this.ImageWidth = width;
            this.ImageHeight = height;

            aspectRatio = (float)width / height;
            scale = (float)Math.Tan(fov * 0.5 * Math.PI / 180);
        }

        /// <summary>
        /// Generates a ray from the camera through the specified pixel coordinates.
        /// </summary>
        /// <param name="x">The x-coordinate of the pixel.</param>
        /// <param name="y">The y-coordinate of the pixel.</param>
        /// <returns>A ray originating from the camera through the pixel.</returns>
        public abstract Ray GenerateRay(float x, float y);

        /// <summary>
        /// Updates the camera's orientation vectors based on its current rotation.
        /// </summary>
        private void UpdateDirections() {
            // Update forward, right, and up vectors based on the camera's transform
            Matrix4 rotation = Transform.RotationMatrix;
            forward = Vector3.Normalize(-rotation.Row2.Xyz); // -Z
            right = Vector3.Normalize(rotation.Row0.Xyz);    // +X
            up = Vector3.Normalize(rotation.Row1.Xyz);       // +Y
        }
    }

    /// <summary>
    /// A perspective camera that generates rays using perspective projection.
    /// </summary>
    public class PerspectiveCamera : Camera {

        /// <summary>
        /// Initializes a new instance of the <see cref="PerspectiveCamera"/> class.
        /// </summary>
        /// <param name="name">The name of the camera node.</param>
        /// <param name="fov">The field of view in degrees.</param>
        /// <param name="width">The width of the output image in pixels.</param>
        /// <param name="height">The height of the output image in pixels.</param>
        /// <param name="transform">Optional transform for the camera.</param>
        public PerspectiveCamera(string name, float fov, int width, int height, Transform? transform)
        : base(name, fov, width, height, transform) { }

        /// <summary>
        /// Generates a ray from the camera through the specified pixel coordinates, with jitter for anti-aliasing.
        /// </summary>
        /// <param name="x">The x-coordinate of the pixel.</param>
        /// <param name="y">The y-coordinate of the pixel.</param>
        /// <returns>A ray originating from the camera through the pixel.</returns>
        public override Ray GenerateRay(float x, float y) {
            float jitterX = Random.Shared.NextSingle();
            float jitterY = Random.Shared.NextSingle();

            float px = (x + jitterX) / ImageWidth;
            float py = (y + jitterY) / ImageHeight;

            float sx = (2.0f * px - 1.0f) * aspectRatio * scale;
            float sy = (1.0f - 2.0f * py) * scale;

            Vector3 direction = Vector3.Normalize(forward + sx * right + sy * up);

            return new Ray(Transform.Position, direction);
        }
    }
}
