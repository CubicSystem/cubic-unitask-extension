using System;
using UnityEngine;
using System.Threading;
using System.Collections.Generic;
using UnityEngine.SceneManagement;

public static class UniTaskTokenContainer
{
    public enum TokenType { SCENE, GROUP, OBJECT, GLOBAL, NONE }

    public struct CancellationTokenData
    {
        public CancellationToken Token { get; private set; }
        public int TokenID { get; private set; }
        
        public CancellationTokenData(CancellationToken token, int handleId)
        {
            Token = token;
            TokenID = handleId;
        }
    }
    private class CancellationTokenHandler : IDisposable
    {
        private CancellationTokenSource source;
        public int TokenID { get; private set; }

        public CancellationTokenData CancellationTokenData => new CancellationTokenData(source.Token, TokenID);


        public CancellationTokenHandler()
        {
            source = new CancellationTokenSource();
            TokenID = source.GetHashCode();
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
        private CancellationTokenHandler sceneToken;
        private Dictionary<string, CancellationTokenHandler> groupTokens = new Dictionary<string, CancellationTokenHandler>();
        private Dictionary<int, CancellationTokenHandler> objectTokens = new Dictionary<int, CancellationTokenHandler>();

        //Create TokenSource and return CancellationTokenData
        #region GetToken Functions
        public CancellationTokenData GetSceneToken()
        {
            sceneToken ??= new CancellationTokenHandler();
            return sceneToken.CancellationTokenData;
        }

        public CancellationTokenData GetObjectToken()
        {
            var newToken = new CancellationTokenHandler();
            objectTokens.Add(newToken.TokenID, newToken);

            return newToken.CancellationTokenData;
        }

        public CancellationTokenData GetGroupToken(string groupKey)
        {

            if(!groupTokens.TryGetValue(groupKey, out var tokenInfo)) {
                tokenInfo = new CancellationTokenHandler();
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
                tokenHandler.Dispose();
                groupTokens.Remove(key);
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
                    sceneToken?.Dispose();
                    break;
                }
                case TokenType.OBJECT: {
                    foreach(var item in objectTokens) {
                        item.Value.Dispose();
                    }
                    objectTokens.Clear();
                    break;
                }
                case TokenType.GROUP: {
                    foreach(var item in groupTokens) {
                        item.Value.Dispose();
                    }
                    groupTokens.Clear();
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
                sceneToken.Dispose();
                sceneToken = null;
                return true;
            }

            //find object token
            if(objectTokens.TryGetValue(tokenID, out var getInfo)) {
                getInfo.Dispose();
                objectTokens.Remove(tokenID);
                return true;
            }

            //find group token
            foreach(var group in groupTokens) {
                if(group.Value.TokenID == tokenID) {
                    group.Value.Dispose();
                    groupTokens.Remove(group.Key);
                    return true;
                }
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
        SceneManager.sceneUnloaded += SceneManager_sceneUnloaded;
    }

    private static void SceneManager_sceneUnloaded(Scene arg0)
    {
        if(sceneCancellationTokenHandlers.TryGetValue(arg0.handle, out var handler)) {
            handler.ClearToken();
            sceneCancellationTokenHandlers.Remove(arg0.handle);
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
            var newSceneCancellationTokenHandler = new SceneCancellationTokenHandler();
            sceneCancellationTokenHandlers[sceneHandle] = newSceneCancellationTokenHandler;
            return newSceneCancellationTokenHandler;
        }

        return null;
    }

    private static Scene GetTargetScene(Scene? scene)
    {
        if(scene == null || scene.Value.buildIndex == -1) {
            var activeScene = SceneManager.GetActiveScene();
            if(!activeScene.IsValid()) {
                throw new InvalidOperationException("not found target scene data");
            }
            return activeScene;
        }

        return scene.Value;
    }
    
    //Create TokenSource and return CancellationTokenData
    public static CancellationTokenData GetGlobalToken(string globalKey)
    {
        if(globalTokens.TryGetValue(globalKey, out var tokenHandler)){
            return tokenHandler.CancellationTokenData;
        }
        else {
            var newCancellationTokenHandler = new CancellationTokenHandler();
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

    public static CancellationTokenData GetGroupToken(string groupKey, Scene? targetScene = null)
    {
        int sceneHandle = GetTargetScene(targetScene).handle;
        var sceneCancellationTokenHandler = GetSceneCancellationTokenHandler(sceneHandle, true);
        return sceneCancellationTokenHandler.GetGroupToken(groupKey);
    }

    public static CancellationTokenData GetObjectToken(Scene? targetScene = null)
    {
        int sceneHandle = GetTargetScene(targetScene).handle;
        var sceneCancellationTokenHandler = GetSceneCancellationTokenHandler(sceneHandle, true);
        return sceneCancellationTokenHandler.GetObjectToken();
    }

    public static bool Cancel(string key)
    {
        //현재 씬에서 찾기
        var activeScene = SceneManager.GetActiveScene();
        int sceneHandle = activeScene.handle;

        CancellationSceneToken(sceneHandle, key);

        //global token에서 찾기
        if(globalTokens.TryGetValue(key, out var handler)) {
            handler.Dispose();
            globalTokens.Remove(key);
            return true;
        }
        return false;
    }

    public static bool Cancel(int tokenID)
    {
        var activeScene = SceneManager.GetActiveScene();
        int sceneHandle = activeScene.handle;

        //현재 씬에서 토큰 찾기
        if(CancellationSceneToken(sceneHandle, tokenID)) {
            return true;
        }

        //global token에서 찾기
        foreach(var item in globalTokens) {
            var tokenHandler = item.Value;
            if(tokenHandler.TokenID == tokenID) {
                tokenHandler.Dispose();
                globalTokens.Remove(item.Key);
                return true;
            }
        }
        return false;
    }

    public static bool Cancel(in CancellationTokenData tokenData)
    {
        return Cancel(tokenData.TokenID);
    }

    public static bool IsCancelled(CancellationTokenData tokenData)
    {
        foreach (var token in globalTokens)
        {
            if(token.Value.TokenID == tokenData.TokenID) {
                return false;
            }
        }

        foreach (var sceneToken in sceneCancellationTokenHandlers)
        {
            if(sceneToken.Value.GetTokenInfo(tokenData.TokenID) == null) {
                return false;
            }
        }
        
        return true;
    }
}
