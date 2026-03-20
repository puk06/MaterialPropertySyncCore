#nullable enable
using System.Collections.Generic;
using System.Linq;
using nadena.dev.ndmf;
using net.puk06.PropertySyncer.Editor.Extension;
using net.puk06.PropertySyncer.Editor.Utils;
using UnityEditor;
using UnityEngine;

namespace net.puk06.PropertySyncer.Editor.Ndmf
{
    internal class NdmfProcessor
    {
        internal static Material?[] SyncShadowSettingsInRenderer(IEnumerable<AbstractMaterialPropertySync> components, Renderer renderer)
        {
            Material?[] materials = renderer.sharedMaterials;
            Material?[] newMaterials = new Material[materials.Length];

            for (int i = 0; i < materials.Length; i++)
            {
                if (materials[i] == null) continue;

                Material? originalMaterial = ObjectRegistry.GetReference(materials[i]).Object as Material;
                if (originalMaterial == null)
                {
                    newMaterials[i] = Object.Instantiate(materials[i]);
                }
                else
                {
                    bool processed = false;

                    foreach (AbstractMaterialPropertySync component in components)
                    {
                        if (component.TargetMaterials.Contains(originalMaterial))
                        {
                            newMaterials[i] = GetProcessedMaterial(component.SourceMaterial, materials[i], component.TargetPropertyNames, component.IncludeTexture);
                            processed = true;
                            break;
                        }
                    }

                    if (!processed) newMaterials[i] = Object.Instantiate(materials[i]);
                }

                ObjectRegistry.RegisterReplacedObject(materials[i], newMaterials[i]);
            }

            return newMaterials;
        }

        internal static Material? GetProcessedMaterial(Material? sourceMaterial, Material? targetMaterial, string[] targetProperties, bool includeTexture)
        {
            if (sourceMaterial == null || targetMaterial == null) return null;

            Material newMaterial = Object.Instantiate(targetMaterial);

            sourceMaterial.ForEachProperty((propertyType, propName) =>
            {
                if (!targetProperties.Contains(propName)) return;

                switch (propertyType)
                {
                    case ShaderUtil.ShaderPropertyType.Color:
                        {
                            Color color = sourceMaterial.GetColor(propName);
                            newMaterial.SetColor(propName, color);
                            break;
                        }
                    case ShaderUtil.ShaderPropertyType.Range:
                    case ShaderUtil.ShaderPropertyType.Float:
                        {
                            float value = sourceMaterial.GetFloat(propName);
                            newMaterial.SetFloat(propName, value);
                            break;
                        }
                    case ShaderUtil.ShaderPropertyType.Int:
                        {
                            int value = sourceMaterial.GetInt(propName);
                            newMaterial.SetInt(propName, value);
                            break;
                        }
                    case ShaderUtil.ShaderPropertyType.TexEnv:
                        {
                            if (!includeTexture) break;
                            Texture texture = sourceMaterial.GetTexture(propName);
                            newMaterial.SetTexture(propName, texture);
                            break;
                        }
                    case ShaderUtil.ShaderPropertyType.Vector:
                        {
                            Vector4 value = sourceMaterial.GetVector(propName);
                            newMaterial.SetVector(propName, value);
                            break;
                        }
                }
            });

            return newMaterial;
        }
    }
}
