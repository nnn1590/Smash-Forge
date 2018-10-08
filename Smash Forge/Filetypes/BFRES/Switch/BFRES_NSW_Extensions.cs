using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Smash_Forge;
using Syroot.NintenTools.NSW.Bfres;
using Syroot.NintenTools.NSW.Bfres.GFX;
using System.IO;
using System.Windows.Forms;

namespace Smash_Forge
{
    //This will have extensions for getting and setting back switch classes from the libary
    //This is extended to keep it more organised and for switch/wii u to be seperate
    public static class BFRES_Switch_Extensions
    {
        public static void SetSwitchMaterial(this BFRES.MaterialData mat, Material m)
        {
            //Now use that to setup the new values
            mat.IsVisable = (int)m.Flags;
            mat.Name = m.Name;

            int CurTex = 0;
            foreach (TextureRef texture in m.TextureRefs)
            {
                BFRES.MatTexture tex = new BFRES.MatTexture();
                texture.Name = texture.Name;
                mat.textures.Add(tex);

                tex.BorderColorType = m.Samplers[CurTex].BorderColorType;
                tex.CompareFunc = m.Samplers[CurTex].CompareFunc;
                tex.FilterMode = m.Samplers[CurTex].FilterMode;
                tex.LODBias = m.Samplers[CurTex].LODBias;
                tex.MaxAnisotropic = m.Samplers[CurTex].MaxAnisotropic;
                tex.magFilter = (int)m.Samplers[CurTex].MaxLOD;
                tex.minFilter = (int)m.Samplers[CurTex].MinLOD;
                tex.wrapModeS = (int)m.Samplers[CurTex].WrapModeU;
                tex.wrapModeT = (int)m.Samplers[CurTex].WrapModeV;
                tex.wrapModeW = (int)m.Samplers[CurTex].WrapModeW;
                tex.SamplerName = m.SamplerDict.GetKey(CurTex);
                CurTex++;
            }
            foreach (RenderInfo renderinfo in m.RenderInfos)
            {
                BFRES.RenderInfoData rnd = new BFRES.RenderInfoData();
                rnd.Name = renderinfo.Name;

                if (renderinfo.Type == RenderInfoType.Int32)
                    rnd.Value_Ints = renderinfo.GetValueInt32s();
                if (renderinfo.Type == RenderInfoType.Single)
                    rnd.Value_Floats = renderinfo.GetValueSingles();
                if (renderinfo.Type == RenderInfoType.String)
                    rnd.Value_Strings = renderinfo.GetValueStrings();
            }
        }
        public static Material CreateSwitchMaterial(this BFRES.MaterialData mat)
        {
            Material m = new Material();
            m.Flags = (MaterialFlags)mat.IsVisable;
            m.Name = mat.Name;
            m.TextureRefs = new List<TextureRef>();
            m.RenderInfos = new List<RenderInfo>();
            m.Samplers = new List<Sampler>();
            m.VolatileFlags = new byte[0];
            m.UserDatas = new List<UserData>();
            m.ShaderParams = new List<ShaderParam>();
            m.SamplerDict = new ResDict();
            m.RenderInfoDict = new ResDict();
            m.ShaderParamDict = new ResDict();
            m.UserDataDict = new ResDict();
            m.VolatileFlags = new byte[0];
            m.TextureSlotArray = new long[mat.textures.Count];
            m.SamplerSlotArray = new long[mat.textures.Count];
            m.ShaderParamData = WriteShaderParams(mat);

            int CurTex = 0;
            foreach (BFRES.MatTexture tex in mat.textures)
            {
                TextureRef texture = new TextureRef();
                texture.Name = tex.Name;
                m.TextureRefs.Add(texture);

                Sampler samp = new Sampler();
                samp.BorderColorType = tex.BorderColorType;
                samp.CompareFunc = tex.CompareFunc;
                samp.FilterMode = tex.FilterMode;
                samp.LODBias = tex.LODBias;
                samp.MaxAnisotropic = tex.MaxAnisotropic;
                samp.MaxLOD = tex.magFilter;
                samp.MinLOD = tex.minFilter;
                samp.WrapModeU = (TexClamp)tex.wrapModeS;
                samp.WrapModeV = (TexClamp)tex.wrapModeT;
                samp.WrapModeW = (TexClamp)tex.wrapModeW;

                m.Samplers.Add(samp);

                m.SamplerDict.Add(tex.SamplerName);

                m.TextureSlotArray[CurTex] = -1;
                m.SamplerSlotArray[CurTex] = -1;

                CurTex++;

            }

            int CurParam = 0;
            foreach (var prm in mat.matparam)
            {
                ShaderParam shaderParam = new ShaderParam();
                shaderParam.Name = prm.Key;
                shaderParam.Type = (ShaderParamType)prm.Value.Type;
                shaderParam.DependIndex = (ushort)CurParam;
                shaderParam.DependedIndex = (ushort)CurParam;
                shaderParam.DataOffset = (ushort)prm.Value.DataOffset;
                CurParam++;
            }
            foreach (BFRES.RenderInfoData rnd in mat.renderinfo)
            {
                RenderInfo renderInfo = new RenderInfo();
                renderInfo.Name = rnd.Name;

                if (rnd.Type == Syroot.NintenTools.Bfres.RenderInfoType.Int32)
                    renderInfo.SetValue(rnd.Value_Ints);
                if (rnd.Type == Syroot.NintenTools.Bfres.RenderInfoType.Single)
                    renderInfo.SetValue(rnd.Value_Floats);
                if (rnd.Type == Syroot.NintenTools.Bfres.RenderInfoType.String)
                    renderInfo.SetValue(rnd.Value_Strings);

                m.RenderInfos.Add(renderInfo);
            }

            ShaderAssign shaderAssign = new ShaderAssign();
            shaderAssign.ShaderArchiveName = mat.shaderassign.ShaderArchive;
            shaderAssign.ShadingModelName = mat.shaderassign.ShaderModel;

            shaderAssign.ShaderOptionDict = new ResDict();
            shaderAssign.AttribAssignDict = new ResDict();
            shaderAssign.SamplerAssignDict = new ResDict();
            shaderAssign.ShaderOptions = new List<string>();
            shaderAssign.AttribAssigns = new List<string>();
            shaderAssign.SamplerAssigns = new List<string>();

            foreach (var op in mat.shaderassign.options)
            {
                shaderAssign.ShaderOptionDict.Add(op.Key);
                shaderAssign.ShaderOptions.Add(op.Value);
            }
            foreach (var att in mat.shaderassign.attributes)
            {
                shaderAssign.AttribAssignDict.Add(att.Key);
                shaderAssign.AttribAssigns.Add(att.Value);
            }
            foreach (var smp in mat.shaderassign.samplers)
            {
                shaderAssign.SamplerAssignDict.Add(smp.Key);
                shaderAssign.SamplerAssigns.Add(smp.Value);
            }

            m.ShaderAssign = shaderAssign;

            return m;
        }
        public static byte[] WriteShaderParams(this BFRES.MaterialData m)
        {
            using (var mem = new MemoryStream())
            using (Syroot.BinaryData.BinaryDataWriter writer = new Syroot.BinaryData.BinaryDataWriter(mem))
            {
                writer.ByteOrder = Syroot.BinaryData.ByteOrder.LittleEndian;
                foreach (var prm in m.matparam.Values)
                {
                    switch (prm.Type)
                    {
                        case Syroot.NintenTools.Bfres.ShaderParamType.Float:
                            writer.Write(prm.Value_float);
                            break;
                        case Syroot.NintenTools.Bfres.ShaderParamType.Float2:
                            writer.Write(prm.Value_float2.X);
                            writer.Write(prm.Value_float2.Y);
                            break;
                        case Syroot.NintenTools.Bfres.ShaderParamType.Float3:
                            writer.Write(prm.Value_float3.X);
                            writer.Write(prm.Value_float3.Y);
                            writer.Write(prm.Value_float3.Z);
                            break;
                        case Syroot.NintenTools.Bfres.ShaderParamType.Float4:
                            writer.Write(prm.Value_float4.X);
                            writer.Write(prm.Value_float4.Y);
                            writer.Write(prm.Value_float4.Z);
                            writer.Write(prm.Value_float4.W);
                            break;
                        case Syroot.NintenTools.Bfres.ShaderParamType.TexSrt:
                            writer.Write(prm.Value_TexSrt.Mode);
                            writer.Write(prm.Value_TexSrt.scale.X);
                            writer.Write(prm.Value_TexSrt.scale.Y);
                            writer.Write(prm.Value_TexSrt.rotate);
                            writer.Write(prm.Value_TexSrt.translate.X);
                            writer.Write(prm.Value_TexSrt.translate.Y);
                            break;
                        case Syroot.NintenTools.Bfres.ShaderParamType.Float4x4:
                            foreach (float f in prm.Value_float4x4)
                            {
                                writer.Write(f);
                            }
                            break;
                        case Syroot.NintenTools.Bfres.ShaderParamType.UInt:
                            writer.Write(prm.Value_UInt);
                            break;
                        default:
                            writer.Write(prm.UnkownTypeData);
                            break;
                    }
                }
                return mem.ToArray();
            }
        }
    }
}
