using UnityEngine;
using Cysharp.Threading.Tasks.Triggers;
using static UniTaskTokenContainer;

/**
 *  @author : hns17@naver.com
 *  @brief  : Monobehaviour 용 CancellationToken 관리용 클래스
 *              - UniTask를 코루틴 처럼 사용하기 위해 작성
 *              - OnDisable, OnDistroy, Cancel 작업을 수행한다.
 */
[RequireComponent(typeof(AsyncDisableAndDestroyTrigger))]
public class UniTaskBehaviour<T> : MonoBehaviour where T : MonoBehaviour
{
    private AsyncDisableAndDestroyTrigger trigger;

    /**
     *  @brief  : Token 발행
     *  @return
     *      - true  : 정상적으로 Cancel
     *      - false : Cancel Failed
     */
    public bool Cancel(int tokenID)
    {
        if(trigger == null) {
            return false;
        }

        return trigger.Cancel(tokenID);
    }

    public bool Cancel(CancellationTokenData tokenData)
    {
        return Cancel(tokenData.TokenID);
    }


    /**
     *  @brief  Token 발행
     *  @return 발행된 Token 반환
     */
    public CancellationTokenData CreateToken()
    {
        if(trigger == null) {
            this.TryGetComponent<AsyncDisableAndDestroyTrigger>(out trigger);
        }

        return trigger.CancellationToken;
    }
}
