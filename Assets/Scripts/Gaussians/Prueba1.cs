using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using ThreeDeeBear.Models.Ply;
using UnityEngine.UIElements;
using System.Linq;

public class Prueba1 : MonoBehaviour
{
    
    PlyResult plyResult;
    List<int> vertexOrder;
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

    void Start()
    {

        Camera.main.depthTextureMode = DepthTextureMode.None;
        vertexOrder = new List<int>();
        transformList = new List<Matrix4x4>();
        //C:/Users/111370/Documents/Plys/f121d198-0/point_cloud/iteration_15000/point_cloud.ply
        //C:/Users/111370/Documents/Plys/person/person2.ply
        //C:/Users/111370/Documents/Plys/75989198-0 2/75989198-0/point_cloud/iteration_7000/pc2.ply
        //C:/Users/111370/Documents/Plys/588c70b9-5/588c70b9-5/point_cloud/iteration_30000/point_cloud.ply
        plyResult = PlyHandler.GetVerticesAndTriangles("C:/Users/Xhaim/Desktop/TFM/Plys/f121d198-0/point_cloud/iteration_15000/point_cloud.ply");
        vertexSize = 14 + plyResult.sh_dim;
        vertexCount = plyResult.data.Count / vertexSize;
       
       
        SortGaussians();

        SetupUniforms();
        CalculateBounds();
        GL.sRGBWrite = false;
    }



    void UpdateUniforms()
    {

        b2.SetData(vertexOrder.ToArray());
        material.SetBuffer("gaussians_order", b2);

        Vector3 camPos = Camera.main.transform.position; //Esto si quiero mas colores
        Matrix4x4 viewMat = Camera.main.worldToCameraMatrix;
    
        material.SetVector("cam_pos", camPos);
        material.SetMatrix("view", viewMat);
        

        
    }
    void SetupUniforms()
    {
                
        Camera cam = Camera.main;
        float htany = Mathf.Tan(Mathf.Deg2Rad * cam.fieldOfView / 2.0f);
        float htanx = htany / Screen.height*Screen.width;
        float focalz = Screen.height / (2.0f * htany);
        Vector3 hfov_focal = new Vector3(htanx, htany, focalz);
 
        //Setup uniforms
        material.SetVector("hfov_focal", hfov_focal);
        material.SetInt("SH_DIM", plyResult.sh_dim+3);

        Matrix4x4 projection = cam.projectionMatrix;
        material.SetMatrix("projection", projection);
        
        
        SetupDataBuffer();
        UpdateUniforms();
    }
    
    void SetupDataBuffer()
    {
        //Vertex count
        int stride2 = System.Runtime.InteropServices.Marshal.SizeOf(typeof(int));
        b2 = new ComputeBuffer(vertexOrder.Count, stride2, ComputeBufferType.Default);

        //Vertex data
        int stride = System.Runtime.InteropServices.Marshal.SizeOf(typeof(float)); 
        data = new ComputeBuffer(plyResult.data.Count, stride, ComputeBufferType.Default);
        data.SetData(plyResult.data.ToArray());
        material.SetBuffer("data", data);

        //args data
        args[0] = (uint)mesh.GetIndexCount(0);
        args[1] = (uint)vertexOrder.Count;
        args[2] = (uint)mesh.GetIndexStart(0);
        args[3] = (uint)mesh.GetBaseVertex(0);
        argsBuffer = new ComputeBuffer(1, args.Length*sizeof(uint), ComputeBufferType.IndirectArguments);
        argsBuffer.SetData(args);
    }
    
    List<(float, int)> depthIndex = new List<(float, int)>();
    void SortGaussians() //Bajon  de fps
    {
        depthIndex.Clear();
        vertexOrder.Clear();
       
        Camera cam = Camera.main;
        Matrix4x4 viewMat = cam.worldToCameraMatrix;

        int count = 0;
        for (int i = 0; i < plyResult.data.Count; i += vertexSize)
        {
            Vector4 xyz = new Vector4(plyResult.data[i], plyResult.data[i+1], plyResult.data[i+2], 1);
            Vector4 xyzView = viewMat * xyz;

            float depth = xyzView.z;

            depthIndex.Add((depth, count));
            ++count;
        }

        depthIndex.Sort((x, y) => x.Item1.CompareTo(y.Item1)); //Orden correcto

        foreach ((float, int) pair in depthIndex)
        {
            vertexOrder.Add(pair.Item2);
        }
        
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
        vertexOrder.Clear();
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
        
    }
}
