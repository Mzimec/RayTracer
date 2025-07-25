using rt004.shared;
using OpenTK.Mathematics;


public class LightingModel {
    public virtual Vector3 Shade(HitRecord hit, Vector3 lightDir, Vector3 viewDir) {
        float diff = MathF.Max(Vector3.Dot(hit.Normal, lightDir), 0.0f);
        Vector3 diffuse = hit.Material.Diffuse * diff;

        Vector3 specular = ComputeSpecular(hit, lightDir, viewDir);
        return diffuse + specular;
    }

    protected virtual Vector3 ComputeSpecular(HitRecord hit, Vector3 lightDir, Vector3 viewDir) {
        return Vector3.Zero;
    }
}

public class PhongLightingModel : LightingModel {
    protected override Vector3 ComputeSpecular(HitRecord hit, Vector3 lightDir, Vector3 viewDir) {
        Vector3 reflectDir = lightDir - 2 * Vector3.Dot(lightDir, hit.Normal) * hit.Normal;
        float specAngle = MathF.Max(Vector3.Dot(viewDir, Vector3.Normalize(reflectDir)), 0.0f);
        float spec = MathF.Pow(specAngle, hit.Material.Shininess);
        return hit.Material.Specular * spec;
    }
}

public class BlinnPhongLightingModel : LightingModel {
    protected override Vector3 ComputeSpecular(HitRecord hit, Vector3 lightDir, Vector3 viewDir) {
        Vector3 halfDir = Vector3.Normalize(lightDir + viewDir);
        float specAngle = MathF.Max(Vector3.Dot(hit.Normal, halfDir), 0.0f);
        float spec = MathF.Pow(specAngle, hit.Material.Shininess);
        return hit.Material.Specular * spec;
    }
}
