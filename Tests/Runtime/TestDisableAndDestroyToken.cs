using Cysharp.Threading.Tasks;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TestDisableAndDestroyToken : MonoBehaviour
{
    private void Awake()
    {
        Debug.Log("Awake");
        var token = this.GetCancellationTokenOnDisableAndDestroy();
    }
    private void OnDestroy()
    {
        Debug.Log("onDestroy");
    }
}
