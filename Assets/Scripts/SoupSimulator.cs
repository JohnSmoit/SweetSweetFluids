using System;
using Unity.VisualScripting;
using UnityEngine;


public class SoupSimulator
{
    public int ParticleCount
    {
        get
        {
            return _particleCount;
        }
        set
        {
            _particleCount = value;
            shader.SetInt("count", _particleCount);
        }
    }

    public float DT
    {
        get
        {
            return _dt;
        }
        set
        {
            _dt = value;
            shader.SetFloat(Shader.PropertyToID("dt"), _dt);
        }
    }
    public Rect SimBounds {
        get => _simBounds;
        set {
            _simBounds = value;
            shader.SetVector("bounds", _simBounds.ToVector4());
        }
    }
    private Rect _simBounds;
    public float Gravity
    {
        get => _gravity;
        set
        {
            _gravity = value;
            shader.SetFloat("gravity", _gravity);
        }
    }
    private float _gravity;
    public ComputeShader shader;
    private ComputeBuffer transformsBuffer;
    private ComputeBuffer positionsBuffer;
    private ComputeBuffer velocitiesBuffer;
    private ComputeBuffer densitiesBuffer;
    private float _dt = 0;
    private int _particleCount;


    private int SoupKernelIndex => shader.FindKernel("SoupKernel");
    private int DensityKernelIndex => shader.FindKernel("SoupDensityKernel");
    private int TBufIndex => Shader.PropertyToID("transforms");
    private int PosBufIndex => Shader.PropertyToID("positions");
    private int VelBufIndex => Shader.PropertyToID("velocities");
    private int DenBufIndex => Shader.PropertyToID("densities");

    public int this[string index] {
        get {
            return shader.FindKernel(index);
        }
    }

    public void GetBufferData<T>(T[] data, BufferType type)
    {
        if (data.Length < ParticleCount)
        {
            throw new ArgumentOutOfRangeException("Array length ust match particle count");
        }

        ModifyBuffer(GetBuf, type, data);
    }

    public void SetBufferData<T>(T[] data, BufferType type) {
        if (data.Length < ParticleCount)
        {
            throw new ArgumentOutOfRangeException("Array length ust match particle count");
        }

        ModifyBuffer(SetBuf, type, data);
    }

    private void GetBuf<T>(T[] data, ComputeBuffer buf) {
        buf.GetData(data);
    }

    private void SetBuf<T>(T[] data, ComputeBuffer buf) {
        buf.SetData(data);
    }

    Vector2 SmoothingKernelDeriv(Vector2 dist, float radius) {
        Vector2 r2 = new(radius * radius, radius * radius);
        Vector2 n2 = r2 - (dist * dist);
        Vector2 n3 = n2 * n2;
        Vector2 num = new Vector2(1, 1) * 945 * dist;

        if (dist.magnitude <= radius) {
            num *= n3;
        } else {
            num *= 0;
        }
        num /= 32 * Mathf.Pow(radius, 9) * Mathf.PI;

        return -num;
    }

    private void ModifyBuffer<T>(Action<T[], ComputeBuffer> func, BufferType type, T[] data) {
        switch (type)
        {
            case BufferType.Velocities:
                func(data, velocitiesBuffer);
                break;
            case BufferType.Positions:
                func(data, positionsBuffer);
                break;
            case BufferType.Transforms:
                func(data, transformsBuffer);
                break;
            case BufferType.Densities:
                func(data, densitiesBuffer);
                break;
        }
    }

    public ComputeBuffer GetRawBuffer(BufferType type) {
        return type switch
        {
            BufferType.Velocities => velocitiesBuffer,
            BufferType.Positions => positionsBuffer,
            BufferType.Transforms => transformsBuffer,
            BufferType.Densities => densitiesBuffer,
            _ => null,
        };
    }
    public SoupSimulator() {
        shader = Resources.Load<ComputeShader>("Soup/SoupSimulation");

        for (float p = 1.0f; p > 0; p -= 0.2f) {
            Vector2 p2 = Vector2.up * p;
        }
    }
    public unsafe void SetupSim()
    {
        transformsBuffer = new ComputeBuffer(ParticleCount, sizeof(Matrix4x4));
        positionsBuffer = new ComputeBuffer(ParticleCount, sizeof(Vector4));
        velocitiesBuffer = new ComputeBuffer(ParticleCount, sizeof(Vector4));
        densitiesBuffer = new ComputeBuffer(ParticleCount, sizeof(float));

        shader.SetBuffer(SoupKernelIndex, TBufIndex, transformsBuffer);
        shader.SetBuffer(SoupKernelIndex, PosBufIndex, positionsBuffer);
        shader.SetBuffer(SoupKernelIndex, VelBufIndex, velocitiesBuffer);
        shader.SetBuffer(SoupKernelIndex, DenBufIndex, densitiesBuffer);

        // Density kernel bindings
        shader.SetBuffer(DensityKernelIndex, DenBufIndex, densitiesBuffer);
        shader.SetBuffer(DensityKernelIndex, PosBufIndex, positionsBuffer);




        FluidParticle[] soupParticles = new FluidParticle[ParticleCount];

        const float Gap = 2f;
        int rowSize = (int)Math.Sqrt(ParticleCount);
        float initialOffsetX = SimBounds.x + (SimBounds.width - (rowSize * Gap)) / 2;
        float initialOffsetY = SimBounds.y + (SimBounds.height - (rowSize * Gap)) / 2;

        for (int i = 0; i < soupParticles.Length; i++)
        {

            Vector4 v = new(
                initialOffsetX + Gap * (i % rowSize),
                initialOffsetY + Gap * (i / rowSize),
                0,
                1
            );

            soupParticles[i].position = v;
            //soupParticles[i].velocity = VectorUtils.Rand(-12f, 12f, -5f, 5f, 0, 0);
        }

        WriteToGPU(soupParticles);
        DispatchShaderKernel(DensityKernelIndex);
    }

    public void Release() {
        transformsBuffer?.Release();        
        positionsBuffer?.Release();        
        velocitiesBuffer?.Release();
        densitiesBuffer?.Release();  
    }

    public void DispatchShaderKernel(string kernelName)
    {
        DispatchShaderKernel(shader.FindKernel(kernelName));
    }

    public void DispatchShaderKernel(int kernelIndex)
    {
        shader.Dispatch(kernelIndex, ParticleCount / 64 + 1, 1, 1);
        if (kernelIndex == shader.FindKernel("SoupDensityKernel")) {
            float[] densitiesTest = new float[ParticleCount];
            densitiesBuffer.GetData(densitiesTest);

            int i = 0;
            foreach (float shit in densitiesTest) {
                if (shit == 0) {
                    Debug.Log($"FUCK DENSITY {i} IS 0");
                } else if (shit == float.NaN) {
                    Debug.Log($"FUCK density {i} is NAN");
                } 
                
                i++;
            }
        }
        // if (kernelIndex == shader.FindKernel("SoupKernel")) {
        //     Vector4[] densitiesTest = new Vector4[ParticleCount];
        //     velocitiesBuffer.GetData(densitiesTest);

        //     int i = 0;
        //     foreach (var shit in densitiesTest) {
        //         Debug.Log($"Velocity {i}: {shit}");
        //         i++;
        //     }
        // }
    }



    void WriteToGPU(FluidParticle[] particles)
    {
        Vector4[] positionStaging = new Vector4[particles.Length];
        Vector4[] velocityStaging = new Vector4[particles.Length];

        for (int i = 0; i < particles.Length; i++)
        {
            positionStaging[i] = particles[i].position;
            velocityStaging[i] = particles[i].velocity;
        }

        positionsBuffer.SetData(positionStaging, 0, 0, particles.Length);
        velocitiesBuffer.SetData(velocityStaging, 0, 0, particles.Length);

        Vector4[] positionStaging2 = new Vector4[particles.Length];
        positionsBuffer.GetData(positionStaging2);
    }

    public struct FluidParticle
    {
        public Vector4 position;
        public Vector4 velocity;
    }

    public enum BufferType
    {
        Velocities,
        Positions,
        Transforms,
        Densities
    }

}