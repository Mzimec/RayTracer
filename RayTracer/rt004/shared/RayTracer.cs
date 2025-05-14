using OpenTK.Mathematics;
using System;
using System.Collections.Generic;
using System.Drawing;
using Util;

namespace rt004.shared {
    public class RayTracer {
        private List<SceneObject> objects;
        private List<LightSource> lights;
        private Camera camera;
        private int maxDepth = 5; // Maximum recursion depth for ray tracing
        private float minContribution = 0.01f; // Minimum contribution for reflection/refraction
        private Vector3 backgroundColor = Vector3.Zero; // Background color
        private bool areReflectionsOn; // Reflection flag
        private bool isTransparencyOn; // Transparency flag
        private bool isShadowingOn; // Shadow flag

        public RayTracer(List<SceneObject> objects, List<LightSource> lights, Camera camera, int maxDepth,
            Vector3 backgroundColor, bool areReflectionsOn = true, bool isTransparencyOn = true, bool isShadowingOn = true) {

            this.objects = objects;
            this.lights = lights;
            this.camera = camera;
            this.maxDepth = maxDepth;
            this.backgroundColor = backgroundColor;
            this.areReflectionsOn = areReflectionsOn;
            this.isTransparencyOn = isTransparencyOn;
            this.isShadowingOn = isShadowingOn;
        }

        public Vector3 TraceRay(Vector3 origin, Vector3 direction, int depth, float contribution) {

            if (depth > maxDepth) return backgroundColor; // Return black if max depth is reached

            // Check for intersection with all objects. And find the closest one
            float closestT = float.MaxValue;
            SceneObject closestObject = null;
            Vector3 hitPoint = Vector3.Zero;

            foreach (var obj in objects) {
                if (obj.Intersect(origin, direction, out float t)) {
                    if (t < closestT && t > 0) {
                        closestT = t;
                        closestObject = obj;
                    }
                }
            }

            if(closestObject == null) return backgroundColor; // No intersection, return background color

            // Get properties of the closest object
            hitPoint = origin + direction * closestT; // Calculate the hit point
            Vector3 normal = Vector3.Normalize(closestObject.GetNormal(hitPoint));
            Material material = closestObject.GetMaterial();
            Vector3 color = Vector3.Zero; // Start with black color

            // Calculate the ligting contribution
            color += LightingContribution(color, material, hitPoint, normal, closestObject);

            // Add reflection and refraction contributions
            if (material.IsReflective && areReflectionsOn) {
                float reflectionContribution = material.Reflectivity * contribution;
                if (reflectionContribution > minContribution) {
                    Vector3 reflectDir = Reflection(direction, normal);
                    color += material.Reflectivity * TraceRay(hitPoint + normal * 1e-4f, reflectDir, depth + 1, reflectionContribution);                    
                }
            }

            // Add refraction contribution
            if (material.IsTransparent && isTransparencyOn) {
                float refractionContribution = material.Transparency * contribution;
                if (refractionContribution > minContribution) {
                    Vector3 refractDir = Refraction(direction, normal, material);
                    color += material.Transparency * TraceRay(hitPoint - normal * 1e-4f, refractDir, depth + 1, refractionContribution);
                }
            }

            return color;
        }

        public FloatImage Render() {
            FloatImage image = new FloatImage(camera.ImageWidth, camera.ImageHeight, 3);

            for (int x = 0; x < camera.ImageWidth; x++) {
                for (int y = 0; y < camera.ImageHeight; y++) {
                    var (origin, direction) = camera.GenerateRay(x, y);
                    Vector3 color = TraceRay(origin, direction, 0,1f);

                    // Normalize or tone map the HDR color (optional, but needed for realistic rendering)
                    color = Vector3.Clamp(color, Vector3.Zero, Vector3.One);


                    // Output or store the color for each pixel (this could be saved to an image file)
                    image.PutPixel(x, y, [color.X, color.Y, color.Z]);
                }
            }

            return image;
        }

        private Vector3 LightingContribution(Vector3 color, Material material, Vector3 hitPoint, Vector3 normal, SceneObject closestObject) {
            foreach (var light in lights) {
                if (light is AmbientLight ambientLight) {
                    color += ambientLight.GetIntensity(hitPoint) * material.Ambient; // Ambient light contribution
                }
                else {
                    Vector3 lightDir = light.GetDirection(hitPoint);
                    Vector3 viewDir = Vector3.Normalize(camera.Position - hitPoint);
                    float lightDistance = 0;
                    bool isLightPositional = false;
                    if (light is ITransform positionalLight) {
                        isLightPositional = true;
                        lightDistance = Vector3.Distance(positionalLight.Position, hitPoint);
                    }
                    // If not in shadow, add light contribution
                    if (!IsInShadow(hitPoint, normal, lightDir, closestObject, isLightPositional, lightDistance)) {
                        Vector3 lightIntensity = light.GetIntensity(hitPoint);
                        color += lightIntensity * Lighting.ComputePhongReflection(normal, lightDir, viewDir, material);
                    }
                }
            }
            return color;
        }

        private bool IsInShadow(Vector3 hitPoint, Vector3 normal, Vector3 lightDir, SceneObject closestObject, bool isPositional, float distance) {
            if (!isShadowingOn) return false;

            // Check if the light source is occluded by any other object
            bool inShadow = false;
            foreach (var obj in objects) {
                if (obj != closestObject) { 
                    Vector3 shadowOrigin = hitPoint + normal * 1e-4f; // Offset to avoid self-intersection 
                    if (obj.Intersect(shadowOrigin, lightDir, out float shadowT) && shadowT > 0) {
                        if (isPositional && distance < shadowT) continue;
                        inShadow = true;
                        break;
                    }
                }
            }
            return inShadow;
        }

        private Vector3 Reflection(Vector3 direction, Vector3 normal) {
            return Vector3.Normalize(direction - 2 * Vector3.Dot(direction, normal) * normal);
        }

        private Vector3 Refraction(Vector3 direction, Vector3 normal, Material material) {
            // Calculate the ratio of indices (1.0f is the refractive index of air)
            float eta = 1.0f / material.RefractiveIndex;

            // Compute the dot product between incoming direction and normal
            float cosI = Vector3.Dot(normal, direction);
            float sinT2 = eta * eta * (1 - cosI * cosI); // sin(theta_t)^2

            // If total internal reflection occurs, return the reflection direction
            if (sinT2 > 1.0f) {
                // Use the reflection function for total internal reflection
                return Reflection(direction, normal);
            }

            // Calculate the refracted direction using Snell's law
            float cosT = MathF.Sqrt(1.0f - sinT2); // cos(theta_t)
            Vector3 refractDir = eta * direction - (eta * cosI + cosT) * normal;

            return Vector3.Normalize(refractDir); // Ensure the refraction direction is normalized
        }

    }
}
