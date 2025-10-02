using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using ThreeDeeBear.Models.Ply;
using UnityEngine.UIElements;
using System.Linq;
using Unity.Mathematics;

public class GaussianWorld : MonoBehaviour
{
    
    PlyResult plyResult;
    uint[] vertexOrder;
    List<Matrix4x4> transformList;

    ComputeBuffer b2;
    ComputeBuffer data;
    private ComputeBuffer argsBuffer;
    uint[] args = { 0,0,0,0,0 };
    [SerializeField] Mesh mesh;
    [SerializeField] Material material;
    int vertexSize = 0;
    int vertexCount = 0;
    Bounds bounds;

    //Sort Order
    GaussianSorter_Extern sortGaussians;
    [SerializeField] ComputeShader sortingShader;
    [SerializeField] ComputeShader depthShader;
    

    [SerializeField] bool debug;
    [SerializeField] string[] paths;


    void Start()
    {

        Camera.main.depthTextureMode = DepthTextureMode.None;

        transformList = new List<Matrix4x4>();
        string path = "";
        if (debug)
        {
            path = paths[0];
        }
        else
        {
            path = GameObject.FindAnyObjectByType<SceneInfo>().GetFilePath();
        }

        plyResult = PlyHandler.GetVerticesAndTriangles(path);
        vertexSize = 14 + plyResult.sh_dim;
        vertexCount = plyResult.data.Count / vertexSize;
        vertexOrder = new uint[vertexCount];
        sortGaussians = new GaussianSorter_Extern();
        sortGaussians.Init(new List<float>(plyResult.data), vertexCount, vertexSize, depthShader, sortingShader);
        SortGaussians();

        SetupUniforms();
        CalculateBounds();
        GL.sRGBWrite = false;
        
    }



    void UpdateUniforms()
    {

        b2.SetData(vertexOrder);
        material.SetBuffer("gaussians_order", b2);

        Vector3 camPos = Camera.main.transform.position; //Esto si quiero mas colores

        material.SetVector("cam_pos", camPos);

        
    }
    void SetupUniforms()
    {
                
        Camera cam = Camera.main;
        float htany = 0;
        float htanx = 0;
        float focalz = 0;
        Vector3 hfov_focal;

    //Calculate hfov_focal depending on target
        #if  UNITY_EDITOR

             htany = Mathf.Tan(Mathf.Deg2Rad * cam.fieldOfView / 2.0f);
             htanx =  htany / Screen.height*Screen.width;; 
             focalz = Screen.height / (2.0f * htany);
             hfov_focal = new Vector3(htanx, htany, focalz);
        #elif UNITY_ANDROID || UNITY_STANDALONE_WIN
            Matrix4x4 proj = cam.GetStereoProjectionMatrix(Camera.StereoscopicEye.Left);
            htany = 1.0f / proj[0, 0];//Mathf.Tan(Mathf.Deg2Rad * cam.fieldOfView / 2.0f);
             htanx =   1.0f / proj[1, 1];//htany / Screen.height*Screen.width;; 
             focalz = Screen.height / (2.0f * htany);
             hfov_focal = new Vector3(htanx, htany, focalz);
#endif

        //Setup uniforms
        material.SetVector("hfov_focal", hfov_focal);
        material.SetInt("SH_DIM", plyResult.sh_dim+3);

        //material.SetMatrix("projectionMat", GL.GetGPUProjectionMatrix(Camera.main.projectionMatrix, false));
        SetupDataBuffer();
        UpdateUniforms();
    }
    
    void SetupDataBuffer()
    {
        //Vertex count
        int stride2 = System.Runtime.InteropServices.Marshal.SizeOf(typeof(int));
        b2 = new ComputeBuffer(vertexCount, stride2, ComputeBufferType.Default);

        //Vertex data
        int stride = System.Runtime.InteropServices.Marshal.SizeOf(typeof(float)); 
        data = new ComputeBuffer(plyResult.data.Count, stride, ComputeBufferType.Default);
        data.SetData(plyResult.data.ToArray());
        material.SetBuffer("data", data);

        //args data
        args[0] = (uint)mesh.GetIndexCount(0);
        args[1] = (uint)vertexCount;
        args[2] = (uint)mesh.GetIndexStart(0);
        args[3] = (uint)mesh.GetBaseVertex(0);
        argsBuffer = new ComputeBuffer(1, args.Length*sizeof(uint), ComputeBufferType.IndirectArguments);
        argsBuffer.SetData(args);
    }


    // List<uint> v = new List<uint>();
    // List<(float,int)> depthIndex = new List<(float,int)>();
    void SortGaussians() 
    {


        vertexOrder = sortGaussians.Sort(Camera.main, transform.localToWorldMatrix);
        // depthIndex.Clear();
        // v.Clear();

        // Camera cam = Camera.main;
        // Matrix4x4 viewMat = cam.worldToCameraMatrix;

        // int count = 0;
        // for (int i = 0; i < plyResult.data.Count; i += vertexSize)
        // {
        //     Vector4 xyz = new Vector4(plyResult.data[i], plyResult.data[i + 1], plyResult.data[i + 2], 1);
        //     Vector4 xyzView = viewMat * xyz;

        //     float depth = xyzView.z;

        //     depthIndex.Add((depth, count));
        //     ++count;
        // }

        // depthIndex.Sort((x, y) => x.Item1.CompareTo(y.Item1)); //Orden correcto

        // foreach ((float, uint) pair in depthIndex)
        // {
        //     v.Add(pair.Item2);
        // }
        // vertexOrder = v.ToArray();

    }


    void CalculateBounds()
    {
        Vector3 min = Vector3.positiveInfinity;
        Vector3 max = Vector3.negativeInfinity;

        for (int i = 0; i < plyResult.data.Count; i += vertexSize)
        {
            Vector3 p = new Vector3(plyResult.data[i], plyResult.data[i + 1], plyResult.data[i + 2]);
            min = Vector3.Min(min, p);
            max = Vector3.Max(max, p);
        }

        Vector3 center = (min + max) * 0.5f;
        Vector3 size = max - min + Vector3.one * 10f; // margen extra
        bounds = new Bounds(center, size);
    }
    

    void Update()
    {
        
        SortGaussians();
        UpdateUniforms();        

        Graphics.DrawMeshInstancedIndirect(mesh, 0, material, bounds, argsBuffer); 

    }

    void OnDestroy()
    {

        transformList.Clear();
        if (b2 != null)
        {
            b2.Release();
            b2 = null;
        }

        if (data != null)
        {
            data.Release();
            data = null;
        }

        if (argsBuffer != null)
        {
            argsBuffer.Release();
            argsBuffer = null;
        }
        sortGaussians.Clean();
        
    }
}
