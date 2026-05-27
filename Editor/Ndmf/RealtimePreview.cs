#nullable enable
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading.Tasks;
using nadena.dev.ndmf.preview;
using net.puk06.PropertySyncer.Editor.Utils;
using UnityEngine;
using Object = UnityEngine.Object;

namespace net.puk06.PropertySyncer.Editor.Ndmf
{
    internal class RealtimePreview : IRenderFilter
    {
        public ImmutableList<RenderGroup> GetTargetGroups(ComputeContext context)
        {
            IEnumerable<GameObject> avatarGameObjects = context.GetAvatarRoots().Distinct();

            List<RenderGroup> targetRenderGroups = new();

            foreach (GameObject avatarGameObject in avatarGameObjects)
            {
                try
                {
                    AbstractMaterialPropertySync[] components = context.GetComponentsInChildren<AbstractMaterialPropertySync>(avatarGameObject, true);
                    if (components.Length == 0) continue;

                    List<Material> targetMaterials = new();

                    foreach (AbstractMaterialPropertySync component in components)
                    {
                        context.Observe(component, c => new List<Material?>(c.TargetMaterials), (a, b) => a.SequenceEqual(b));
                        foreach (Material? material in component.TargetMaterials)
                        {
                            if (material == null || targetMaterials.Contains(material)) continue;
                            targetMaterials.Add(material);
                        }
                    }

                    List<Renderer> targetRenderers = new();
                    foreach (Renderer avatarRenderer in avatarGameObject.GetComponentsInChildren<Renderer>().Where(r => r is MeshRenderer or SkinnedMeshRenderer))
                    {
                        Material[] materials = avatarRenderer.sharedMaterials;
                        if (materials == null) continue;

                        if (materials.Any(i => targetMaterials.Contains(i)))
                        {
                            targetRenderers.Add(avatarRenderer);
                        }
                    }

                    if (targetRenderers.Count > 0)
                    {
                        targetRenderGroups.Add(RenderGroup.For(targetRenderers).WithData(avatarGameObject));
                    }
                }
                catch (Exception ex)
                {
                    Debug.LogError($"Failed to add renderer for avatar: '{avatarGameObject.name}'.\n{ex}");
                }
            }

            return targetRenderGroups.ToImmutableList();
        }

        public Task<IRenderFilterNode> Instantiate(RenderGroup group, IEnumerable<(Renderer, Renderer)> proxyPairs, ComputeContext context)
        {
            Dictionary<Renderer, Material?[]>? processedMaterialDictionary = new();

            try
            {
                GameObject root = group.GetData<GameObject>();

                AbstractMaterialPropertySync[] components = root.GetComponentsInChildren<AbstractMaterialPropertySync>(true);
                if (components.Length == 0) return Task.FromResult<IRenderFilterNode>(new EmptyNode());

                foreach (AbstractMaterialPropertySync component in components)
                {
                    context.Observe(component);
                    context.Observe(component, c => new List<Material?>(c.TargetMaterials), (a, b) => a.SequenceEqual(b));
                }

                foreach ((Renderer original, Renderer proxy) in proxyPairs)
                {
                    processedMaterialDictionary[original] = NdmfProcessor.SyncShadowSettingsInRenderer(components, proxy, isPreview: true);
                }

                return Task.FromResult<IRenderFilterNode>(new MaterialReplacerNode(processedMaterialDictionary));
            }
            catch (Exception ex)
            {
                Debug.LogError($"Failed to instantiate.\n{ex}");
                if (processedMaterialDictionary != null)
                {
                    foreach (Material?[] materials in processedMaterialDictionary.Values)
                        foreach (Material? material in materials)
                            if (material != null) Object.DestroyImmediate(material);
                    processedMaterialDictionary.Clear();
                    processedMaterialDictionary = null;
                }
                return Task.FromResult<IRenderFilterNode>(new MaterialReplacerNode(null));
            }
        }

        private class MaterialReplacerNode : IRenderFilterNode, IDisposable
        {
            private Dictionary<Renderer, Material?[]>? _processedMaterialDictionary;

            public RenderAspects WhatChanged { get; private set; } = RenderAspects.Texture & RenderAspects.Material;

            public MaterialReplacerNode(Dictionary<Renderer, Material?[]>? processedMaterialDictionary)
            {
                _processedMaterialDictionary = processedMaterialDictionary;
            }

            public void OnFrame(Renderer original, Renderer proxy)
            {
                try
                {
                    if (_processedMaterialDictionary?.TryGetValue(original, out Material?[] processedMaterials) ?? false)
                    {
                        proxy.sharedMaterials = processedMaterials;
                    }
                }
                catch (Exception ex)
                {
                    Debug.LogError("Error occurred while rendering proxy.\n" + ex);
                }
            }

            public void Dispose()
            {
                if (_processedMaterialDictionary != null)
                {
                    foreach (Material?[] materials in _processedMaterialDictionary.Values)
                        foreach (Material? material in materials)
                            if (material != null) Object.DestroyImmediate(material);
                    _processedMaterialDictionary.Clear();
                    _processedMaterialDictionary = null;
                }
            }
        }

        private class EmptyNode : IRenderFilterNode
        {
            public RenderAspects WhatChanged { get; private set; } = 0;

            public void OnFrame(Renderer original, Renderer proxy)
            {
                // Do nothing
            }
        }
    }
}
