using rt004.shared;
using OpenTK.Mathematics;

public enum SpecularModel {
    Phong,
    BlinnPhong
}

public static class Lighting {
    public static SpecularModel CurrentModel = SpecularModel.BlinnPhong;

    public static Vector3 ComputePhongReflection(Vector3 normal, Vector3 lightDir, Vector3 viewDir, Material material) {
        float diff = MathF.Max(Vector3.Dot(normal, lightDir), 0.0f);
        Vector3 diffuse = material.Diffuse * diff;

        Vector3 specular = Vector3.Zero;

        if (CurrentModel == SpecularModel.Phong) {
            // Phong specular
            Vector3 reflectDir = lightDir - 2 * Vector3.Dot(lightDir, normal) * normal;
            float specAngle = MathF.Max(Vector3.Dot(viewDir, Vector3.Normalize(reflectDir)), 0.0f);
            float spec = MathF.Pow(specAngle, material.Shininess);
            specular = material.Specular * spec;
        }
        else if (CurrentModel == SpecularModel.BlinnPhong) {
            // Blinn-Phong specular
            Vector3 halfDir = Vector3.Normalize(lightDir + viewDir);
            float specAngle = MathF.Max(Vector3.Dot(normal, halfDir), 0.0f);
            float spec = MathF.Pow(specAngle, material.Shininess);
            specular = material.Specular * spec;
        }

        return diffuse + specular;
    }
}
