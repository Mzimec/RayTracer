using OpenTK.Mathematics;

namespace rt004.shared {
    /// <summary>
    /// Interface for objects that have a transform and transformation matrices.
    /// </summary>
    public interface ITransformable {
        /// <summary>
        /// Gets the transform of the object.
        /// </summary>
        Transform Transform { get; }
        /// <summary>
        /// Gets the local-to-world transformation matrix.
        /// </summary>
        Matrix4 LocalToWorld { get; }
        /// <summary>
        /// Gets the world-to-local transformation matrix.
        /// </summary>
        Matrix4 WorldToLocal { get; }
    }

    /// <summary>
    /// Represents a 3D transformation including position, rotation, and scale.
    /// Provides transformation matrices and notifies on changes.
    /// </summary>
    public class Transform {
        private Vector3 _position;
        private Quaternion _rotation;
        private Vector3 _scale;

        /// <summary>
        /// Gets or sets the position of the transform.
        /// </summary>
        public Vector3 Position {
            get => _position;
            set {
                _position = value;
                UpdateMatrices();
                OnTransformChnaged?.Invoke();
            }
        }

        /// <summary>
        /// Gets or sets the rotation of the transform as a quaternion.
        /// </summary>
        public Quaternion Rotation {
            get => _rotation;
            set {
                _rotation = value;
                UpdateMatrices();
                OnRotationChanged?.Invoke();
                OnTransformChnaged?.Invoke();
            }
        }

        /// <summary>
        /// Gets or sets the rotation of the transform as Euler angles (degrees).
        /// </summary>
        public Vector3 EuelerRotation {
            get => _rotation.ToEulerAngles().RadiansToDegrees();
            set {
                _rotation = Quaternion.FromEulerAngles(value.DegreesToRadians());
                UpdateMatrices();
                OnRotationChanged?.Invoke();
                OnTransformChnaged?.Invoke();
            }
        }

        /// <summary>
        /// Gets or sets the scale of the transform.
        /// </summary>
        public Vector3 Scale {
            get => _scale;
            set {
                _scale = value;
                UpdateMatrices();
                OnTransformChnaged?.Invoke();
            }
        }

        /// <summary>
        /// Gets the rotation matrix corresponding to the current rotation.
        /// </summary>
        public Matrix4 RotationMatrix => Matrix4.CreateFromQuaternion(_rotation);

        /// <summary>
        /// Gets the parent-to-local transformation matrix.
        /// </summary>
        public Matrix4 ParentToLocal { get; private set; }

        /// <summary>
        /// Gets the local-to-parent transformation matrix.
        /// </summary>
        public Matrix4 LocalToParent { get; private set; }

        /// <summary>
        /// Action invoked when the rotation changes.
        /// </summary>
        public Action OnRotationChanged { get; set; } = null!;

        /// <summary>
        /// Action invoked when any part of the transform changes.
        /// </summary>
        public Action OnTransformChnaged { get; set; } = null!;

        /// <summary>
        /// Initializes a new instance of the <see cref="Transform"/> class using position, Euler rotation (degrees), and scale.
        /// </summary>
        /// <param name="position">The position vector.</param>
        /// <param name="rotation">The Euler rotation in degrees.</param>
        /// <param name="scale">The scale vector.</param>
        public Transform(Vector3 position, Vector3 rotation, Vector3 scale) {
            this._position = position;
            this._rotation = Quaternion.FromEulerAngles(rotation.DegreesToRadians());
            this._scale = scale;
            this.LocalToParent = Matrix4.Identity;
            this.ParentToLocal = Matrix4.Identity;
            UpdateMatrices();
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="Transform"/> class using position, quaternion rotation, and scale.
        /// </summary>
        /// <param name="position">The position vector.</param>
        /// <param name="rotation">The rotation as a quaternion.</param>
        /// <param name="scale">The scale vector.</param>
        public Transform(Vector3 position, Quaternion rotation, Vector3 scale) {
            this._position = position;
            this._rotation = rotation;
            this._scale = scale;
            this.LocalToParent = Matrix4.Identity;
            this.ParentToLocal = Matrix4.Identity;
            UpdateMatrices();
        }

        /// <summary>
        /// Updates the transformation matrices based on the current position, rotation, and scale.
        /// </summary>
        private void UpdateMatrices() {
            LocalToParent = Matrix4.CreateScale(Scale) *
                            RotationMatrix *
                            Matrix4.CreateTranslation(Position);
            ParentToLocal = Matrix4.Invert(LocalToParent);
        }
    }
}