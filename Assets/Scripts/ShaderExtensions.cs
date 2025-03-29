

using UnityEngine;

public static class ShaderExtensions {
    public static void SetStructBuffer<T>(this ComputeShader shader, int kernelIndex, ComputeStructBuffer<T> buf) where T : struct {

    }
}