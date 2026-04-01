#nullable enable
using System.Collections.Generic;
using System.Linq;
using nadena.dev.ndmf;
using net.puk06.PropertySyncer.Editor.Ndmf;
using UnityEngine;

[assembly: ExportsPlugin(typeof(NdmfPlugin))]
namespace net.puk06.PropertySyncer.Editor.Ndmf
{
    internal class NdmfPlugin : Plugin<NdmfPlugin>
    {
        public override string QualifiedName => "net.puk06.material-property-sync";
        public override string DisplayName => "Material Property Sync";

        protected override void Configure()
        {
            InPhase(BuildPhase.Transforming)
                .AfterPlugin("net.rs64.tex-trans-tool")
                .AfterPlugin("nadena.dev.modular-avatar")
                .AfterPlugin("net.puk06.tex-stack-editor")
                .AfterPlugin("net.puk06.color-changer")
                .AfterPlugin("net.puk06.texture-replacer")
                .Run(ReplaceTextures.Instance)
#if LLC_2_4_0_OR_NEWER
                .BeforePass("io.github.azukimochi.light-limit-changer.normalize-materials")
#else
                .BeforePlugin("io.github.azukimochi.light-limit-changer")
#endif
                .PreviewingWith(new RealtimePreview());

            InPhase(BuildPhase.Optimizing)
                .AfterPlugin("net.rs64.tex-trans-tool")
                .BeforePlugin("com.anatawa12.avatar-optimizer")
                .Run(RemoveComponents.Instance);
        }
    }

    internal class ReplaceTextures : Pass<ReplaceTextures>
    {
        protected override void Execute(BuildContext context)
        {
            GameObject avatar = context.AvatarRootObject;
            AbstractMaterialPropertySync[] components = avatar.GetComponentsInChildren<AbstractMaterialPropertySync>(false);

            IEnumerable<AbstractMaterialPropertySync> enabledComponents = components.Where(x => x.gameObject.activeInHierarchy && x.IsEnabled);
            IEnumerable<Renderer> renderers = avatar.GetComponentsInChildren<Renderer>().Where(r => r is MeshRenderer or SkinnedMeshRenderer);
            foreach (Renderer renderer in renderers)
            {
                renderer.sharedMaterials = NdmfProcessor.SyncShadowSettingsInRenderer(enabledComponents, renderer);
            }
        }
    }

    internal class RemoveComponents : Pass<RemoveComponents>
    {
        protected override void Execute(BuildContext context)
        {
            GameObject avatar = context.AvatarRootObject;
            AbstractMaterialPropertySync[] components = avatar.GetComponentsInChildren<AbstractMaterialPropertySync>(true);

            RemoveAllComponents(components);
        }

        private void RemoveAllComponents(IEnumerable<Component> components)
        {
            foreach (Component component in components)
            {
                if (component == null) continue;
                Object.DestroyImmediate(component);
            }
        }
    }
}
