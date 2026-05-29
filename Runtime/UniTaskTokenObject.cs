using UnityEngine;
using UnityEngine.SceneManagement;

namespace CubicEngine.UnitaskExtension
{
[CreateAssetMenu(fileName = "NewUnitaskTokenData", menuName = "Utility/UnitaskExtention/TokenObject")]
public partial class UniTaskTokenObject : ScriptableObject
{
    [SerializeField] private bool isGlobalToken = false;
    [SerializeField] private string tokenKey;

    public UniTaskTokenContainer.CancellationTokenData GetTokenData()
    {
        if(isGlobalToken)
            return UniTaskTokenContainer.GetGlobalToken(tokenKey);
        return UniTaskTokenContainer.GetGroupToken(tokenKey);
    }

    public UniTaskTokenContainer.CancellationTokenData GetTokenData(Scene targetScene)
    {
        if(isGlobalToken)
            return UniTaskTokenContainer.GetGlobalToken(tokenKey);
        return UniTaskTokenContainer.GetGroupToken(tokenKey, targetScene);
    }

    public UniTaskTokenContainer.CancellationTokenData GetTokenData(GameObject targetObject)
    {
        if(isGlobalToken)
            return UniTaskTokenContainer.GetGlobalToken(tokenKey);
        return UniTaskTokenContainer.GetGroupToken(tokenKey, targetObject);
    }

    public UniTaskTokenContainer.CancellationTokenData GetTokenData(Component targetComponent)
    {
        if(isGlobalToken)
            return UniTaskTokenContainer.GetGlobalToken(tokenKey);
        return UniTaskTokenContainer.GetGroupToken(tokenKey, targetComponent);
    }

    public bool Cancel()
    {
        return UniTaskTokenContainer.Cancel(tokenKey);
    }

    public bool Cancel(Scene targetScene)
    {
        if(isGlobalToken)
            return UniTaskTokenContainer.Cancel(tokenKey);
        return UniTaskTokenContainer.Cancel(tokenKey, targetScene);
    }

    public bool Cancel(GameObject targetObject)
    {
        if(isGlobalToken)
            return UniTaskTokenContainer.Cancel(tokenKey);
        return UniTaskTokenContainer.Cancel(tokenKey, targetObject);
    }

    public bool Cancel(Component targetComponent)
    {
        if(isGlobalToken)
            return UniTaskTokenContainer.Cancel(tokenKey);
        return UniTaskTokenContainer.Cancel(tokenKey, targetComponent);
    }
}
}
