#nullable enable
using System;
using UnityEngine;

namespace net.puk06.PropertySyncer
{
    [Serializable]
    public class MaterialPropertySync : AbstractMaterialPropertySync
    {
        [Header("同期するプロパティ名")]
        public string[] TargetProperties = new string[0];
        public override string[] TargetPropertyNames => TargetProperties;
    }
}
