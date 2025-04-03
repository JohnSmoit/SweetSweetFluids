using UnityEngine;

public class DensityTest : MonoBehaviour
{
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    RenderTexture rt;
    RenderTexture rt2;
    public GameObject RenderObject;
    public GameObject RenderObject2;
    void Start()
    {
        SoupSimulator sim = new() {
            ParticleCount = 1024,
            SimBounds = new Rect(0, 0, 20, 20)
        };

        sim.SetupSim();

        //randomize particle position
        Vector4[] horkldorklporklforkl = new Vector4[sim.ParticleCount];

        for (int i = 0; i < horkldorklporklforkl.Length; i++) {
            horkldorklporklforkl[i] = VectorUtils.Rand(0, 20, 0, 20, 0, 0);
        }
        sim.SetBufferData(horkldorklporklforkl, SoupSimulator.BufferType.Positions);

        sim.DispatchShaderKernel(sim["SoupDensityKernel"]);

        ComputeBuffer positions = sim.GetRawBuffer(SoupSimulator.BufferType.Positions);
        ComputeBuffer densities = sim.GetRawBuffer(SoupSimulator.BufferType.Densities);

        ComputeShader densityView = Resources.Load<ComputeShader>("Soup/DebugDensity");
        int densityKernel = densityView.FindKernel("DebugDensity");
        int gradientKernel = densityView.FindKernel("DebugGradient");

        rt = new RenderTexture(512, 256, 16, RenderTextureFormat.ARGB32) {
            enableRandomWrite = true
        };
        rt2 = new RenderTexture(100, 50, 16, RenderTextureFormat.ARGB32) {
            enableRandomWrite = true
        };
        
        rt.Create();
        rt2.Create();

        Vector4 stupidBounds = new(
            sim.SimBounds.x,
            sim.SimBounds.y,
            sim.SimBounds.width,
            sim.SimBounds.height
        );

        densityView.SetBuffer(densityKernel, "positions", positions);
        densityView.SetBuffer(densityKernel, "densities", densities);
        densityView.SetTexture(densityKernel, "result", rt);
        densityView.SetVector("bounds", stupidBounds);

        densityView.SetInt("count", sim.ParticleCount);

        densityView.Dispatch(densityKernel, 512 / 4, 256 / 4, 1);

        // set gradient related uniforms
        densityView.SetBuffer(gradientKernel, "positions", positions);
        densityView.SetBuffer(gradientKernel, "densities", densities);
        densityView.SetTexture(gradientKernel, "result", rt2);
        densityView.SetVector("texWidth", new Vector4(100, 50, 0, 0));
        densityView.Dispatch(gradientKernel, 100 / 4, 50 / 4, 1);
    

        SetupRenderObject(sim);

        sim.Release();
    }

    void OnDestroy()
    {
        rt.Release();
        rt2.Release();
    }

    void SetupRenderObject(SoupSimulator sim) {
        MeshRenderer renderer = RenderObject.GetComponent<MeshRenderer>();
        renderer.material.SetTexture("_MainTex", rt);

        renderer = RenderObject2.GetComponent<MeshRenderer>();
        renderer.material.SetTexture("_MainTex", rt2);
    }
}
