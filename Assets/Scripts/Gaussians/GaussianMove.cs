using System.Collections.Generic;
using UnityEngine;
using ThreeDeeBear.Models.Ply;
using Unity.VisualScripting;
using System.Data.Common;


public class GaussianMove : MonoBehaviour
{
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    PlyResult plyResult;
    PlyResult originalData;
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

    [SerializeField] ColliderSetup colliderSetup;

    [SerializeField] string ply;

    GaussianSorter_Extern sortGaussians;
    [SerializeField] ComputeShader sortingShader;
    [SerializeField] ComputeShader depthShader;

    public void SetPath(string newPath)
    {
        ply = newPath;
    }
    public void Init()
    {
        sortGaussians = new GaussianSorter_Extern();
        Camera.main.depthTextureMode = DepthTextureMode.None;
        
        transformList = new List<Matrix4x4>();

        material = new Material(material);
        plyResult = PlyHandler.GetVerticesAndTriangles(ply);
        originalData = new PlyResult(new List<float>(plyResult.data), plyResult.sh_dim);
        vertexSize = 14 + plyResult.sh_dim;
        vertexCount = plyResult.data.Count / vertexSize;

        vertexOrder = new uint[vertexCount];
        sortGaussians.Init(plyResult.data, vertexCount, vertexSize, depthShader, sortingShader);
        SortGaussians();

        SetupUniforms();
        CalculateBounds();
    
        GL.sRGBWrite = false;
    }
    

    void UpdatePositions()
    {
        for (int i = 0; i < plyResult.data.Count; i += vertexSize)
        {
            Vector4 p = new Vector4(originalData.data[i], originalData.data[i + 1], originalData.data[i + 2], 1.0f);
            p = transform.localToWorldMatrix * p;
            plyResult.data[i] = p.x;
            plyResult.data[i + 1] = p.y;
            plyResult.data[i + 2] = p.z;
        }
      
    }

    void UpdateUniforms()
    {

        b2.SetData(vertexOrder);
        material.SetBuffer("gaussians_order", b2);

        

        Vector3 camPos = Camera.main.transform.position; //Esto si quiero mas colores

        material.SetVector("cam_pos", camPos);
        material.SetMatrix("modelPos", transform.localToWorldMatrix);
        
    }
    void SetupUniforms()
    {
                
        Camera cam = Camera.main;

        //Matrix4x4 proj = cam.GetStereoProjectionMatrix(Camera.StereoscopicEye.Left);

        float htany = 0;
        float htanx = 0;
        float focalz = 0;
        Vector3 hfov_focal;

        //Calculate hfov_focal depending on target
#if UNITY_EDITOR

        htany = Mathf.Tan(Mathf.Deg2Rad * cam.fieldOfView / 2.0f);
        htanx = htany / Screen.height * Screen.width; ;
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
    
   
    void SortGaussians() 
    {
        
        vertexOrder = sortGaussians.Sort(Camera.main, transform.localToWorldMatrix);
        
        
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
        colliderSetup.SetupCollider(max, min);
    }
    
    void Update()
    {
        //UpdatePositions();
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
