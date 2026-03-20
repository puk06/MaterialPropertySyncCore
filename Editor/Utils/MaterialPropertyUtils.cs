using System;
using System.Linq;
using net.puk06.PropertySyncer.Editor.Extension;
using UnityEditor;
using UnityEngine;

namespace net.puk06.PropertySyncer.Editor.Utils
{
    internal static class MaterialPropertyUtils
    {
        internal static int CalculatePropertyHash(Material material, string[] targetPropertyNames)
        {
            int hash = 17;

            material.ForEachProperty((propertyType, propName) =>
            {
                if (!targetPropertyNames.Contains(propName)) return;

                switch (propertyType)
                {
                    case ShaderUtil.ShaderPropertyType.Color:
                        {
                            Color color = material.GetColor(propName);
                            hash = hash * 31 + color.GetHashCode();
                            break;
                        }
                    case ShaderUtil.ShaderPropertyType.Range:
                    case ShaderUtil.ShaderPropertyType.Float:
                        {
                            float value = material.GetFloat(propName);
                            hash = hash * 31 + value.GetHashCode();
                            break;
                        }
                    case ShaderUtil.ShaderPropertyType.Int:
                        {
                            int value = material.GetInt(propName);
                            hash = hash * 31 + value.GetHashCode();
                            break;
                        }
                    case ShaderUtil.ShaderPropertyType.TexEnv:
                        {
                            Texture texture = material.GetTexture(propName);
                            if (texture == null) break;
                            hash = hash * 31 + texture.GetHashCode();
                            break;
                        }
                    case ShaderUtil.ShaderPropertyType.Vector:
                        {
                            Vector4 value = material.GetVector(propName);
                            hash = hash * 31 + value.GetHashCode();
                            break;
                        }
                }
            });

            return hash;
        }
    }
}
