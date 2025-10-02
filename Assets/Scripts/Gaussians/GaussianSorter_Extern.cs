using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.XR.Interaction.Toolkit.AR;

public class GaussianSorter_Extern
{
    /*
        DepthShader
    */
    ComputeShader depthShader;
    ComputeShader orderShader;
    
    ComputeBuffer depthBuffer;

    ComputeBuffer indexBuffer;
    ComputeBuffer inputBuffer;

    int THREADS_PER_BLOCK = 1024;

    int depthKernel = -1;


    int vertexCount;
    int vertexSize;

    //DeviceRadixSort
    // https://github.com/b0nes164/GPUSorting
    const uint DEVICE_RADIX_SORT_PARTITION_SIZE = 3840;
    const uint DEVICE_RADIX_SORT_BITS = 8;
    const uint DEVICE_RADIX_SORT_RADIX = 256;
    const uint DEVICE_RADIX_SORT_PASSES = 4;

    //Keywords
    LocalKeyword keyUintKeyword;
    LocalKeyword payloadUintKeyword;
    LocalKeyword ascendKeyword;
    LocalKeyword sortPairKeyword;
    LocalKeyword vulkanKeyword;

    //kernels
    int initDeviceRadixSortKernel = -1;
    int upsweepKernel = -1;
    int scanKernel = -1;
    int downsweepKernel = -1;

    CommandBuffer cmd;
    //Buffers
    Args args;

    //data arrays
    uint[] depths;
    uint[] indexes;
    public struct Args
    {
        public uint count;
        public GraphicsBuffer inputKeys; //indexes creo
        public GraphicsBuffer inputValues; //Profundidades

        public SupportResources resources;
        internal int workGroupCount;
    }

    static uint DivRoundUp(uint x, uint y) => (x + y - 1) / y;
    public struct SupportResources
    {
        public GraphicsBuffer altBuffer;
        public GraphicsBuffer altPayloadBuffer;
        public GraphicsBuffer passHistBuffer;
        public GraphicsBuffer globalHistBuffer;

        public static SupportResources Load(uint count)
        {
            uint scratchBufferSize = DivRoundUp(count, DEVICE_RADIX_SORT_PARTITION_SIZE) * DEVICE_RADIX_SORT_RADIX;
            uint reducedScratchBufferSize = DEVICE_RADIX_SORT_RADIX * DEVICE_RADIX_SORT_PASSES;

            var target = GraphicsBuffer.Target.Structured;
            var resources = new SupportResources
            {
                altBuffer = new GraphicsBuffer(target, (int)count, 4) { name = "DeviceRadixAlt" },
                altPayloadBuffer = new GraphicsBuffer(target, (int)count, 4) { name = "DeviceRadixAltPayload" },
                passHistBuffer = new GraphicsBuffer(target, (int)scratchBufferSize, 4) { name = "DeviceRadixPassHistogram" },
                globalHistBuffer = new GraphicsBuffer(target, (int)reducedScratchBufferSize, 4) { name = "DeviceRadixGlobalHistogram" },
            };
            return resources;
        }

        public void Dispose()
        {
            altBuffer?.Dispose();
            altPayloadBuffer?.Dispose();
            passHistBuffer?.Dispose();
            globalHistBuffer?.Dispose();

            altBuffer = null;
            altPayloadBuffer = null;
            passHistBuffer = null;
            globalHistBuffer = null;
        }
    }

    public void Init(List<float> data, int vertexCount, int vertexSize, ComputeShader depthShader, ComputeShader sortShader)
    {
        this.depthShader = depthShader;
        this.orderShader = sortShader;

        this.vertexCount = vertexCount;
        this.vertexSize = vertexSize;
        //Prepare data shader
        depthBuffer = new ComputeBuffer(vertexCount, sizeof(uint));
        inputBuffer = new ComputeBuffer(vertexCount * vertexSize, sizeof(float));
        indexBuffer = new ComputeBuffer(vertexCount, sizeof(uint));


        depthKernel = depthShader.FindKernel("CalculateDepth");


        inputBuffer.SetData(data);

        depthShader.SetInt("vertexCount", vertexCount);
        depthShader.SetInt("vertexSize", vertexSize);
   
        //DeviceRadixSort initialization
        initDeviceRadixSortKernel = orderShader.FindKernel("InitDeviceRadixSort");
        upsweepKernel = orderShader.FindKernel("Upsweep");
        scanKernel = orderShader.FindKernel("Scan");
        downsweepKernel = orderShader.FindKernel("Downsweep");

        keyUintKeyword = new LocalKeyword(orderShader, "KEY_UINT");
        payloadUintKeyword = new LocalKeyword(orderShader, "PAYLOAD_UINT");
        ascendKeyword = new LocalKeyword(orderShader, "SHOULD_ASCEND");
        sortPairKeyword = new LocalKeyword(orderShader, "SORT_PAIRS");
        vulkanKeyword = new LocalKeyword(orderShader, "VULKAN");

        orderShader.EnableKeyword(keyUintKeyword);
        orderShader.EnableKeyword(payloadUintKeyword);
        orderShader.EnableKeyword(ascendKeyword);
        orderShader.EnableKeyword(sortPairKeyword);
        if (SystemInfo.graphicsDeviceType == UnityEngine.Rendering.GraphicsDeviceType.Vulkan)
        {
            orderShader.EnableKeyword(vulkanKeyword);
        }
        else
        {
            orderShader.DisableKeyword(vulkanKeyword);
        }

        args = new Args();
        args.count = (uint)vertexCount;
        var target = GraphicsBuffer.Target.Structured;
        args.inputKeys = new GraphicsBuffer(target, (int)vertexCount, 4) { name = "GaussianSplatSortIndices" };
        args.inputValues = new GraphicsBuffer(target, (int)vertexCount, 4) { name = "GaussianSplatSortDistances" };
        args.resources = SupportResources.Load((uint)vertexCount);
        cmd = new CommandBuffer();

        depths = new uint[vertexCount];
        indexes = new uint[vertexCount];
    }

    //Hace efectos raros con algunos angulos de cámara. Mirar pq
    public uint[] Sort(Camera cam, Matrix4x4 modelMatrix)
    {
        cmd.Clear(); //Reset command buffer
    
        int NUM_BLOCKS = (int)Mathf.Ceil(vertexCount / (float)THREADS_PER_BLOCK);
        
        depthShader.SetMatrix("viewMat", cam.worldToCameraMatrix);
        depthShader.SetMatrix("modelMatrix", modelMatrix);

        depthShader.SetBuffer(depthKernel, "depth", depthBuffer);
        depthShader.SetBuffer(depthKernel, "input", inputBuffer);
        depthShader.SetBuffer(depthKernel, "index", indexBuffer);

        depthShader.Dispatch(depthKernel, NUM_BLOCKS, 1, 1);

        depthBuffer.GetData(depths);
        indexBuffer.GetData(indexes);


        NUM_BLOCKS = (int)DivRoundUp(args.count, DEVICE_RADIX_SORT_PARTITION_SIZE);
        //DeviceRadixSort
        //Keys -> profundidades
        //values -> Indices que quiero ordenados 
        args.inputKeys.SetData(depths);
        args.inputValues.SetData(indexes);
        //Datos bien subidos

        GraphicsBuffer srcKeyBuffer = args.inputKeys;
        GraphicsBuffer srcPayloadBuffer = args.inputValues;
        GraphicsBuffer dstKeyBuffer = args.resources.altBuffer;
        GraphicsBuffer dstPayloadBuffer = args.resources.altPayloadBuffer;

        cmd.SetComputeIntParam(orderShader, "e_numKeys", vertexCount);
        cmd.SetComputeIntParam(orderShader, "e_threadBlocks", NUM_BLOCKS);

        //upsweep
        cmd.SetComputeBufferParam(orderShader, upsweepKernel, "b_passHist", args.resources.passHistBuffer);
        cmd.SetComputeBufferParam(orderShader, upsweepKernel, "b_globalHist", args.resources.globalHistBuffer);

        //scan
        cmd.SetComputeBufferParam(orderShader, scanKernel, "b_passHist", args.resources.passHistBuffer);

        //downSweep
        cmd.SetComputeBufferParam(orderShader, downsweepKernel, "b_passHist", args.resources.passHistBuffer);
        cmd.SetComputeBufferParam(orderShader, downsweepKernel, "b_globalHist", args.resources.globalHistBuffer);

        //clear global histogram
        cmd.SetComputeBufferParam(orderShader, initDeviceRadixSortKernel, "b_globalHist", args.resources.globalHistBuffer);
        cmd.DispatchCompute(orderShader, initDeviceRadixSortKernel, 1, 1, 1);

        //for
        for (uint shift = 0; shift < 32; shift += DEVICE_RADIX_SORT_BITS)
        {
            cmd.SetComputeIntParam(orderShader, "e_radixShift", (int)shift);

            //upsweep
            cmd.SetComputeBufferParam(orderShader, upsweepKernel, "b_sort", srcKeyBuffer);
            cmd.DispatchCompute(orderShader, upsweepKernel, NUM_BLOCKS, 1, 1);

            //scan
            cmd.DispatchCompute(orderShader, scanKernel, (int)DEVICE_RADIX_SORT_RADIX, 1, 1);

            //downsweep
            cmd.SetComputeBufferParam(orderShader, downsweepKernel, "b_sort", srcKeyBuffer);
            cmd.SetComputeBufferParam(orderShader, downsweepKernel, "b_sortPayload", srcPayloadBuffer);
            cmd.SetComputeBufferParam(orderShader, downsweepKernel, "b_alt", dstKeyBuffer);
            cmd.SetComputeBufferParam(orderShader, downsweepKernel, "b_altPayload", dstPayloadBuffer);
            cmd.DispatchCompute(orderShader, downsweepKernel, NUM_BLOCKS, 1, 1);


            //swap buffers
            (srcKeyBuffer, dstKeyBuffer) = (dstKeyBuffer, srcKeyBuffer);
            (srcPayloadBuffer, dstPayloadBuffer) = (dstPayloadBuffer, srcPayloadBuffer);
        }

        //Wait bc getData
        Graphics.ExecuteCommandBuffer(cmd);

        srcPayloadBuffer.GetData(indexes);
        //dstKeyBuffer tiene que tener las depths ordenadas
        //dstPayloadBuffer tiene que tener los indices como tal


        return indexes;
    }

    public void Clean()
    {
        depthBuffer?.Release();
        inputBuffer?.Release();
        indexBuffer?.Release();
        
        args.inputKeys?.Release();
        args.inputValues?.Release();
        args.resources.Dispose();

        cmd?.Clear();

        depthBuffer = null;
        inputBuffer = null;
        indexBuffer = null;

        args.inputKeys = null;
        args.inputValues = null;

        cmd = null;
        
    }
}
