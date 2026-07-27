#nullable enable
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading.Tasks;
using nadena.dev.ndmf.preview;
using UnityEngine;
using Object = UnityEngine.Object;

namespace net.puk06.PropertySyncer.Editor.Ndmf
{
    internal class RealtimePreview : IRenderFilter
    {
        public ImmutableList<RenderGroup> GetTargetGroups(ComputeContext context)
        {
            var avatarGameObjects = context.GetAvatarRoots().Distinct();

            var targetRenderGroups = new List<RenderGroup>();

            foreach (var avatarGameObject in avatarGameObjects)
            {
                try
                {
                    var components = context.GetComponentsInChildren<AbstractMaterialPropertySync>(avatarGameObject, true);
                    if (components.Length == 0) continue;

                    var targetMaterials = new List<Material>();

                    foreach (var component in components)
                    {
                        foreach (Material? material in component.TargetMaterials)
                        {
                            if (material == null || targetMaterials.Contains(material)) continue;
                            targetMaterials.Add(material);
                        }
                    }

                    var targetRenderers = new List<Renderer>();
                    foreach (var avatarRenderer in context.GetComponentsInChildren<Renderer>(avatarGameObject, true).Where(r => r is MeshRenderer or SkinnedMeshRenderer))
                    {
                        var materials = context.Observe(avatarRenderer, i => i.sharedMaterials, (a, b) => a != null && b != null && a.SequenceEqual(b));
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
            var processedMaterialDictionary = new Dictionary<Renderer, Material?[]>();
            Dictionary<Material, Material>? materialMap = null;

            try
            {
                var root = group.GetData<GameObject>();

                var components = root.GetComponentsInChildren<AbstractMaterialPropertySync>(true);
                if (components.Length == 0) return Task.FromResult<IRenderFilterNode>(new EmptyNode());

                foreach (var component in components)
                {
                    context.Observe(component);
                    if (component.SourceMaterial != null)
                    {
                        context.Observe(component, c => c.SourceMaterial == null ? -1 : c.SourceMaterial.ComputeCRC(), (a, b) => a == b);
                    }
                    context.Observe(component, c => new List<Material?>(c.TargetMaterials), (a, b) => a.SequenceEqual(b));
                }

                var processedDict = NdmfProcessor.BuildProcessedMaterialDictionary(
                    components, proxyPairs.Select(p => p.Item2), isPreview: true);

                materialMap = new();

                foreach ((Renderer original, Renderer proxy) in proxyPairs)
                {
                    var result = NdmfProcessor.GetReplacedMaterials(proxy.sharedMaterials, processedDict, materialMap);
                    if (result != proxy.sharedMaterials)
                        processedMaterialDictionary[original] = result;
                }

                return Task.FromResult<IRenderFilterNode>(new MaterialReplacerNode(processedMaterialDictionary, materialMap.Values));
            }
            catch (Exception ex)
            {
                Debug.LogError($"Failed to instantiate.\n{ex}");
                if (materialMap != null)
                {
                    foreach (var material in materialMap.Values)
                        Object.DestroyImmediate(material);
                }
                if (processedMaterialDictionary != null)
                {
                    processedMaterialDictionary.Clear();
                    processedMaterialDictionary = null;
                }
                return Task.FromResult<IRenderFilterNode>(new MaterialReplacerNode(null, null));
            }
        }

        private class MaterialReplacerNode : IRenderFilterNode, IDisposable
        {
            private Dictionary<Renderer, Material?[]>? _processedMaterialDictionary;
            private IEnumerable<Material>? _createdMaterials;

            public RenderAspects WhatChanged { get; private set; } = RenderAspects.Texture | RenderAspects.Material;

            public MaterialReplacerNode(Dictionary<Renderer, Material?[]>? processedMaterialDictionary, IEnumerable<Material>? createdMaterials)
            {
                _processedMaterialDictionary = processedMaterialDictionary;
                _createdMaterials = createdMaterials;
            }

            public void OnFrame(Renderer original, Renderer proxy)
            {
                try
                {
                    if (_processedMaterialDictionary?.TryGetValue(original, out var processedMaterials) ?? false)
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
                if (_createdMaterials != null)
                {
                    foreach (var material in _createdMaterials)
                        Object.DestroyImmediate(material);
                    _createdMaterials = null;
                }

                if (_processedMaterialDictionary != null)
                {
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
