using UnityEngine;

public class DensityTest : MonoBehaviour
{
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    RenderTexture rt;
    public GameObject RenderObject;
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

        rt = new RenderTexture(512, 256, 16, RenderTextureFormat.ARGB32) {
            enableRandomWrite = true
        };
        rt.Create();

        densityView.SetBuffer(densityKernel, "positions", positions);
        densityView.SetBuffer(densityKernel, "densities", densities);
        densityView.SetTexture(densityKernel, "result", rt);
        densityView.SetVector("bounds", new Vector4(
            sim.SimBounds.x,
            sim.SimBounds.y,
            sim.SimBounds.width,
            sim.SimBounds.height
        ));

        densityView.SetInt("count", sim.ParticleCount);

        densityView.Dispatch(densityKernel, 512 / 4, 256 / 4, 1);

        SetupRenderObject(sim);

        sim.Release();
    }

    void OnDestroy()
    {
        rt.Release();
    }

    void SetupRenderObject(SoupSimulator sim) {
        MeshRenderer renderer = RenderObject.GetComponent<MeshRenderer>();

        renderer.sharedMaterial.SetTexture("_MainTex", rt);
    }
}
