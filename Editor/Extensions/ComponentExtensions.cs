using UnityEngine;

namespace net.puk06.PropertySyncer.Editor.Extension
{
    internal static class ComponentExtensions
    {
        public static bool IsActivePSComponent(this Component component, bool isPreview = false)
        {
            if (!IsActiveComponent(component)) return false;

            if (component is AbstractMaterialPropertySync materialPropertySyncComponent)
            {
                return materialPropertySyncComponent.IsEnabled && (isPreview == false || materialPropertySyncComponent.IsPreviewEnabled);
            }

            return false;
        }

        public static bool IsActiveComponent(this Component component)
        {
            return component.gameObject.activeInHierarchy && component.IsEditorOnly() == false;
        }

        public static bool IsEditorOnly(this Component component)
        {
            return component.CompareTag("EditorOnly");
        }
    }
}
