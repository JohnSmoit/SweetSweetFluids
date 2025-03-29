using UnityEngine;

public static class VectorUtils {

    public static Vector4 Rand(float minx, float maxx, float miny, float maxy, float minz, float maxz) {
        return new Vector4(
            Random.Range(minx, maxx),
            Random.Range(miny, maxy),
            Random.Range(minz, maxz)
        );
    }

    public static Vector4 ToVector4(this Rect rect) {
        return new(
            rect.x,
            rect.y,
            rect.width,
            rect.height
        );
    }
}