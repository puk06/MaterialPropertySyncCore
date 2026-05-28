#nullable enable
using UnityEngine;
using UnityEditor;
using net.puk06.PropertySyncer;
using UnityEditor.SceneManagement;

namespace net.puk06.PropertySyncer.Editor
{
    [CustomEditor(typeof(AbstractMaterialPropertySync), true)]
    public class AbstractMaterialPropertySyncInspector : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            serializedObject.Update();
            DrawDefaultInspector();
            serializedObject.ApplyModifiedProperties();

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("", GUI.skin.horizontalSlider);

            EditorGUILayout.LabelField("プレビュー", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox("プレビューを更新しても、ビルド時には最新の状態が正しく同期されます。プレビューは編集時の確認用です。", MessageType.Info);

            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("プレビューを更新する"))
            {
                foreach (var obj in targets)
                {
                    if (obj is AbstractMaterialPropertySync sync)
                    {
                        sync.PreviewRefreshRequested = true;
                        EditorUtility.SetDirty(sync);
                        if (!Application.isPlaying)
                        {
                            EditorSceneManager.MarkSceneDirty(sync.gameObject.scene);
                        }
                    }
                }
            }

            EditorGUILayout.EndHorizontal();
        }
    }
}
