using System;
using System.Data;
using System.Runtime.InteropServices;
using System.Text;
using Unity.VisualScripting;
using UnityEditor;
using UnityEngine;

public class ComputeDriver : MonoBehaviour
{
    private SoupSimulator simulator;
    
    private float _accum;
    private int SoupKernelIndex;
    private int DensityKernelIndex;

    public bool DoSingleSteps = false;

    public Rect SimBounds;

    Matrix4x4[] transforms;
    Vector4[] debugVelocities;
    public Mesh ParticleMesh;
    public Material ParticleMaterial;
    void Start()
    {
        simulator = new SoupSimulator
        {
            // gotta figure out how to expose properties to the inspector
            ParticleCount = 1024,
            DT = 1f / 60f,
            Gravity = -9.8f,
            SimBounds = SimBounds,
        };


        SoupKernelIndex = simulator["SoupKernel"];
        DensityKernelIndex = simulator["SoupDensityKernel"];

        transforms = new Matrix4x4[simulator.ParticleCount];
        debugVelocities = new Vector4[simulator.ParticleCount];


        simulator.SetupSim();  

    }


    void OnDestroy()
    {
        simulator.Release();
    }

    public void SingleStep() {
        simulator.DispatchShaderKernel(DensityKernelIndex);
        simulator.DispatchShaderKernel(SoupKernelIndex);
    }

    void Update()
    {
        if (!DoSingleSteps) {
            _accum += Time.deltaTime;


            while (_accum > simulator.DT) {
                simulator.DispatchShaderKernel(DensityKernelIndex);
                simulator.DispatchShaderKernel(SoupKernelIndex);
                _accum -= simulator.DT;
            }
        }
        
        simulator.GetBufferData(transforms, SoupSimulator.BufferType.Transforms);
        simulator.GetBufferData(debugVelocities, SoupSimulator.BufferType.Velocities);

        MaterialPropertyBlock instanceData = new();

        instanceData.SetVectorArray("_Velocity", debugVelocities);

        Graphics.DrawMeshInstanced(ParticleMesh, 0, ParticleMaterial, transforms, simulator.ParticleCount, instanceData);

        
        // TODO: When I need scalability, use a draw call that doesn't require as many
        // GPU-CPU-GPU transfers such as the call below
        // Graphics.RenderMeshIndirect(rp, )
    }

}
