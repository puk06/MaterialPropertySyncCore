#nullable enable
using System;
using System.Collections.Generic;
using System.Linq;
using nadena.dev.ndmf;
using net.puk06.PropertySyncer.Editor.Extension;
using UnityEditor;
using UnityEngine;

namespace net.puk06.PropertySyncer.Editor.Ndmf
{
    internal class NdmfProcessor
    {
        internal static Dictionary<Material, Material> BuildProcessedMaterialDictionary(
            IEnumerable<AbstractMaterialPropertySync> components,
            IEnumerable<Renderer> renderers,
            bool isPreview = true)
        {
            var result = new Dictionary<Material, Material>();

            var activeComponents = new List<AbstractMaterialPropertySync>();
            foreach (var c in components)
                if (c.IsActivePSComponent(isPreview))
                    activeComponents.Add(c);

            if (activeComponents.Count == 0) return result;

            var targetMap = new Dictionary<Material, AbstractMaterialPropertySync>();
            foreach (var component in activeComponents)
            {
                foreach (var targetMat in component.TargetMaterials)
                {
                    if (targetMat == null) continue;
                    var resolved = ObjectRegistry.GetReference(targetMat).Object as Material;
                    if (resolved == null) resolved = targetMat;
                    
                    if (!targetMap.ContainsKey(resolved))
                        targetMap[resolved] = component;
                }
            }

            if (targetMap.Count == 0) return result;

            foreach (var renderer in renderers)
            {
                foreach (var mat in renderer.sharedMaterials)
                {
                    if (mat == null) continue;
                    var resolved = ObjectRegistry.GetReference(mat).Object as Material;
                    if (resolved == null) resolved = mat;
                    
                    if (!targetMap.TryGetValue(resolved, out var component)) continue;
                    if (result.ContainsKey(resolved)) continue;
                    if (component.SourceMaterial == null) continue;

                    var processed = GetProcessedMaterial(component.SourceMaterial, mat, component.TargetPropertyNames, component.IncludeTexture);
                    if (processed != null)
                        result[resolved] = processed;
                }
            }

            return result;
        }

        internal static void ReplaceMaterialsInRenderers(IEnumerable<Renderer> renderers, Dictionary<Material, Material> processedMaterialDictionary)
        {
            if (processedMaterialDictionary.Count == 0) return;

            var registeredOriginals = new HashSet<Material>();
            var replacedMaterials = new HashSet<Material>();

            foreach (var renderer in renderers)
            {
                var materials = renderer.sharedMaterials;
                bool changed = false;

                foreach (ref var material in materials.AsSpan())
                {
                    if (material == null) continue;
                    if (replacedMaterials.Contains(material)) continue;

                    var resolved = ObjectRegistry.GetReference(material).Object as Material;
                    if (resolved == null) resolved = material;

                    if (processedMaterialDictionary.TryGetValue(resolved, out var processed))
                    {
                        if (!registeredOriginals.Contains(resolved))
                        {
                            ObjectRegistry.RegisterReplacedObject(resolved, processed);
                            registeredOriginals.Add(resolved);
                        }
                        replacedMaterials.Add(processed);
                        material = processed;
                        changed = true;
                    }
                }

                if (changed) renderer.sharedMaterials = materials;
            }
        }

        internal static Material?[] GetReplacedMaterials(Material?[] materials, Dictionary<Material, Material> processedMaterialDictionary, Dictionary<Material, Material> materialMap)
        {
            if (processedMaterialDictionary.Count == 0) return materials;

            var newMaterials = (Material?[])materials.Clone();
            bool changed = false;

            for (int i = 0; i < newMaterials.Length; i++)
            {
                var newMaterial = newMaterials[i];
                if (newMaterial == null) continue;

                var resolved = ObjectRegistry.GetReference(newMaterial).Object as Material;
                if (resolved == null) resolved = newMaterial;

                if (materialMap.TryGetValue(resolved, out var cached))
                {
                    newMaterials[i] = cached;
                    changed = true;
                }
                else if (processedMaterialDictionary.TryGetValue(resolved, out var processed))
                {
                    materialMap[resolved] = processed;
                    newMaterials[i] = processed;
                    changed = true;
                }
            }

            return changed ? newMaterials : materials;
        }

        internal static Material? GetProcessedMaterial(Material? sourceMaterial, Material? targetMaterial, string[] targetProperties, bool includeTexture)
        {
            if (sourceMaterial == null || targetMaterial == null) return null;

            var newMaterial = UnityEngine.Object.Instantiate(targetMaterial);

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
