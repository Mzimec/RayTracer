using OpenTK.Mathematics;

namespace rt004.shared {

    /// <summary>
    /// Interface for objects that can be intersected by a ray.
    /// </summary>
    public interface IIntersectable {
        /// <summary>
        /// Checks for intersection between the object and a ray.
        /// </summary>
        /// <param name="ray">The ray to test.</param>
        /// <param name="hit">The hit record to populate if an intersection occurs.</param>
        /// <returns>True if the ray intersects the object; otherwise, false.</returns>
        bool Intersect(Ray ray, ref HitRecord hit);
    }

    /// <summary>
    /// Abstract base class for geometric shapes that can be intersected by rays.
    /// </summary>
    public abstract class Shape : IIntersectable {
        /// <inheritdoc/>
        public abstract bool Intersect(Ray ray, ref HitRecord hit);

        /// <summary>
        /// Gets the surface normal at the specified point on the shape.
        /// </summary>
        /// <param name="hitPoint">The point on the shape.</param>
        /// <returns>The normal vector at the point.</returns>
        public abstract Vector3 GetNormal(Vector3 hitPoint);

        /// <summary>
        /// Gets the UV texture coordinates at the specified point on the shape.
        /// </summary>
        /// <param name="hitPoint">The point on the shape.</param>
        /// <returns>The (u, v) texture coordinates.</returns>
        public abstract (float u, float v) GetUV(Vector3 hitPoint);

        /// <summary>
        /// Populates the hit record with intersection details.
        /// </summary>
        /// <param name="ray">The ray that hit the shape.</param>
        /// <param name="t">The distance along the ray to the intersection.</param>
        /// <param name="hit">The hit record to populate.</param>
        protected void SetHit(Ray ray, float t, ref HitRecord hit) {
            hit.T = t;
            hit.Point = ray.GetPoint(t);
            hit.Normal = GetNormal(hit.Point);
            hit.TextureCoordinates = GetUV(hit.Point);
        }
    }

    /// <summary>
    /// Represents a scene object with a geometric shape and material, placed in the scene graph.
    /// </summary>
    public class SceneObject : SceneNode, IIntersectable, IHasMaterial {
        /// <summary>
        /// Gets the geometric shape of the object.
        /// </summary>
        public Shape Shape { get; }

        private Material? _materialOverride;

        /// <summary>
        /// Gets the material assigned to this object, or inherited from the parent.
        /// </summary>
        public Material? Material { get; protected set; }

        /// <summary>
        /// Gets or sets the material override for this object.
        /// </summary>
        public Material? MaterialOverride {
            get => _materialOverride;
            set {
                _materialOverride = value;
                OnMaterialOverrideUpdated(value);
            }
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="SceneObject"/> class.
        /// </summary>
        /// <param name="name">The name of the object.</param>
        /// <param name="shape">The geometric shape of the object.</param>
        /// <param name="transform">The transform of the object.</param>
        /// <param name="materialOverride">Optional material override for the object.</param>
        public SceneObject(string name, Shape shape, Transform? transform = null, Material? materialOverride = null) : base(name, transform) {
            this.Shape = shape;
            this.MaterialOverride = materialOverride;
        }

        /// <inheritdoc/>
        public bool Intersect(Ray ray, ref HitRecord hit) {
            if (Material is null) return false;
            // Transform the ray to local space
            Vector3 transformedOrigin = Vector3.TransformPosition(ray.Origin, WorldToLocal);
            Vector3 transformedDirection = Vector3.TransformVector(ray.Direction, WorldToLocal);

            Ray localRay = new Ray(
                transformedOrigin,
                Vector3.Normalize(transformedDirection)
            );

            // Check intersection in local space
            if (Shape.Intersect(localRay, ref hit)) {
                // Compensate hit distance due to scaled ray direction
                float directionLengthScale = transformedDirection.Length;
                hit.T /= directionLengthScale;

                // Transform hit point and normal back to world space
                hit.Point = Vector3.TransformPosition(hit.Point, LocalToWorld);
                hit.Normal = Vector3.Normalize(Vector3.TransformNormalInverse(hit.Normal, WorldToLocal));
                hit.Material = Material;
                hit.Normal = Material.GetNormal(hit); // Get the normal from the material, which may use a normal map
                hit.IsFrontFace = Vector3.Dot(ray.Direction, hit.Normal) < 0;
                if (!hit.IsFrontFace) hit.Normal = -hit.Normal;

                return true;
            }

            return false;
        }

        /// <summary>
        /// Updates the material for this object, unless a material override is set.
        /// </summary>
        /// <param name="material">The new material to assign, or null to inherit from parent.</param>
        public void UpdateMaterial(Material? material = null) {
            if (MaterialOverride is not null) return;
            Material = material ?? Parent?.Material;
        }

        /// <summary>
        /// Updates the material for this object when the override changes.
        /// </summary>
        /// <param name="material">The new material to assign, or null to inherit from parent.</param>
        private void OnMaterialOverrideUpdated(Material? material = null) => Material = material ?? Parent?.Material;
    }

    /// <summary>
    /// Represents an empty shape that never intersects with rays.
    /// </summary>
    public class Empty : Shape {
        /// <inheritdoc/>
        public override bool Intersect(Ray ray, ref HitRecord hit) {
            return false;
        }
        /// <inheritdoc/>
        public override Vector3 GetNormal(Vector3 hitPoint) {
            return Vector3.Zero;
        }
        /// <inheritdoc/>
        public override (float u, float v) GetUV(Vector3 hitPoint) {
            return (0f, 0f);
        }
    }

    /// <summary>
    /// Represents a unit sphere centered at the origin.
    /// </summary>
    public class Sphere : Shape {
        /// <inheritdoc/>
        public override bool Intersect(Ray ray, ref HitRecord hit) {
            Vector3 origin = ray.Origin;
            Vector3 direction = ray.Direction;
            float a = Vector3.Dot(direction, direction);
            float b = 2.0f * Vector3.Dot(origin, direction);
            float c = Vector3.Dot(origin, origin) - 1f;
            float discriminant = b * b - 4 * a * c;

            if (discriminant < 0) return false;
            float sqrtDisc = MathF.Sqrt(discriminant);
            float t1 = (-b - sqrtDisc) / (2.0f * a);
            float t2 = (-b + sqrtDisc) / (2.0f * a);

            float t = float.MaxValue;
            if (t1 > 0 && t1 < t) t = t1;
            if (t2 > 0 && t2 < t) t = t2;
            if (t == float.MaxValue) return false;

            SetHit(ray, t, ref hit);
            return true;
        }

        /// <inheritdoc/>
        public override Vector3 GetNormal(Vector3 hitPoint) {
            return Vector3.Normalize(hitPoint);
        }

        /// <inheritdoc/>
        public override (float u, float v) GetUV(Vector3 hitPoint) {
            float u = 0.5f + MathF.Atan2(hitPoint.Z, hitPoint.X) / (2 * MathF.PI);
            float v = 0.5f - MathF.Asin(hitPoint.Y) / MathF.PI;
            return (u, v);
        }
    }

    /// <summary>
    /// Represents an infinite horizontal plane at y=0.
    /// </summary>
    public class Plane : Shape {
        private Vector3 _normal = new Vector3(0, 1, 0);

        /// <inheritdoc/>
        public override bool Intersect(Ray ray, ref HitRecord hit) {
            float denom = Vector3.Dot(_normal, ray.Direction);
            if (MathF.Abs(denom) > 1e-6f) {
                float t = -ray.Origin.Y / ray.Direction.Y;
                if (t > 0) {
                    SetHit(ray, t, ref hit);
                    return true;
                }
            }
            return false;
        }

        /// <inheritdoc/>
        public override Vector3 GetNormal(Vector3 point) {
            return _normal;
        }

        /// <inheritdoc/>
        public override (float u, float v) GetUV(Vector3 hitPoint) {
            float u = hitPoint.X % 1f;
            float v = hitPoint.Z % 1f;
            if (u < 0) u += 1f;
            if (v < 0) v += 1f;
            return (u, v);
        }
    }

    /// <summary>
    /// Represents a finite vertical cylinder of unit radius and height 1, centered at the origin.
    /// </summary>
    public class Cylinder : Shape {
        private Vector3 _axis = new Vector3(0, 1, 0);

        /// <inheritdoc/>
        public override bool Intersect(Ray ray, ref HitRecord hit) {
            Vector3 origin = ray.Origin;
            Vector3 direction = ray.Direction;
            List<float> tValues = new List<float>();

            // Curved surface intersections
            float a = direction.X * direction.X + direction.Z * direction.Z;
            float b = 2 * (origin.X * direction.X + origin.Z * direction.Z);
            float c = origin.X * origin.X + origin.Z * origin.Z - 1f;

            float discriminant = b * b - 4 * a * c;
            if (discriminant < 0) return false;

            float sqrtDisc = MathF.Sqrt(discriminant);
            float t1 = (-b - sqrtDisc) / (2 * a);
            float t2 = (-b + sqrtDisc) / (2 * a);

            if (t1 > t2) (t1, t2) = (t2, t1);
            float y1 = origin.Y + t1 * direction.Y;
            float y2 = origin.Y + t2 * direction.Y;

            if (y1 > 0 && y1 < 1 && t1 > 0) tValues.Add(t1);
            if (y2 > 0 && y2 < 1 && t2 > 0) tValues.Add(t2);

            // Basis intersections
            if (MathF.Abs(direction.Y) > 1e-6f) {
                float tTop = (1f - origin.Y) / direction.Y;
                Vector3 topPoint = ray.GetPoint(tTop);
                Vector3 posTop = new Vector3(topPoint.X, 0f, topPoint.Z);
                if (posTop.LengthSquared <= 1f && tTop > 0) tValues.Add(tTop);

                float tBottom = -origin.Y / direction.Y;
                Vector3 bottomPoint = ray.GetPoint(tBottom);
                Vector3 posBottom = new Vector3(bottomPoint.X, 0f, bottomPoint.Z);
                if (posBottom.LengthSquared <= 1f && tBottom > 0) tValues.Add(tBottom);
            }

            tValues.Sort();
            if (tValues.Count > 0) {
                SetHit(ray, tValues[0], ref hit);
                return true;
            }
            return false;
        }

        /// <inheritdoc/>
        public override Vector3 GetNormal(Vector3 hitPoint) {
            if (MathF.Abs(hitPoint.Y - 0f) < 1e-6f) return -_axis;
            if (MathF.Abs(hitPoint.Y - 1f) < 1e-6f) return _axis;
            return Vector3.Normalize(new Vector3(hitPoint.X, 0, hitPoint.Z));
        }

        /// <inheritdoc/>
        public override (float u, float v) GetUV(Vector3 hitPoint) {
            float u, v;
            if (MathF.Abs(hitPoint.Y - 0f) < Constants.Epsilon) {
                u = hitPoint.X + 1f;
                v = hitPoint.Z + 1f;
                return (u, v);
            }
            if (MathF.Abs(hitPoint.Y - 1f) < Constants.Epsilon) {
                u = hitPoint.X + 1f;
                v = hitPoint.Z + 1f;
                return (u, v);
            }
            u = MathF.Atan2(hitPoint.Z, hitPoint.X) + 0.5f;
            v = Math.Clamp(hitPoint.Y, 0f, 1f);
            return (u, v);
        }
    }
}