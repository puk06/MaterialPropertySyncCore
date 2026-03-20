#nullable enable
using System;
using UnityEngine;
using System.Collections.Generic;

namespace net.puk06.PropertySyncer
{
    [Serializable]
    public abstract class AbstractMaterialPropertySync : MonoBehaviour, VRC.SDKBase.IEditorOnly
    {
        [Header("ツールの有効化")]
        public bool IsEnabled = true;

        [Header("リアルタイムプレビュー")]
        public bool IsPreviewEnabled = true;

        [Header("同期元マテリアル")]
        public Material? SourceMaterial = null;

        [Header("テクスチャも同期する")]
        public bool IncludeTexture = false;

        [Header("同期先マテリアル")]
        public List<Material?> TargetMaterials = new();

        [HideInInspector]
        abstract public string[] TargetPropertyNames { get; }
    }
}
