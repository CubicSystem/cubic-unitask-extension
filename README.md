> # UniTaskExtention

- Unitask 편의 기능 확장
  - Cancellation을 위한 토큰 생성 및 관리 기능 추가
- Version 1.1.0



> # 설치

1. 우선 Unitask의 설치가 필요하다.

   - upm git : https://github.com/Cysharp/UniTask.git?path=src/UniTask/Assets/Plugins/UniTask
     - package manager를 통해 위 git 주소를 추가

   - 자세한 내용은 [Unitask GitPage](https://github.com/Cysharp/UniTask)를 확인

2. UniTaskExtention 추가
   - upm git : https://github.com/hns17/UnityUtility.git?path=Assets/Utility/UniTaskExtention
     - 위와 동일한 방법으로 추가



> # 항목

## UnitaskTokenContainer

- Container를 통해 Token을 생성하고 파괴(Cancel)한다.



### Token의 종류

1. GlobalToken
   - Project 전체에서 사용되는 Token으로 직접 Container를 통해 직접 파괴(Cancel)하지 않으면 파괴되지 않는다.
   - string key 값을 통해 발행한다.
2. SceneToken
   - Scene 내부에서 사용하는 공유 토큰으로 하나만 존재한다.
   - Scene이 Unload되면 파괴된다.
3. GroupToken
   - Task의 Token을 Group단위로 관리할 때 사용하는 Token
   - string key 값으로 발행한다.
   -  Scene이 Unload되면 파괴된다.
4. ObjectToken
   - Object단위로 사용되는 Token.
   - Scene이 Unload되면 파괴된다.



### Struct & Function

1. CancellationTokenData
   - UniTaskTokenContainer를 통해 발행한 Token Data
     - TokenID(int)
       - 발행된 CancellationToken의 고유 id
     - Token(CancellationToken)
       - 발행된 CancellationToke
2. Get Cancellation Token
   - 목적에 맞는 토큰 발행하기
   - CancellationTokenData를 가져오며, 없는 경우 생성 후 가져온다.
   - return : TokenData

```
//Get GlobalToken
CancellationTokenData global = UniTaskTokenContainer.GetGlobalToken("GlobalToken");

//Get SceneToken
CancellationTokenData scene = UniTaskTokenContainer.GetSceneToken();

//Get GroupToken
CancellationTokenData group = UniTaskTokenContainer.GetGroupToken("GroupToken");

//Get ObjectToken
CancellationTokenData obj = UniTaskTokenContainer.GetObjectToken();
```

3. Cancel
   - 발행된 Token 파괴하기
   - return : bool
     - Cancel이 실패한 경우 false, 성공한 경우 true

```
bool isCancel = false;
CancellationTokenData global = UniTaskTokenContainer.GetGlobalToken("GlobalToken");

//TokenID를 통해 파괴
isCancel = UniTaskTokenContainer.Cancel(group.TokenID);

//string Key값을 통해 파괴
isCancel = UniTaskTokenContainer.Cancel("GlobalToken");

//TokenData를 통해 파괴
isCancel = UniTaskTokenContainer.Cancel(global);
```



## GetCancellationTokenOnDisableAndDestroy Method

- Monobehaviour용 확장함수
- Unitask의 GetCancellationOnDestroy 함수의 확장형
- 오브젝트가 비활성화 되거나 파괴될 경우 Cancel되는 Token을 발행한다.

```C#
public class XXXXXX : MonoBehaviour{
    async UniTaskVoid TestTask() {
        CancellationTokenData token = this.GetCancellationTokenOnDisableAndDestroy();
        
        while(true) {
            await UniTask.Delay(1000, false, PlayerLoopTiming.Update, token.Token);
			...
        }
    }    
}
```



## UniTaskBehaviour

- GetCancellationTokenOnDisableAndDestroy 함수는 내부적으로 GetComponent 및 AddComponent 함수를 호출한다.
- 반복적으로 사용 할 경우 Overhead 문제가 있으므로 이를 해결하기 위해 UniTaskBehaviour를 상속받아서 사용한다.
- UniTaskBehaviour는 AddComponent 연산을 수행하지 않으며, 처음 Token 발행시 한번만 GetComponent 연산을 수행한다.
- MemberFunction
  - CreateToken : CancellationTokenData 가져오기
  - Cancel(int) : Token Cancel
  - Cancel(TokenData) : Token Cancel

```c#
public class TestMonoTask : UniTaskBehaviour<TestMonoTask>{
    private CancellationTokenData tokenData;
    
	async UniTaskVoid TestTask()
    {
        tokenData = CreateToken();
        
        while(true) {
            //Get Token, this.Token
            await UniTask.Delay(1000, false, PlayerLoopTiming.Update, tokenData.Token);
			...
        }
    }
    
    private void Update(){
        if(Input.GetKeyUp(KeyCode.Space)) {
            //Cancellation Token
            this.Cancel(tokenData);
        }
    }
}
```



# UniTaskTokenObject

- Unity의 ScriptableObject와 SerializeField 기능을 사용하여 종속성 없이 GlobalToken과 GroupToken의 key 정보를 공유하기 위해 사용
- 코드 상으로 key를 관리할 필요가 없다.
- 하지만 asset 파일을 만들기 때문에 파일 관리가 필요하다.
