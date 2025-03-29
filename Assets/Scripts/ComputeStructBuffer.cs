
using System.Reflection;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using System.Runtime.InteropServices;
using UnityEditor;

// Please Note: Does not work with SubUpdate...
public class ComputeStructBuffer<T>
{
    private readonly Dictionary<string, ComputeBufferMapping> fieldShaderIdMap = new();
    public ComputeShader shader;
    public ComputeStructBuffer(int count, ComputeBufferMode mode) {
        // using reflection, build a field map of the template provided structure
        // setup internal compute buffers as well
        T dummy = default;
        foreach (var info in typeof(T).GetFields(BindingFlags.Public | BindingFlags.Instance)) {
            ComputeBufferMapping m = new()
            {
                propertyID = Shader.PropertyToID(info.Name),
                buffer = new ComputeBuffer(count, GetPrimitiveSize(info.GetValue(dummy)), ComputeBufferType.Structured, ComputeBufferMode.Dynamic),
                fieldType = info.GetType(),
            };
            fieldShaderIdMap.Add(info.Name, m);

        }

        // do a quick test...
    }

    private int GetPrimitiveSize(object field) {
        return field switch
        {
            float _ => 4,
            int _ => 4,
            Vector4 _ => 16,
            Vector3 _ => 12,
            Vector2 _ => 8,
            _ => throw new Exception("Fuckll"),
        };
    }
    public void Release() {
        foreach (var pair in fieldShaderIdMap) {
            pair.Value.buffer?.Release();
        }
    }

    public void SetData(T[] particles, int startIndex, int gpuStartIndex, int count) {
        // using reflection, build separate arrays and write them to the GPU individually

        foreach (var mapping in fieldShaderIdMap.Values) {
            Array data = particles.ExtractRange(mapping.fieldType, mapping.FieldName, startIndex, count);
            ComputeBuffer buf = fieldShaderIdMap[mapping.FieldName].buffer;

            buf.SetData(data, startIndex, gpuStartIndex, count);
        }
    }

    private struct ComputeBufferMapping {
        public int propertyID;
        public ComputeBuffer buffer;
        public Type fieldType;
        public string FieldName => fieldType.Name;

        public override readonly string ToString()
        {
            return ReflectionUtils.Stringify(this);
        }
    }
}