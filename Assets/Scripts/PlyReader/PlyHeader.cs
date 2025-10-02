using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
namespace ThreeDeeBear.Models.Ply
{
    public enum PlyFaceParseMode
    {
        VertexCountVertexIndex // todo: see if other modes exist
    }

    public enum PlyFormat
    {
        Ascii,
        BinaryBigEndian,
        BinaryLittleEndian,
        Unknown
    }

    //Esto lee el header
    public class PlyHeader
    {
        public PlyFormat Format;
        public int VertexCount; //Propiedades del esto, cambiarlo a x, y, z, opacity, scale_0, scale_1, scale_2, rot_0, rot_1, rot_2, rot_3, f_dc_0, f_dc_1, f_dc_2 (De momento solo color base)
        public int x; //Sus posiciones dentro de la estructura
        public int y;
        public int z;
        public int opacity;
        public int scale_0;
        public int scale_1;
        public int scale_2;
        public int rot_0;
        public int rot_1;
        public int rot_2;
        public int rot_3;
        public int f_dc_0;
        public int f_dc_1;
        public int f_dc_2;

        public int f_rest_count = 0;
        public int f_rest_0;
        public PlyFaceParseMode FaceParseMode;
        public List<string> RawHeader;

        public int propertyCount = 0;
        public PlyHeader()
        {

        }

        public PlyHeader(List<string> headerUnparsed)
        {
            Format = GetFormat(headerUnparsed.FirstOrDefault(x => x.Contains("format")).Split(' ')[1]);
            var elementVertexIndex = headerUnparsed.IndexOf(headerUnparsed.FirstOrDefault(x => x.Contains("element vertex")));
            VertexCount = Convert.ToInt32(headerUnparsed[elementVertexIndex].Split(' ')[2]);
            //var elementFaceIndex = headerUnparsed.IndexOf(headerUnparsed.FirstOrDefault(x => x.Contains("element face"))); //No tenemos faces
            //FaceCount = Convert.ToInt32(headerUnparsed[elementFaceIndex].Split(' ')[2]);
            SetVertexProperties(GetProperties(headerUnparsed, elementVertexIndex));
            //SetFaceProperties(GetProperties(headerUnparsed, elementFaceIndex)); //No hay faces
            RawHeader = headerUnparsed;
        }

		private PlyFormat GetFormat(string formatLine)
        {
            switch (formatLine)
            {
                case "binary_little_endian":
                    return PlyFormat.BinaryLittleEndian;
                case "binary_big_endian":
                    return PlyFormat.BinaryBigEndian;
                case "ascii":
                    return PlyFormat.Ascii;
                default:
                    return PlyFormat.Unknown;
            }
        }

		private void SetVertexProperties(IList<string> properties)
        {
            for (int i = 0; i < properties.Count; i++)
            {
                var split = properties[i].Split(' ');
                var propertyType = split.Last();
                switch (propertyType)
                {
                    //Propiedades del esto, cambiarlo a x, y, z, opacity, scale_0, scale_1, scale_2, rot_0, rot_1, rot_2, rot_3, f_dc_0, f_dc_1, f_dc_2 (De momento solo color base)
                    case "x": //Cambiar las properties
                        x = i;
                        break;
                    case "y":
                        y = i;
                        break;
                    case "z":
                        z = i;
                        break;
                    case "opacity":
                        opacity = i;
                        break;
                    case "scale_0":
                        scale_0 = i;
                        break;
                    case "scale_1":
                        scale_1 = i;
                        break;
                    case "scale_2":
                        scale_2 = i;
                        break;
                    case "rot_0":
                        rot_0 = i;
                        break;
                    case "rot_1":
                        rot_1 = i;
                        break;
                    case "rot_2":
                        rot_2 = i;
                        break;
                    case "rot_3":
                        rot_3 = i;
                        break;
                    case "f_dc_0":
                        f_dc_0 = i;
                        break;
                    case "f_dc_1":
                        f_dc_1 = i;
                        break;
                    case "f_dc_2":
                        f_dc_2 = i;
                        break;
                    default:
                        if (propertyType.Contains("f_rest"))
                        {
                            f_rest_count++;
                            if (propertyType == "f_rest_0")
                            {
                                f_rest_0 = i;
                            }
                        }
                        break;
                }
            }
        }


        private List<string> GetProperties(IList<string> header, int elementIndex)
        {
            var properties = new List<string>();
            for (int i = elementIndex + 1; i < header.Count; i++)
            {
                var property = header[i];
                if (property.Contains("property"))
                {
                    properties.Add(property);
                }
                else
                {
                    break;
                }
            }
            propertyCount = properties.Count;
            return properties;
        }


    }
}