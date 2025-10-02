using System;
using System.Collections.Generic;

using UnityEngine;


public class SortGaussians
{
    ComputeShader shader;
    ComputeBuffer inputBuffer;
    ComputeBuffer indexBuffer;
    ComputeBuffer unchangedInputBuffer;
    ComputeBuffer depthBuffer;


    ComputeBuffer tempDepthsBuffer;

    ComputeBuffer prefixSumBuffer;

    ComputeBuffer uintDepthsBuffer_2;
    ComputeBuffer indexesBuffer_2;
    

    int MAX_THREAD_COUNT = 65535;
    int THREADS_PER_BLOCK = 1024;

    int depthKernel;
    int sortKernel;
    int moveKernel;
    int countKernel;


    int vertexCount;
    int vertexSize;

    //Reset the prefix sum every frame
    int[] emptyPrefix;
    public void Init(List<float> data, int vertexCount, int vertexSize, ComputeShader shader)
    {
        this.shader = shader;
        this.vertexCount = vertexCount;
        this.vertexSize = vertexSize;

        inputBuffer = new ComputeBuffer(vertexCount * vertexSize, sizeof(float));
        unchangedInputBuffer = new ComputeBuffer(vertexCount * vertexSize, sizeof(float));
        indexBuffer = new ComputeBuffer(vertexCount, sizeof(int));

        depthBuffer = new ComputeBuffer(vertexCount, sizeof(float));

        tempDepthsBuffer = new ComputeBuffer(vertexCount, sizeof(uint));
        prefixSumBuffer = new ComputeBuffer(16 * 8, sizeof(int));

        uintDepthsBuffer_2 = new ComputeBuffer(vertexCount, sizeof(uint));
        indexesBuffer_2 = new ComputeBuffer(vertexCount, sizeof(int));


        depthKernel = shader.FindKernel("CalculateDepth");
        sortKernel = shader.FindKernel("SortGaussian");
        moveKernel = shader.FindKernel("MoveGaussians");
        countKernel = shader.FindKernel("CountKernel");

        inputBuffer.SetData(data);
        unchangedInputBuffer.SetData(data);

        emptyPrefix = new int[16 * 8];

    }
        
    /*
        Set modelMatrix as identity to not move the gaussians
        
    */
    public int[] Sort(Camera cam, Matrix4x4 modelMatrix)
    {
               
        //Block quantity
        int NUM_BLOCKS = (int)Mathf.Ceil(vertexCount / (float)THREADS_PER_BLOCK);
       
        shader.SetInt("numBlocks", NUM_BLOCKS);
        shader.SetMatrix("viewMat", cam.worldToCameraMatrix);
        shader.SetInt("vertexSize", vertexSize);
        shader.SetInt("vertexCount", vertexCount);

        if (modelMatrix != Matrix4x4.identity)
        {
            //transform positions

            shader.SetMatrix("modelMatrix", modelMatrix);
            shader.SetBuffer(moveKernel, "Input", inputBuffer);
            shader.SetBuffer(moveKernel, "UnchangedInput", unchangedInputBuffer);

            shader.Dispatch(moveKernel, NUM_BLOCKS, 1, 1);

        }
        

        shader.SetBuffer(depthKernel, "Input", inputBuffer);

        shader.SetBuffer(depthKernel, "Indexes", indexBuffer);
        shader.SetBuffer(depthKernel, "Depth", depthBuffer);
        shader.SetBuffer(depthKernel, "uintDepths", tempDepthsBuffer);

        shader.Dispatch(depthKernel, NUM_BLOCKS, 1, 1);


        //Ordenacion

        prefixSumBuffer.SetData(emptyPrefix);
        shader.SetBuffer(countKernel, "Depth", depthBuffer);
        shader.SetBuffer(countKernel, "prefixSum", prefixSumBuffer);


        shader.Dispatch(countKernel, NUM_BLOCKS, 1, 1);

        shader.SetBuffer(sortKernel, "Indexes", indexBuffer);
        shader.SetBuffer(sortKernel, "uintDepths", tempDepthsBuffer);
        shader.SetBuffer(sortKernel, "prefixSum", prefixSumBuffer);
        shader.SetBuffer(sortKernel, "uintDepths_2", uintDepthsBuffer_2);
        shader.SetBuffer(sortKernel, "Indexes_2", indexesBuffer_2);

        bool parity = false;
        for (int i = 0; i < 32; i += 4)
        {
            shader.SetInt("shift", i);
            shader.SetBool("parity", parity);
            shader.Dispatch(sortKernel, NUM_BLOCKS, 1, 1);
            parity = !parity;
        }

        int[] orderedInts = new int[vertexCount];
        indexBuffer.GetData(orderedInts);
        return orderedInts;
    }

    int[] CPURadixSort(float[] depths)
    {
        int[] order = new int[vertexCount];
        uint[] uintDephts = new uint[vertexCount];
        uint[] tempDepthsBuffer = new uint[vertexCount];
        int[] tempOrder = new int[vertexCount];

        //PrepareData
        for (int i = 0; i < vertexCount; i++)
        {
            uint intValue = BitConverter.ToUInt32(BitConverter.GetBytes(depths[i]), 0);
            uint mask = (uint)-(intValue >> 31) | 0x80000000;
            uintDephts[i] = intValue ^ mask;
            tempDepthsBuffer[i] = uintDephts[i];
            order[i] = i;
            tempOrder[i] = i;
        }

        //Radix sort
        for (int i = 0; i < 32; i += 4)
        {
            //16 -> HEXADECIMAL
            int[] hist = new int[16];//Resetear cada bucle

            for (int j = 0; j < vertexCount; j++)
            {
                uint currentValue = (uintDephts[j] >> i) & 0x000F;
                hist[currentValue]++;
            }
            //Inclusive scan hist
            for (int j = 1; j < 16; j++)
            {
                hist[j] += hist[j - 1];
                
            }

            for (int j = vertexCount-1; j >= 0; j--)
            {
                tempDepthsBuffer[hist[(uintDephts[j] >> i) & 0x000F] - 1] = uintDephts[j];
                tempOrder[hist[(uintDephts[j] >> i) & 0x000F] - 1] = order[j];
                hist[(uintDephts[j] >> i) & 0x000F]--;
            }

            for (int j = 0; j < vertexCount; j++)
            {
                uintDephts[j] = tempDepthsBuffer[j];
                order[j] = tempOrder[j];
            }
        }
        
        return order;
    }


    public void Clean()
    {
        if (inputBuffer != null)
        {
            inputBuffer.Release();
            inputBuffer = null;
        }
        if (indexBuffer != null)
        {
            indexBuffer.Release();
            indexBuffer = null;
        }
        if (depthBuffer != null)
        {
            depthBuffer.Release();
            depthBuffer = null;
        }
        if (unchangedInputBuffer != null)
        {
            unchangedInputBuffer.Release();
            unchangedInputBuffer = null;
        }
        if (tempDepthsBuffer != null)
        {
            tempDepthsBuffer.Release();
            tempDepthsBuffer = null;
        }
        if (prefixSumBuffer != null)
        {
            prefixSumBuffer.Release();
            prefixSumBuffer = null;
        }
        if (uintDepthsBuffer_2 != null)
        {
            uintDepthsBuffer_2.Release();
            uintDepthsBuffer_2 = null;
        }
        if (indexesBuffer_2 != null)
        {
            indexesBuffer_2.Release();
            indexesBuffer_2 = null;
        }
        

    }
}
