using Cysharp.Threading.Tasks;
using UnityEngine;
using static UniTaskTokenContainer;

public class TestTokenContainer : MonoBehaviour
{
    private CancellationTokenData global;
    private CancellationTokenData scene;
    private CancellationTokenData group;
    private CancellationTokenData obj;
    
    [SerializeField] private UniTaskTokenObject tokenObject;

    void Start()
    {
        global = UniTaskTokenContainer.GetGlobalToken("GlobalToken");
        scene = UniTaskTokenContainer.GetSceneToken();
        group = UniTaskTokenContainer.GetGroupToken("GroupToken");
        obj = UniTaskTokenContainer.GetObjectToken();

        TestTask("global", global).Forget();
        TestTask("scene", scene).Forget();
        TestTask("group_task0", group).Forget();
        TestTask("group_task1", group).Forget();
        TestTask("obj0", obj).Forget();
        
        TestTask("token_object", tokenObject.GetTokenData()).Forget();
    }


    private async UniTaskVoid TestTask(string name, CancellationTokenData tokenData)
    {
        while(true) {
            await UniTask.Delay(1500, false, PlayerLoopTiming.Update, tokenData.Token);
            Debug.LogFormat("{0}", name);
        }
    }

    private void Update()
    {
        if(Input.GetKeyUp(KeyCode.Alpha1)) {
            UniTaskTokenContainer.Cancel("GlobalToken");
        }
        else if(Input.GetKeyUp(KeyCode.Alpha2)) {
            UniTaskTokenContainer.Cancel(scene);
        }
        else if(Input.GetKeyUp(KeyCode.Alpha3)) {
            UniTaskTokenContainer.Cancel(group.TokenID);
        }
        else if(Input.GetKeyUp(KeyCode.Alpha4)) {
            UniTaskTokenContainer.Cancel(obj);
        }
        else if(Input.GetKeyUp(KeyCode.Alpha5))
        {
            tokenObject.Cancel();
        }
    }
}
