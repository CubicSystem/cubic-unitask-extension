using System;
using UnityEngine;
using System.Threading;
using System.Collections.Generic;
using UnityEngine.SceneManagement;

namespace CubicEngine.UnitaskExtension
{
public static class UniTaskTokenContainer
{
    public enum TokenType { SCENE, GROUP, OBJECT, GLOBAL, NONE }

    private const int InvalidSceneHandle = -1;
    private static int nextTokenID;

    public struct CancellationTokenData
    {
        public CancellationToken Token { get; private set; }
        public int TokenID { get; private set; }
        public int SceneHandle { get; private set; }
        public TokenType Type { get; private set; }
        public bool IsValid => TokenID != 0;

        public CancellationTokenData(CancellationToken token, int tokenID)
            : this(token, tokenID, InvalidSceneHandle, TokenType.NONE)
        {
        }

        public CancellationTokenData(CancellationToken token, int tokenID, int sceneHandle, TokenType type)
        {
            Token = token;
            TokenID = tokenID;
            SceneHandle = sceneHandle;
            Type = type;
        }
    }
    private class CancellationTokenHandler : IDisposable
    {
        private CancellationTokenSource source;
        public int TokenID { get; private set; }
        public int SceneHandle { get; private set; }
        public TokenType Type { get; private set; }

        public CancellationTokenData CancellationTokenData
        {
            get {
                var token = source != null ? source.Token : CancellationToken.None;
                return new CancellationTokenData(token, TokenID, SceneHandle, Type);
            }
        }


        public CancellationTokenHandler(TokenType type, int sceneHandle)
        {
            source = new CancellationTokenSource();
            TokenID = Interlocked.Increment(ref nextTokenID);
            Type = type;
            SceneHandle = sceneHandle;
        }

        public void Dispose()
        {
            if(source != null) {
                source.Cancel();
                source.Dispose();
                source = null;
            }
        }
    }
    private class SceneCancellationTokenHandler
    {
        private readonly int sceneHandle;
        private CancellationTokenHandler sceneToken;
        private Dictionary<string, CancellationTokenHandler> groupTokens = new Dictionary<string, CancellationTokenHandler>();
        private Dictionary<int, CancellationTokenHandler> objectTokens = new Dictionary<int, CancellationTokenHandler>();

        public SceneCancellationTokenHandler(int sceneHandle)
        {
            this.sceneHandle = sceneHandle;
        }

        //Create TokenSource and return CancellationTokenData
        #region GetToken Functions
        public CancellationTokenData GetSceneToken()
        {
            if(sceneToken == null) {
                sceneToken = new CancellationTokenHandler(TokenType.SCENE, sceneHandle);
            }
            return sceneToken.CancellationTokenData;
        }

        public CancellationTokenData GetObjectToken()
        {
            var newToken = new CancellationTokenHandler(TokenType.OBJECT, sceneHandle);
            objectTokens.Add(newToken.TokenID, newToken);

            return newToken.CancellationTokenData;
        }

        public CancellationTokenData GetGroupToken(string groupKey)
        {

            if(!groupTokens.TryGetValue(groupKey, out var tokenInfo)) {
                tokenInfo = new CancellationTokenHandler(TokenType.GROUP, sceneHandle);
                groupTokens[groupKey] = tokenInfo;
            }
            return tokenInfo.CancellationTokenData;
        }
        #endregion

        //Cancellation and Disposable
        #region CancelToken Function
        public bool Cancel(int tokenID)
        {
            return RemoveToken(tokenID);
        }

        public bool Cancel(in CancellationTokenData tokenData)
        {
            return Cancel(tokenData.TokenID);
        }

        public bool Cancel(string key)
        {
            if(groupTokens.TryGetValue(key, out var tokenHandler)) {
                groupTokens.Remove(key);
                tokenHandler.Dispose();
                return true;
            }
            return false;
        }
        #endregion

        //Clear TokenSource
        #region Clear Token
        public void ClearToken()
        {
            ClearToken(TokenType.SCENE);
            ClearToken(TokenType.OBJECT);
            ClearToken(TokenType.GROUP);
        }

        public void ClearToken(TokenType tokenType)
        {
            switch(tokenType) {
                case TokenType.SCENE: {
                    var tokenHandler = sceneToken;
                    sceneToken = null;
                    tokenHandler?.Dispose();
                    break;
                }
                case TokenType.OBJECT: {
                    var tokenHandlers = new List<CancellationTokenHandler>(objectTokens.Values);
                    objectTokens.Clear();
                    foreach(var item in tokenHandlers) {
                        item.Dispose();
                    }
                    break;
                }
                case TokenType.GROUP: {
                    var tokenHandlers = new List<CancellationTokenHandler>(groupTokens.Values);
                    groupTokens.Clear();
                    foreach(var item in tokenHandlers) {
                        item.Dispose();
                    }
                    break;
                }
                default: {
                    break;
                }
            }
        }
        #endregion

        /**
         *  @brief  토큰을 삭제한다.
         *  @param  tokenID : 삭제하려는 token id
         *  @return true : token이 있는 경우, false : token이 없는 경우
         */
        private bool RemoveToken(int tokenID)
        {
            //check scene token
            if(sceneToken != null && sceneToken.TokenID == tokenID) {
                var tokenHandler = sceneToken;
                sceneToken = null;
                tokenHandler.Dispose();
                return true;
            }

            //find object token
            if(objectTokens.TryGetValue(tokenID, out var getInfo)) {
                objectTokens.Remove(tokenID);
                getInfo.Dispose();
                return true;
            }

            //find group token
            string removeGroupKey = null;
            CancellationTokenHandler removeGroupToken = null;
            foreach(var group in groupTokens) {
                if(group.Value.TokenID == tokenID) {
                    removeGroupKey = group.Key;
                    removeGroupToken = group.Value;
                    break;
                }
            }
            if(removeGroupKey != null) {
                groupTokens.Remove(removeGroupKey);
                removeGroupToken.Dispose();
                return true;
            }
            return false;
        }

        /**
         *  @brief  tokenID와 동일한 Token 정보가 있는지 확인한다.
         *  @param  tokenID : 찾으려는 token id
         *  @return 찾은 tokenInfo
         */
        internal CancellationTokenHandler GetTokenInfo(int tokenID)
        {
            //check scene token
            if(sceneToken != null && sceneToken.TokenID == tokenID) {
                return sceneToken;
            }

            //find object token
            if(objectTokens.TryGetValue(tokenID, out var getInfo)) {
                return getInfo;
            }

            //find group token
            foreach(var group in groupTokens) {
                if(group.Value.TokenID == tokenID) {
                    return group.Value;
                }
            }
            return null;
        }
    }

    private static Dictionary<string, CancellationTokenHandler> globalTokens = new Dictionary<string, CancellationTokenHandler>();
    private static Dictionary<int, SceneCancellationTokenHandler> sceneCancellationTokenHandlers = new Dictionary<int, SceneCancellationTokenHandler>();

    //called when the project start
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void Initialize()
    {
        SceneManager.sceneUnloaded -= SceneManager_sceneUnloaded;
        SceneManager.sceneUnloaded += SceneManager_sceneUnloaded;
    }

    private static void SceneManager_sceneUnloaded(Scene arg0)
    {
        if(sceneCancellationTokenHandlers.TryGetValue(arg0.handle, out var handler)) {
            sceneCancellationTokenHandlers.Remove(arg0.handle);
            handler.ClearToken();
        }
    }

    private static bool CancellationSceneToken(int sceneHandle, int tokenID)
    {
        var handler = GetSceneCancellationTokenHandler(sceneHandle);
        if(handler == null) {
            return false;
        }
        return handler.Cancel(tokenID);
    }

    private static bool CancellationSceneToken(int sceneHandle, string key)
    {
        var handler = GetSceneCancellationTokenHandler(sceneHandle);
        if(handler == null) {
            return false;
        }
        return handler.Cancel(key);
    }

    private static SceneCancellationTokenHandler GetSceneCancellationTokenHandler(int sceneHandle, bool isCreate = false)
    {
        if(sceneCancellationTokenHandlers.TryGetValue(sceneHandle, out var tokenHandler)) {
            return tokenHandler;
        }
        else if(isCreate) {
            var newSceneCancellationTokenHandler = new SceneCancellationTokenHandler(sceneHandle);
            sceneCancellationTokenHandlers[sceneHandle] = newSceneCancellationTokenHandler;
            return newSceneCancellationTokenHandler;
        }

        return null;
    }

    private static Scene GetTargetScene(Scene? scene)
    {
        if(scene != null) {
            if(!scene.Value.IsValid()) {
                throw new InvalidOperationException("not found target scene data");
            }
            return scene.Value;
        }

        var activeScene = SceneManager.GetActiveScene();
        if(!activeScene.IsValid()) {
            throw new InvalidOperationException("not found target scene data");
        }

        return activeScene;
    }

    private static Scene GetTargetScene(GameObject targetObject)
    {
        if(targetObject == null) {
            throw new ArgumentNullException(nameof(targetObject));
        }

        var targetScene = targetObject.scene;
        if(!targetScene.IsValid()) {
            throw new InvalidOperationException("not found target object scene data");
        }

        return targetScene;
    }

    private static Scene GetTargetScene(Component targetComponent)
    {
        if(targetComponent == null) {
            throw new ArgumentNullException(nameof(targetComponent));
        }

        return GetTargetScene(targetComponent.gameObject);
    }

    private static bool CancelGlobalToken(string key)
    {
        if(globalTokens.TryGetValue(key, out var handler)) {
            globalTokens.Remove(key);
            handler.Dispose();
            return true;
        }
        return false;
    }

    private static bool CancelGlobalToken(int tokenID)
    {
        string removeKey = null;
        foreach(var item in globalTokens) {
            if(item.Value.TokenID == tokenID) {
                removeKey = item.Key;
                break;
            }
        }

        if(removeKey == null) {
            return false;
        }

        var handler = globalTokens[removeKey];
        globalTokens.Remove(removeKey);
        handler.Dispose();
        return true;
    }

    public static void ClearGlobalTokens()
    {
        var tokenHandlers = new List<CancellationTokenHandler>(globalTokens.Values);
        globalTokens.Clear();
        foreach(var tokenHandler in tokenHandlers) {
            tokenHandler.Dispose();
        }
    }
    
    //Create TokenSource and return CancellationTokenData
    public static CancellationTokenData GetGlobalToken(string globalKey)
    {
        if(globalTokens.TryGetValue(globalKey, out var tokenHandler)){
            return tokenHandler.CancellationTokenData;
        }
        else {
            var newCancellationTokenHandler = new CancellationTokenHandler(TokenType.GLOBAL, InvalidSceneHandle);
            globalTokens[globalKey] = newCancellationTokenHandler;
            return newCancellationTokenHandler.CancellationTokenData;
        }
    }

    public static CancellationTokenData GetSceneToken(Scene? targetScene = null)
    {
        int sceneHandle = GetTargetScene(targetScene).handle;
        var sceneCancellationTokenHandler = GetSceneCancellationTokenHandler(sceneHandle, true);
        return sceneCancellationTokenHandler.GetSceneToken();
    }

    public static CancellationTokenData GetSceneToken(GameObject targetObject)
    {
        return GetSceneToken(GetTargetScene(targetObject));
    }

    public static CancellationTokenData GetSceneToken(Component targetComponent)
    {
        return GetSceneToken(GetTargetScene(targetComponent));
    }

    public static CancellationTokenData GetGroupToken(string groupKey, Scene? targetScene = null)
    {
        int sceneHandle = GetTargetScene(targetScene).handle;
        var sceneCancellationTokenHandler = GetSceneCancellationTokenHandler(sceneHandle, true);
        return sceneCancellationTokenHandler.GetGroupToken(groupKey);
    }

    public static CancellationTokenData GetGroupToken(string groupKey, GameObject targetObject)
    {
        return GetGroupToken(groupKey, GetTargetScene(targetObject));
    }

    public static CancellationTokenData GetGroupToken(string groupKey, Component targetComponent)
    {
        return GetGroupToken(groupKey, GetTargetScene(targetComponent));
    }

    public static CancellationTokenData GetObjectToken(Scene? targetScene = null)
    {
        int sceneHandle = GetTargetScene(targetScene).handle;
        var sceneCancellationTokenHandler = GetSceneCancellationTokenHandler(sceneHandle, true);
        return sceneCancellationTokenHandler.GetObjectToken();
    }

    public static CancellationTokenData GetObjectToken(GameObject targetObject)
    {
        return GetObjectToken(GetTargetScene(targetObject));
    }

    public static CancellationTokenData GetObjectToken(Component targetComponent)
    {
        return GetObjectToken(GetTargetScene(targetComponent));
    }

    public static bool Cancel(string key)
    {
        //현재 씬에서 찾기
        var activeScene = SceneManager.GetActiveScene();
        bool isCancelled = activeScene.IsValid() && CancellationSceneToken(activeScene.handle, key);

        //global token에서 찾기
        return CancelGlobalToken(key) || isCancelled;
    }

    public static bool Cancel(string key, Scene targetScene)
    {
        bool isCancelled = CancellationSceneToken(GetTargetScene(targetScene).handle, key);
        return CancelGlobalToken(key) || isCancelled;
    }

    public static bool Cancel(string key, GameObject targetObject)
    {
        return Cancel(key, GetTargetScene(targetObject));
    }

    public static bool Cancel(string key, Component targetComponent)
    {
        return Cancel(key, GetTargetScene(targetComponent));
    }

    public static bool Cancel(int tokenID)
    {
        var sceneTokenHandlers = new List<SceneCancellationTokenHandler>(sceneCancellationTokenHandlers.Values);
        foreach(var sceneToken in sceneTokenHandlers) {
            if(sceneToken.Cancel(tokenID)) {
                return true;
            }
        }

        //global token에서 찾기
        return CancelGlobalToken(tokenID);
    }

    public static bool Cancel(int tokenID, Scene targetScene)
    {
        if(CancellationSceneToken(GetTargetScene(targetScene).handle, tokenID)) {
            return true;
        }

        return CancelGlobalToken(tokenID);
    }

    public static bool Cancel(int tokenID, GameObject targetObject)
    {
        return Cancel(tokenID, GetTargetScene(targetObject));
    }

    public static bool Cancel(int tokenID, Component targetComponent)
    {
        return Cancel(tokenID, GetTargetScene(targetComponent));
    }

    public static bool Cancel(in CancellationTokenData tokenData)
    {
        if(!tokenData.IsValid) {
            return false;
        }

        if(tokenData.Type == TokenType.GLOBAL) {
            return CancelGlobalToken(tokenData.TokenID);
        }

        if(tokenData.SceneHandle != InvalidSceneHandle && CancellationSceneToken(tokenData.SceneHandle, tokenData.TokenID)) {
            return true;
        }

        return Cancel(tokenData.TokenID);
    }

    public static bool IsCancelled(CancellationTokenData tokenData)
    {
        return tokenData.IsValid && tokenData.Token.IsCancellationRequested;
    }
}
}
