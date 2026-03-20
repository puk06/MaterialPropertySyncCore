using System;
using UnityEditor;
using UnityEngine;

namespace net.puk06.PropertySyncer.Editor.Extension
{
    internal static class MaterialExtensions
    {
        internal static void ForEachProperty(this Material material, Action<ShaderUtil.ShaderPropertyType, string> action)
        {
            if (material == null || action == null) return;

            Shader shader = material.shader;
            if (shader == null) return;

            int propertyCount = ShaderUtil.GetPropertyCount(shader);
            for (int i = 0; i < propertyCount; i++)
            {
                string propName = ShaderUtil.GetPropertyName(shader, i);
                if (propName == null) continue;

                action(ShaderUtil.GetPropertyType(shader, i), propName);
            }
        }
    }
}
