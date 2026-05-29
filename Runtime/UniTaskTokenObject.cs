using UnityEngine;
using UnityEditor;

[CreateAssetMenu(fileName = "NewUnitaskTokenData", menuName = "Utility/UnitaskExtention/TokenObject")]
public class UniTaskTokenObject : ScriptableObject
{
    [SerializeField] private bool isGlobalToken = false;
    [SerializeField] private string tokenKey;

    public UniTaskTokenContainer.CancellationTokenData GetTokenData()
    {
        if(isGlobalToken)
            return UniTaskTokenContainer.GetGlobalToken(tokenKey);
        return UniTaskTokenContainer.GetGroupToken(tokenKey);
    }

    public bool Cancel()
    {
        return UniTaskTokenContainer.Cancel(tokenKey);
    }
    
#if UNITY_EDITOR
    private void OnValidate()
    {
        if (tokenKey != "")
            return;
        
        string assetPath = AssetDatabase.GetAssetPath(this);
        string guid = AssetDatabase.AssetPathToGUID(assetPath);

        tokenKey = guid;
    }
#endif
}
