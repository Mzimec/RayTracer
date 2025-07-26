// ReSharper disable once InconsistentNaming
// ReSharper disable once CheckNamespace
using OpenTK.Mathematics;

namespace Util;

/// <summary>
/// Math utilities.
/// </summary>
public abstract class MathUtil
{
  public static bool IsZero(
    double a)
  {
    return a <= double.Epsilon && a >= -double.Epsilon;
  }

  /// <summary>
  /// Mirror reflection vector computed from unit vectors.
  /// </summary>
  /// <param name="normal">Unit normal vector.</param>
  /// <param name="input">Unit vector pointing towards the source.</param>
  /// <param name="output">Unit output reflected vector.</param>
  public static void IdealReflection(
    in Vector3d normal,
    in Vector3d input,
    out Vector3d output)
  {
    Vector3d.Dot(in normal, in input, out double k);
    output = (k + k) * normal - input;
  }

  /// <summary>
  /// Refraction vector computed from unit vectors.
  /// </summary>
  /// <param name="normal">Unit normal vector.</param>
  /// <param name="n">Relative refractive index.</param>
  /// <param name="input">Unit vector pointing towards the source.</param>
  /// <param name="output">Unit output refracted vector.</param>
  /// <returns>True if refraction actually happened, false for total reflection.</returns>
  public static bool IdealRefraction(
    in Vector3d normal,
    double n,
    in Vector3d input,
    out Vector3d output)
  {
    double d = Vector3d.Dot(normal, input);
    double flip = 1.0;

    if (d < 0.0) // (N*L) should be > 0.0 (N and L in the same half-space)
    {
      flip = -1.0;
      d = -d;
    }
    else
      n = 1.0 / n;

    double cos2 = 1.0 - n * n * (1.0 - d * d);
    if (cos2 <= 0.0)
    {
      output = Vector3d.Zero;
      return false; // total reflection
    }

    d = n * d - Math.Sqrt(cos2);
    output = normal * flip * d - input * n;
    return true;
  }

  /// <summary>
  /// gluUnproject substitution.
  /// </summary>
  /// <param name="projection">Projection matrix.</param>
  /// <param name="view">View matrix.</param>
  /// <param name="width">Viewport width in pixels.</param>
  /// <param name="height">Viewport height in pixels.</param>
  /// <param name="x">Pointing 2D screen coordinate - x.</param>
  /// <param name="y">Pointing 2D screen coordinate - y.</param>
  /// <param name="z">Supplemental z-coord (0.0 for a near point, 1.0 for a far point).</param>
  /// <returns>Coordinate in the world-space.</returns>
  public static Vector3 UnProject(
    in Matrix4 projection,
    in Matrix4 view,
    int width,
    int height,
    float x,
    float y,
    float z = 0.0f)
  {
    Vector4 vec;
    vec.X = 2.0f * x / width - 1.0f;
    vec.Y = 2.0f * y / height - 1.0f;
    vec.Z = z;
    vec.W = 1.0f;

    Matrix4 viewInv = Matrix4.Invert(view);
    Matrix4 projInv = Matrix4.Invert(projection);

    Vector4.TransformRow(in vec, in projInv, out vec);
    Vector4.TransformRow(in vec, in viewInv, out vec);

    if (vec.W >  float.Epsilon ||
        vec.W < -float.Epsilon)
    {
      vec.X /= vec.W;
      vec.Y /= vec.W;
      vec.Z /= vec.W;
      vec.W = 1.0f;
    }

    return new(vec.X, vec.Y, vec.Z);
  }

  /// <summary>
  /// Ray vs. AABB intersection, direction vector in regular form,
  /// box defined by lower-left corner and size.
  /// </summary>
  /// <param name="result">Parameter (t) bounds: [min, max].</param>
  /// <returns>True if intersections exist.</returns>
  public static bool RayBoxIntersection(
    in Vector3d p0,
    in Vector3d p1,
    in Vector3d ul,
    in Vector3d size,
    out Vector2d result)
  {
    result.X =
    result.Y = -1.0;
    double tMin = double.NegativeInfinity;
    double tMax = double.PositiveInfinity;
    double t1, t2, mul;

    // X axis:
    if (IsZero(p1.X))
    {
      if (p0.X <= ul.X ||
          p0.X >= ul.X + size.X)
        return false;
    }
    else
    {
      mul = 1.0 / p1.X;
      t1 = (ul.X - p0.X) * mul;
      t2 = t1 + size.X * mul;

      if (mul > 0.0)
      {
        if (t1 > tMin) tMin = t1;
        if (t2 < tMax) tMax = t2;
      }
      else
      {
        if (t2 > tMin) tMin = t2;
        if (t1 < tMax) tMax = t1;
      }

      if (tMin > tMax)
        return false;
    }

    // Y axis:
    if (IsZero(p1.Y))
    {
      if (p0.Y <= ul.Y ||
          p0.Y >= ul.Y + size.Y)
        return false;
    }
    else
    {
      mul = 1.0 / p1.Y;
      t1 = (ul.Y - p0.Y) * mul;
      t2 = t1 + size.Y * mul;

      if (mul > 0.0)
      {
        if (t1 > tMin) tMin = t1;
        if (t2 < tMax) tMax = t2;
      }
      else
      {
        if (t2 > tMin) tMin = t2;
        if (t1 < tMax) tMax = t1;
      }

      if (tMin > tMax)
        return false;
    }

    // Z axis:
    if (IsZero(p1.Z))
    {
      if (p0.Z <= ul.Z ||
          p0.Z >= ul.Z + size.Z)
        return false;
    }
    else
    {
      mul = 1.0 / p1.Z;
      t1 = (ul.Z - p0.Z) * mul;
      t2 = t1 + size.Z * mul;

      if (mul > 0.0)
      {
        if (t1 > tMin) tMin = t1;
        if (t2 < tMax) tMax = t2;
      }
      else
      {
        if (t2 > tMin) tMin = t2;
        if (t1 < tMax) tMax = t1;
      }

      if (tMin > tMax)
        return false;
    }

    result.X = tMin;
    result.Y = tMax;
    return true;
  }

  /// <summary>
  /// Ray vs. AABB intersection, direction vector in inverted form,
  /// box defined by lower-left corner and size.
  /// </summary>
  /// <param name="result">Parameter (t) bounds: [min, max].</param>
  /// <returns>True if intersections exist.</returns>
  public static bool RayBoxIntersectionInv(
    in Vector3d p0,
    in Vector3d p1inv,
    in Vector3d ul,
    in Vector3d size,
    out Vector2d result)
  {
    result.X =
    result.Y = -1.0;
    double tMin = double.NegativeInfinity;
    double tMax = double.PositiveInfinity;
    double t1, t2;

    // X axis:
    if (double.IsInfinity(p1inv.X))
    {
      if (p0.X <= ul.X ||
          p0.X >= ul.X + size.X)
        return false;
    }
    else
    {
      t1 = (ul.X - p0.X) * p1inv.X;
      t2 = t1 + size.X * p1inv.X;

      if (p1inv.X > 0.0)
      {
        if (t1 > tMin) tMin = t1;
        if (t2 < tMax) tMax = t2;
      }
      else
      {
        if (t2 > tMin) tMin = t2;
        if (t1 < tMax) tMax = t1;
      }

      if (tMin > tMax)
        return false;
    }

    // Y axis:
    if (double.IsInfinity(p1inv.Y))
    {
      if (p0.Y <= ul.Y ||
          p0.Y >= ul.Y + size.Y)
        return false;
    }
    else
    {
      t1 = (ul.Y - p0.Y) * p1inv.Y;
      t2 = t1 + size.Y * p1inv.Y;

      if (p1inv.Y > 0.0)
      {
        if (t1 > tMin) tMin = t1;
        if (t2 < tMax) tMax = t2;
      }
      else
      {
        if (t2 > tMin) tMin = t2;
        if (t1 < tMax) tMax = t1;
      }

      if (tMin > tMax)
        return false;
    }

    // Z axis:
    if (double.IsInfinity(p1inv.Z))
    {
      if (p0.Z <= ul.Z ||
          p0.Z >= ul.Z + size.Z)
        return false;
    }
    else
    {
      t1 = (ul.Z - p0.Z) * p1inv.Z;
      t2 = t1 + size.Z * p1inv.Z;

      if (p1inv.Z > 0.0)
      {
        if (t1 > tMin) tMin = t1;
        if (t2 < tMax) tMax = t2;
      }
      else
      {
        if (t2 > tMin) tMin = t2;
        if (t1 < tMax) tMax = t1;
      }

      if (tMin > tMax)
        return false;
    }

    result.X = tMin;
    result.Y = tMax;
    return true;
  }

  /// <summary>
  /// Ray vs. AABB intersection, direction vector in inverted form,
  /// box defined by lower-left corner and size.
  /// </summary>
  /// <param name="result">Parameter (t) bounds: [min, max].</param>
  /// <returns>True if intersections exist.</returns>
  public static bool RayBoxIntersectionInv(
    in Vector3d p0,
    in Vector3d p1inv,
    in Vector3 ul,
    in Vector3 size,
    out Vector2d result)
  {
    result.X =
    result.Y = -1.0;
    double tMin = double.NegativeInfinity;
    double tMax = double.PositiveInfinity;
    double t1, t2;

    // X axis:
    if (double.IsInfinity(p1inv.X))
    {
      if (p0.X <= ul.X ||
          p0.X >= ul.X + size.X)
        return false;
    }
    else
    {
      t1 = (ul.X - p0.X) * p1inv.X;
      t2 = t1 + size.X * p1inv.X;

      if (p1inv.X > 0.0)
      {
        if (t1 > tMin) tMin = t1;
        if (t2 < tMax) tMax = t2;
      }
      else
      {
        if (t2 > tMin) tMin = t2;
        if (t1 < tMax) tMax = t1;
      }

      if (tMin > tMax)
        return false;
    }

    // Y axis:
    if (double.IsInfinity(p1inv.Y))
    {
      if (p0.Y <= ul.Y ||
          p0.Y >= ul.Y + size.Y)
        return false;
    }
    else
    {
      t1 = (ul.Y - p0.Y) * p1inv.Y;
      t2 = t1 + size.Y * p1inv.Y;

      if (p1inv.Y > 0.0)
      {
        if (t1 > tMin) tMin = t1;
        if (t2 < tMax) tMax = t2;
      }
      else
      {
        if (t2 > tMin) tMin = t2;
        if (t1 < tMax) tMax = t1;
      }

      if (tMin > tMax)
        return false;
    }

    // Z axis:
    if (double.IsInfinity(p1inv.Z))
    {
      if (p0.Z <= ul.Z ||
          p0.Z >= ul.Z + size.Z)
        return false;
    }
    else
    {
      t1 = (ul.Z - p0.Z) * p1inv.Z;
      t2 = t1 + size.Z * p1inv.Z;

      if (p1inv.Z > 0.0)
      {
        if (t1 > tMin) tMin = t1;
        if (t2 < tMax) tMax = t2;
      }
      else
      {
        if (t2 > tMin) tMin = t2;
        if (t1 < tMax) tMax = t1;
      }

      if (tMin > tMax)
        return false;
    }

    result.X = tMin;
    result.Y = tMax;
    return true;
  }

  /// <summary>
  /// Ray-triangle intersection test in 3D.
  /// According to Tomas Moller and Ben Trumbore:
  /// https://en.wikipedia.org/wiki/M%C3%B6ller%E2%80%93Trumbore_intersection_algorithm
  /// http://www.graphics.cornell.edu/pubs/1997/MT97.pdf
  /// https://fileadmin.cs.lth.se/cs/Personal/Tomas_Akenine-Moller/raytri/
  /// https://fileadmin.cs.lth.se/cs/Personal/Tomas_Akenine-Moller/code/
  /// https://github.com/erich666/jgt-code/tree/master/Volume_02/Number_1/Moller1997a
  /// (origin + t * direction = (1 - u - v) * a + u * b + v * c)
  /// Even faster algorithm by (Havel & Herout): https://www.researchgate.net/publication/41910471_Yet_Faster_Ray-Triangle_Intersection_Using_SSE4
  /// </summary>
  /// <param name="p0">Ray origin.</param>
  /// <param name="p1">Ray direction vector.</param>
  /// <param name="a">Vertex A of the triangle.</param>
  /// <param name="b">Vertex B of the triangle.</param>
  /// <param name="c">Vertex C of the triangle.</param>
  /// <param name="uv">Barycentric coordinates of the intersection.</param>
  /// <returns>Parametric coordinate on the ray if succeeded, double.NegativeInfinity otherwise.</returns>
  public static double RayTriangleIntersection(
    in Vector3d p0,
    in Vector3d p1,
    in Vector3d a,
    in Vector3d b,
    in Vector3d c,
    out Vector2d uv)
  {
    Vector3d e1 = b - a;
    Vector3d e2 = c - a;
    Vector3d.Cross(in p1, in e2, out Vector3d pvec);
    Vector3d.Dot(in e1, in pvec, out double det);
    uv.X = uv.Y = 0.0;
    if (IsZero(det))
      return double.NegativeInfinity;

    double detInv = 1.0 / det;
    Vector3d tvec = p0 - a;
    Vector3d.Dot(in tvec, in pvec, out uv.X);
    uv.X *= detInv;
    if (uv.X < 0.0 || uv.X > 1.0)
      return double.NegativeInfinity;

    Vector3d.Cross(in tvec, in e1, out Vector3d qvec);
    Vector3d.Dot(in p1, in qvec, out uv.Y);
    uv.Y *= detInv;
    if (uv.Y < 0.0 || uv.X + uv.Y > 1.0)
      return double.NegativeInfinity;

    Vector3d.Dot(in e2, in qvec, out det);
    return detInv * det;
  }

  /// <summary>
  /// Ray-triangle intersection test in 3D.
  /// According to Tomas Moller and Ben Trumbore:
  /// https://en.wikipedia.org/wiki/M%C3%B6ller%E2%80%93Trumbore_intersection_algorithm
  /// http://www.graphics.cornell.edu/pubs/1997/MT97.pdf
  /// https://fileadmin.cs.lth.se/cs/Personal/Tomas_Akenine-Moller/raytri/
  /// https://fileadmin.cs.lth.se/cs/Personal/Tomas_Akenine-Moller/code/
  /// https://github.com/erich666/jgt-code/tree/master/Volume_02/Number_1/Moller1997a
  /// (origin + t * direction = (1 - u - v) * a + u * b + v * c)
  /// Even faster algorithm by (Havel & Herout): https://www.researchgate.net/publication/41910471_Yet_Faster_Ray-Triangle_Intersection_Using_SSE4
  /// </summary>
  /// <param name="p0">Ray origin.</param>
  /// <param name="p1">Ray direction vector.</param>
  /// <param name="a">Vertex A of the triangle.</param>
  /// <param name="b">Vertex B of the triangle.</param>
  /// <param name="c">Vertex C of the triangle.</param>
  /// <param name="uv">Barycentric coordinates of the intersection.</param>
  /// <returns>Parametric coordinate on the ray if succeeded, double.NegativeInfinity otherwise.</returns>
  public static double RayTriangleIntersection(
    in Vector3d p0,
    in Vector3d p1,
    in Vector3 a,
    in Vector3 b,
    in Vector3 c,
    out Vector2d uv)
  {
    Vector3d e1 = (Vector3d)b - (Vector3d)a;
    Vector3d e2 = (Vector3d)c - (Vector3d)a;
    Vector3d.Cross(in p1, in e2, out Vector3d pvec);
    Vector3d.Dot(in e1, in pvec, out double det);
    uv.X = uv.Y = 0.0;
    if (IsZero(det))
      return double.NegativeInfinity;

    double detInv = 1.0 / det;
    Vector3d tvec = p0 - a;
    Vector3d.Dot(in tvec, in pvec, out uv.X);
    uv.X *= detInv;
    if (uv.X < 0.0 || uv.X > 1.0)
      return double.NegativeInfinity;

    Vector3d.Cross(in tvec, in e1, out Vector3d qvec);
    Vector3d.Dot(in p1, in qvec, out uv.Y);
    uv.Y *= detInv;
    if (uv.Y < 0.0 || uv.X + uv.Y > 1.0)
      return double.NegativeInfinity;

    Vector3d.Dot(in e2, in qvec, out det);
    return detInv * det;
  }
}
