Shader "Unlit/GaussianShaderInteractable"
{
    Properties
    {
        _UniformScale ("Uniform Scale", float) = 1
        _Frustrum ("Frustrum test", float) = 1.3
        _RenderMode ("Render Mode", int) = 0
        _ColorCorrection("Color Correction", float) = 0.5

    }
    SubShader
    {
        Tags { "RenderType"="Transparent" "Queue"="Transparent" "IgnoreProjector"="True" }
        ZWrite On
        Blend SrcAlpha OneMinusSrcAlpha
        Cull Off
        ZTest LEqual //Always sino
        LOD 100

        Pass
        {
            CGPROGRAM

            #pragma target 4.6
            #pragma vertex vert
            #pragma fragment frag
          
            #pragma multi_compile_instancing
            //#pragma multi_compile _ _STEREO_INSTANCING_ON

            #include "UnityCG.cginc"
            #include "UnityInstancing.cginc"
            

            //comienzo de cada variable
            #define POS_IDX 0
            #define ROT_IDX 3
            #define SCALE_IDX 7
            #define OPACITY_IDX 10
            #define SH_IDX 11

            #define SH_C0 0.28209479177387814
            #define SH_C1 0.4886025119029199

            #define SH_C2_0 1.0925484305920792
            #define SH_C2_1 -1.0925484305920792
            #define SH_C2_2 0.31539156525252005
            #define SH_C2_3 -1.0925484305920792
            #define SH_C2_4 0.5462742152960396

            #define SH_C3_0 -0.5900435899266435
            #define SH_C3_1 2.890611442640554
            #define SH_C3_2 -0.4570457994644658
            #define SH_C3_3 0.3731763325901154
            #define SH_C3_4 -0.4570457994644658
            #define SH_C3_5 1.445305721320277
            #define SH_C3_6 -0.5900435899266435

            StructuredBuffer<float> data;
            StructuredBuffer<uint> gaussians_order;
   
            uniform float3 hfov_focal;
            uniform float3 cam_pos;
            uniform int SH_DIM;
            uniform float4x4 modelPos; 
            
            
            float _UniformScale;
            float _Frustrum;
            int _RenderMode;
            float _ColorCorrection;


            float3 get_vec3(int offset){
                return float3(data[offset], data[offset+1], data[offset+2]);
            }
            float3x3 computeCov3D(float4 rots, float3 scales){
              
                float3x3 scaleMatrix = float3x3(
                     _UniformScale*scales.x, 0.0, 0.0,
                    0.0,  _UniformScale*scales.y, 0.0,
                    0.0,0.0, _UniformScale*scales.z
                );
               
                float3x3 rotMatrix = float3x3(
                    1.0 - 2.0 * (rots.z * rots.z + rots.w * rots.w), 2.0 * (rots.y * rots.z - rots.x * rots.w), 2.0 * (rots.y * rots.w + rots.x * rots.z),
                    2.0 * (rots.y * rots.z + rots.x * rots.w), 1.0 - 2.0 * (rots.y * rots.y + rots.w * rots.w), 2.0 * (rots.z * rots.w - rots.x * rots.y),
                    2.0 * (rots.y * rots.w - rots.x * rots.z), 2.0 * (rots.z * rots.w + rots.x * rots.y), 1.0 - 2.0 * (rots.y * rots.y + rots.z * rots.z)
                );
                rotMatrix =  mul((float3x3)modelPos, rotMatrix);
                float3x3 mMatrix = mul(rotMatrix, scaleMatrix);
                
                float3x3 sigma =  mul(mMatrix, transpose(mMatrix));
                return sigma;
            }

            float3 computeCov2D(float4 cam, float3x3 cov3d, float3 hfov_focal){
                float limx = 1.3*hfov_focal.x;
                float limy = 1.3*hfov_focal.y;

                float txtz = cam.x / cam.z;
                float tytz = cam.y / cam.z;
                float tx = min(limx, max(-limx, txtz)) * cam.z;
                float ty = min(limy, max(-limy, tytz)) * cam.z;

                float3x3 J = float3x3(
                    hfov_focal.z / cam.z, 0.0, -(hfov_focal.z * tx) / (cam.z * cam.z),
                    0.0, hfov_focal.z / cam.z, -(hfov_focal.z * ty) / (cam.z * cam.z),
                    0.0,0.0,0.0
                );
          
                float3x3 T = mul(J, ((float3x3)UNITY_MATRIX_V));
                float3x3 cov2d = mul(mul(T, (cov3d)), transpose(T));//mul(T,mul(transpose(cov3d), transpose(T)));
                    
                cov2d[0][0] += 0.3f;
                cov2d[1][1] += 0.3f;

                return float3(cov2d._m00, cov2d._m01, cov2d._m11);
             
            }
           
            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
                uint instanceID : SV_InstanceID;
            };

            struct v2f //sale del vertex para entrar en el fragment
            {
                precise float2 uv : TEXCOORD0; //coordxy
                
                float3 outColor : COLOR0;
                float4 vertex : SV_POSITION;

                precise float3 conic : TEXCOORD2;
                float opacity : TEXCOORD1; 
                uint instanceID : SV_InstanceID;
                
               
            };

            v2f vert (appdata v) //Vertex shader
            {

                
                v2f o;
                //Dibujar los puntos primeros
                UNITY_SETUP_INSTANCE_ID(v); 
                UNITY_TRANSFER_INSTANCE_ID(v,o);
                
                float2 quadPosition = v.vertex.xy*2.0; 
                int quadId = gaussians_order[v.instanceID]; 
                int total_dim = 3+4+3+1+SH_DIM;
                int start = quadId*total_dim;

                
                float3 center = get_vec3(start+POS_IDX);
                center = mul(modelPos, float4(center, 1)).xyz;
                float3 colorVal = get_vec3(start+SH_IDX); 
                float4 rotations = float4(data[start+ROT_IDX],data[start+ROT_IDX+1],data[start+ROT_IDX+2], data[start+ROT_IDX+3]);
              
                float3 scale = get_vec3(start+SCALE_IDX);
                

                float3x3 cov3d = computeCov3D(rotations, scale);
                float4 cam = mul(UNITY_MATRIX_V, float4(center, 1.0)); 
                
                //correct UNITY_MATRIX_P and GL.GetGPUProjectionMatrix diferences
                float4x4 correction = float4x4(
                    1, 0, 0, 0,
                    0, -1, 0, 0,
                    0, 0, 0.5, 0.5,
                    0, 0, 0, 1
                );
                correction = mul(correction, UNITY_MATRIX_P);
                float4 pos2d = mul(correction, cam); 
                
                 pos2d.xyz = pos2d.xyz / pos2d.w;
            
                 pos2d.w = 1.0;

                float4 basePos2d = mul(UNITY_MATRIX_P, cam);
                basePos2d.xyz = basePos2d.xyz / basePos2d.w;
                basePos2d.w = 1.0;


                
                float2 wh = 2.0* hfov_focal.xy * hfov_focal.z;

                if(any(abs(pos2d.xyz) > float3(_Frustrum, _Frustrum, _Frustrum))){
                    o.vertex = UnityObjectToClipPos(float4(-100,-100,-100,1));
                    
                    return o;
                }
                float3 cov2d = computeCov2D(cam, cov3d, hfov_focal);

                float det = (cov2d.x * cov2d.z - cov2d.y * cov2d.y); 
                
                if (det == 0.0){
                    o.vertex = UnityObjectToClipPos(float4(0.0,0.0,0.0,0.0));
                    
                    return o;
                }

                float det_inv = rcp(det);
                o.conic = float3(cov2d.z, -cov2d.y, cov2d.x) * det_inv;

                float2 quadwh_scr = float2(sqrt(cov2d.x), sqrt(cov2d.z)) * 3.0; 

                float2 quadwh_ndc = quadwh_scr / wh*2.0;
                
                pos2d.xy = pos2d.xy + quadPosition * quadwh_ndc;
                o.uv = quadPosition * quadwh_scr;
                pos2d.y *= -1.0;
                pos2d.zw = basePos2d.zw;
                o.vertex =pos2d;

                o.opacity = data[start+OPACITY_IDX];

                int sh_start = start+SH_IDX;
                
                float3 dir = center - cam_pos;
                dir = normalize(dir);
                dir *= -1;
                o.outColor = SH_C0 * colorVal;
                if(SH_DIM > 3 && _RenderMode >= 1){
                    float x = dir.x;
                    float y = dir.y;
                    float z = dir.z;
                    o.outColor = o.outColor - SH_C1 * y * get_vec3(sh_start+1*3) + 
                                SH_C1 * z * get_vec3(sh_start +2*3) - 
                                SH_C1 * x * get_vec3(sh_start +3*3);

                    if(SH_DIM > 12 && _RenderMode >= 2){
                        float xx = x * x, yy = y * y, zz = z * z;
                        float xy = x * y, yz = y * z, xz = x * z;
                        o.outColor = o.outColor +
                            SH_C2_0 * xy * get_vec3(sh_start + 4 * 3) +
                            SH_C2_1 * yz * get_vec3(sh_start + 5 * 3) +
                            SH_C2_2 * (2.0 * zz - xx - yy) * get_vec3(sh_start + 6 * 3) +
                            SH_C2_3 * xz * get_vec3(sh_start + 7 * 3) +
                            SH_C2_4 * (xx - yy) * get_vec3(sh_start + 8 * 3);
                            if (SH_DIM > 27 && _RenderMode >= 3)  // (1 + 3 + 5) * 3
                            {
                                o.outColor = o.outColor +
                                    SH_C3_0 * y * (3.0 * xx - yy) * get_vec3(sh_start + 9 * 3) +
                                    SH_C3_1 * xy * z * get_vec3(sh_start + 10 * 3) +
                                    SH_C3_2 * y * (4.0 * zz - xx - yy) * get_vec3(sh_start + 11 * 3) +
                                    SH_C3_3 * z * (2.0 * zz - 3.0f * xx - 3.0f * yy) * get_vec3(sh_start + 12 * 3) +
                                    SH_C3_4 * x * (4.0 * zz - xx - yy) * get_vec3(sh_start + 13 * 3) +
                                    SH_C3_5 * z * (xx - yy) * get_vec3(sh_start + 14 * 3) +
                                    SH_C3_6 * x * (xx - 3.0 * yy) * get_vec3(sh_start + 15 * 3);
                            }
                    }
                }
                o.outColor += _ColorCorrection;
                
                o.outColor = GammaToLinearSpace(o.outColor);
                return o;
            }

            float4 frag (v2f i) : SV_Target //Fragment shader
            {
             
                UNITY_SETUP_INSTANCE_ID(i);
                float power = -0.5 * (i.conic.x * i.uv.x * i.uv.x + i.conic.z * i.uv.y * i.uv.y) - i.conic.y * i.uv.x * i.uv.y;
                if(power > 0.0) discard;
                float alpha = min(0.99, i.opacity.x*exp(power));
                if(alpha < 1.0 / 255.0) discard;
               
               
                return float4(i.outColor, alpha);

                
            }
            ENDCG
        }
    }
}
