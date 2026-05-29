using System.Collections.Generic;
using UnityEngine;
using Cysharp.Threading.Tasks.Triggers;
using static UniTaskTokenContainer;

namespace Cysharp.Threading.Tasks
{
    /**
     * @author  hns17@naver.com
     * @brief   Monobehaviour에서 사용할 CancellationTokenSource 관리를 위한 Extention Method
     *          OnDisable 및 OnDestroy가 호출되면 Cancel 처리되는 토큰을 리턴한다.
     */
    public static class UnityCancellationTask
    {
        /// <summary>This CancellationToken is canceled when the MonoBehaviour will be destroyed.</summary>
        public static CancellationTokenData GetCancellationTokenOnDisableAndDestroy(this GameObject gameObject)
        {
            return gameObject.GetAsyncDisableAndDestroyTrigger().CancellationToken;
        }

        /// <summary>This CancellationToken is canceled when the MonoBehaviour will be destroyed.</summary>
        public static CancellationTokenData GetCancellationTokenOnDisableAndDestroy(this Component component)
        {
            return component.GetAsyncDisableAndDestroyTrigger().CancellationToken;
        }
    }
}


#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member
namespace Cysharp.Threading.Tasks.Triggers
{
    /**
     * @author  hns17@naver.com
     * @brief   CancellationToken 관리용 컴포넌트(AsyncDisableAndDestroyTrigger)를 반환한다.
     *          오브젝트에 컴포넌트가 없는 경우 추가 후 반환
     */
    public static partial class AsyncTriggerExtensions
    {
        public static AsyncDisableAndDestroyTrigger GetAsyncDisableAndDestroyTrigger(this GameObject gameObject)
        {
            return GetOrAddComponent<AsyncDisableAndDestroyTrigger>(gameObject);
        }

        public static AsyncDisableAndDestroyTrigger GetAsyncDisableAndDestroyTrigger(this Component component)
        {
            return component.gameObject.GetAsyncDisableAndDestroyTrigger();
        }


        static T GetOrAddComponent<T>(GameObject gameObject)
    where T : Component
        {
#if UNITY_2019_2_OR_NEWER
            if(!gameObject.TryGetComponent<T>(out var component)) {
                component = gameObject.AddComponent<T>();
            }
#else
            var component = gameObject.GetComponent<T>();
            if (component == null)
            {
                component = gameObject.AddComponent<T>();
            }
#endif

            return component;
        }
    }

    /**
     * @author  hns17@naver.com
     * @brief   CancellationToken 관리용 클래스
     *          Monobehaviour의 이벤트 함수 OnDisable, OnDestroy 함수가 호출되면 Cancel 되는 Token 관리용 컴포넌트
     */
    [DisallowMultipleComponent]
    public sealed class AsyncDisableAndDestroyTrigger : MonoBehaviour
    {
        private Dictionary<int, CancellationTokenData> tokenDatas = new Dictionary<int, CancellationTokenData>();

        public CancellationTokenData CancellationToken
        {
            get {
                if(!gameObject.activeSelf) {
                    var errMsg = string.Format("Couldn't be getted because the game object '{0}' is inactive!", this.gameObject.name);
                    throw new System.Exception(errMsg);
                }

                var newCancellationTokenData = UniTaskTokenContainer.GetObjectToken();
                tokenDatas.Add(newCancellationTokenData.TokenID, newCancellationTokenData);
                return newCancellationTokenData;
            }
        }

        public bool Cancel(int tokenID)
        {
            var res = false;
            if(tokenDatas.TryGetValue(tokenID, out var tokenData)) {
                res = UniTaskTokenContainer.Cancel(tokenID);
                tokenDatas.Remove(tokenID);
            }
            return res;
        }

        public bool Cancel(CancellationTokenData targetData)
        {
            return Cancel(targetData.TokenID);
        }

        public void Clear()
        {
            foreach(var tokenData in tokenDatas) {
                UniTaskTokenContainer.Cancel(tokenData.Value);
            }
            tokenDatas.Clear();
        }

        void OnDisable()
        {
            Clear();
        }

        void OnDestroy()
        {
            Clear();
        }
    }
}

