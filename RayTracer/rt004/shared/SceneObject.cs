using System;
using System.Collections.Generic;
using System.Linq;
using OpenTK.Mathematics;
using System.Text;
using System.Threading.Tasks;

namespace rt004.shared {

    public interface ITransform {
        public Vector3 Position { get; set; }
        public Quaternion Rotation { get; set; } 
        public Quaternion Scale { get; set; }
    }
    public abstract class SceneObject : ITransform {
        public Vector3 Position { get; set; }
        public Quaternion Rotation { get; set; }
        public Quaternion Scale { get; set; }

        protected Material material;

        public SceneObject(Material material, Vector3 position) {  
            this.material = material;
            this.Position = position;
        }

        public abstract bool Intersect(Vector3 origin, Vector3 direction, out float t);
        public abstract Vector3 GetNormal(Vector3 point);

        public Material GetMaterial() { return material; }

    }

    public class Sphere : SceneObject {
        public float Radius { get; }

        public Sphere( float radius, Material material, Vector3 position) : 
            base(material, position) {

            Radius = radius;
        }

        public override bool Intersect(Vector3 origin, Vector3 direction, out float t) {
            Vector3 oc = origin - Position;
            float a = Vector3.Dot(direction, direction);
            float b = 2.0f * Vector3.Dot(oc, direction);
            float c = Vector3.Dot(oc, oc) - Radius * Radius;
            float discriminant = b * b - 4 * a * c;

            if (discriminant < 0) {
                t = -1;
                return false;
            }
            else {
                float t1 = (-b - MathF.Sqrt(discriminant)) / (2.0f * a);
                float t2 = (-b + MathF.Sqrt(discriminant)) / (2.0f * a);

                // Pick the smallest t that is greater than zero
                t = (t1 > t2 && t2 >= 0) ? t2 : t1;
                return t > 0;
            }
        }

        public override Vector3 GetNormal(Vector3 hitPoint) {
            return Vector3.Normalize(hitPoint - Position);
        }
    }

    public class Plane : SceneObject {
        public Vector3 Normal { get; }

        public Plane(Vector3 normal, Material material, Vector3 position) :
            base(material, position) {
            
            Normal = Vector3.Normalize(normal);
        }

        public override bool Intersect(Vector3 origin, Vector3 direction, out float t) {
            float denom = Vector3.Dot(Normal, direction);
            if (MathF.Abs(denom) > 1e-6f) {
                t = Vector3.Dot(Position - origin, Normal) / denom;
                return t > 0;
            }
            t = float.MaxValue;
            return false;
        }

        public override Vector3 GetNormal(Vector3 point) {
            return Normal;
        }
    }

    public class Cylinder : SceneObject {
        public float Radius { get; }
        public float Height { get; }
        public Vector3 Axis { get; }

        public Cylinder(Vector3 axis, float radius, float height, Material material, Vector3 position) :
            base(material, position) {
            
            Axis = Vector3.Normalize(axis);
            Radius = radius;
            Height = height;
        }

        public override bool Intersect(Vector3 origin, Vector3 direction, out float t) {
            t = float.MaxValue; // Ensure 't' is assigned before returning

            Vector3 d = direction - Vector3.Dot(direction, Axis) * Axis;
            Vector3 o = (origin - Position) - Vector3.Dot(origin - Position, Axis) * Axis;

            float a = Vector3.Dot(d, d);
            float b = 2 * Vector3.Dot(o, d);
            float c = Vector3.Dot(o, o) - Radius * Radius;
            float discriminant = b * b - 4 * a * c;

            if (discriminant < 0) {
                return false;
            }

            float t0 = (-b - MathF.Sqrt(discriminant)) / (2.0f * a);
            float t1 = (-b + MathF.Sqrt(discriminant)) / (2.0f * a);

            if (t0 > t1) (t0, t1) = (t1, t0);

            float y0 = Vector3.Dot(Axis, (origin + t0 * direction) - Position);
            float y1 = Vector3.Dot(Axis, (origin + t1 * direction) - Position);

            if (y0 < 0) {
                if (y1 < 0) return false;
                t = t1;
            }
            else if (y0 > Height) {
                if (y1 > Height) return false;
                t = t1;
            }
            else {
                t = t0;
            }

            return t > 0;
        }

        public override Vector3 GetNormal(Vector3 hitPoint) {
            // Compute the projection of the hit point onto the cylinder's axis
            Vector3 v = hitPoint - Position;
            float projection = Vector3.Dot(v, Axis);

            // Check if we are at the top or bottom caps
            if (projection <= 0) return -Axis; // Bottom cap normal
            if (projection >= Height) return Axis; // Top cap normal

            // Otherwise, we are on the curved surface
            Vector3 centerToHit = v - projection * Axis;
            return Vector3.Normalize(centerToHit);
        }
    }
}
