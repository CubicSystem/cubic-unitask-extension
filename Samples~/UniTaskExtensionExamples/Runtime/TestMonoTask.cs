using CubicEngine.UnitaskExtension;
using UnityEngine;
using UnityEngine.SceneManagement;
using Cysharp.Threading.Tasks;
using static CubicEngine.UnitaskExtension.UniTaskTokenContainer;

namespace CubicEngine.UnitaskExtension.Samples
{
public class TestMonoTask : UniTaskBehaviour<TestMonoTask>
{
    private CancellationTokenData tokenData;

    private void Awake()
    {
        Debug.Log("Awake");
    }
    private void OnEnable()
    {
        TestTask().Forget();
    }

    async UniTaskVoid TestTask()
    {
        tokenData = CreateToken();
        while(true) {
            await UniTask.Delay(1000, false, PlayerLoopTiming.Update, tokenData.Token);
            Debug.Log("Test" + SceneManager.GetActiveScene().buildIndex);
        }
    }

    private void Update()
    {
        if(Input.GetKeyUp(KeyCode.Space)) {
            Cancel(tokenData);
        }
        else if(Input.GetKeyUp(KeyCode.Return)) {
            TestTask().Forget();
        }
    }
}
}
