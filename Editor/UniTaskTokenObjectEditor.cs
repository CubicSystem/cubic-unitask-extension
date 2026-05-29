using CubicEngine.UnitaskExtension;
using UnityEditor;

namespace CubicEngine.UnitaskExtension.Editor
{
    [CustomEditor(typeof(UniTaskTokenObject))]
    public sealed class UniTaskTokenObjectEditor : UnityEditor.Editor
    {
        private void OnEnable()
        {
            UniTaskTokenObjectEditorUtility.TryEnsureTokenKey((UniTaskTokenObject)target);
        }

        public override void OnInspectorGUI()
        {
            UniTaskTokenObjectEditorUtility.TryEnsureTokenKey((UniTaskTokenObject)target);
            DrawDefaultInspector();
        }
    }

    public sealed class UniTaskTokenObjectAssetPostprocessor : AssetPostprocessor
    {
        private static void OnPostprocessAllAssets(
            string[] importedAssets,
            string[] deletedAssets,
            string[] movedAssets,
            string[] movedFromAssetPaths)
        {
            foreach(var assetPath in importedAssets) {
                var tokenObject = AssetDatabase.LoadAssetAtPath<UniTaskTokenObject>(assetPath);
                UniTaskTokenObjectEditorUtility.TryEnsureTokenKey(tokenObject, assetPath);
            }
        }
    }

    internal static class UniTaskTokenObjectEditorUtility
    {
        private const string TokenKeyPropertyName = "tokenKey";

        public static bool TryEnsureTokenKey(UniTaskTokenObject tokenObject)
        {
            if(tokenObject == null) {
                return false;
            }

            return TryEnsureTokenKey(tokenObject, AssetDatabase.GetAssetPath(tokenObject));
        }

        public static bool TryEnsureTokenKey(UniTaskTokenObject tokenObject, string assetPath)
        {
            if(tokenObject == null || string.IsNullOrEmpty(assetPath)) {
                return false;
            }

            var serializedObject = new SerializedObject(tokenObject);
            var tokenKeyProperty = serializedObject.FindProperty(TokenKeyPropertyName);
            if(tokenKeyProperty == null || !string.IsNullOrEmpty(tokenKeyProperty.stringValue)) {
                return false;
            }

            var guid = AssetDatabase.AssetPathToGUID(assetPath);
            if(string.IsNullOrEmpty(guid)) {
                return false;
            }

            tokenKeyProperty.stringValue = guid;
            serializedObject.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(tokenObject);
            return true;
        }
    }
}
