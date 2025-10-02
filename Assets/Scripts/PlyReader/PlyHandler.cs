using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using System.Linq;
using System;
using System.Globalization;
using System.Text;

namespace ThreeDeeBear.Models.Ply
{
    public class PlyResult
    {
        public List<float> data;
        public int sh_dim;

        public PlyResult(List<float> vertices, int f_rest_count)
        {
            data = vertices;
            sh_dim = f_rest_count;
        }
    }
    public static class PlyHandler
    {

        #region Binary

        private static PlyResult ParseBinaryLittleEndian(string path, PlyHeader header)
        {
            var headerAsText = header.RawHeader.Aggregate((a, b) => $"{a}\n{b}") + "\n";
            var headerAsBytes = Encoding.ASCII.GetBytes(headerAsText);
            var withoutHeader = File.ReadAllBytes(path).Skip(headerAsBytes.Length).ToArray();
            var vertices = GetVertices(withoutHeader, header);
            
            return new PlyResult(vertices, header.f_rest_count);
        }

        private static List<float> GetVertices(byte[] bytes, PlyHeader header)
        {
            var vertices = new List<float>();

            int bpf = 4; // bytes per float
            // todo: support other types than just float for vertex components and byte for color components
            int bytesPerVertex = GetByteCountPerVertex(header); //Un dato entero

            for (int i = 0; i < header.VertexCount; i++)
            {
                int byteIndex = i * bytesPerVertex;

                if (byteIndex + bytesPerVertex > bytes.Length)
                    break;

                float x = BitConverter.ToSingle(bytes, byteIndex + header.x * bpf);
                float y = BitConverter.ToSingle(bytes, byteIndex + header.y * bpf);
                float z = BitConverter.ToSingle(bytes, byteIndex + header.z * bpf);

                float scale_0 = BitConverter.ToSingle(bytes, byteIndex + header.scale_0 * bpf);
                float scale_1 = BitConverter.ToSingle(bytes, byteIndex + header.scale_1 * bpf);
                float scale_2 = BitConverter.ToSingle(bytes, byteIndex + header.scale_2 * bpf);

                float rot_0 = BitConverter.ToSingle(bytes, byteIndex + header.rot_0 * bpf);
                float rot_1 = BitConverter.ToSingle(bytes, byteIndex + header.rot_1 * bpf);
                float rot_2 = BitConverter.ToSingle(bytes, byteIndex + header.rot_2 * bpf);
                float rot_3 = BitConverter.ToSingle(bytes, byteIndex + header.rot_3 * bpf);

                float f_dc_0 = BitConverter.ToSingle(bytes, byteIndex + header.f_dc_0 * bpf);
                float f_dc_1 = BitConverter.ToSingle(bytes, byteIndex + header.f_dc_1 * bpf);
                float f_dc_2 = BitConverter.ToSingle(bytes, byteIndex + header.f_dc_2 * bpf);
                float opacity = BitConverter.ToSingle(bytes, byteIndex + header.opacity * bpf);

                List<float> f_rests = new List<float>();
                for (int j = 0; j < header.f_rest_count; j++)
                {
                    f_rests.Add(BitConverter.ToSingle(bytes, byteIndex + (header.f_rest_0 + j) * bpf));
                }
                Quaternion normRotation = new Quaternion(rot_0, rot_1, rot_2, rot_3);
          
                normRotation = Quaternion.Normalize(normRotation);
               
                opacity = 1.0f / (1.0f + Mathf.Exp(-opacity)); //sigmoid

                float[] data = { x, -y, z, normRotation.x, -normRotation.y, normRotation.z, -normRotation.w, Mathf.Exp(scale_0), Mathf.Exp(scale_1), Mathf.Exp(scale_2), opacity, f_dc_0, f_dc_1, f_dc_2 };

                vertices.AddRange(data);
                vertices.AddRange(f_rests.ToArray());
            }
            return vertices;
        }

        private static int GetByteCountPerVertex(PlyHeader header)
        {
            int bpf = 4; //bytes per float
            
            return bpf*header.propertyCount;
        }
        #endregion


        public static PlyResult GetVerticesAndTriangles(string path)
        {
            List<string> header = File.ReadLines(path).TakeUntilIncluding(x => x == "end_header").ToList();
            var headerParsed = new PlyHeader(header);
            if (headerParsed.Format == PlyFormat.Ascii)
            {
                //return ParseAscii(File.ReadAllLines(path).ToList(), headerParsed);
                return null;
            }
            else if (headerParsed.Format == PlyFormat.BinaryLittleEndian)
            {
                return ParseBinaryLittleEndian(path, headerParsed);
            }
            else // todo: support BinaryBigEndian
            {
                return null;
            }
        }

    }
}