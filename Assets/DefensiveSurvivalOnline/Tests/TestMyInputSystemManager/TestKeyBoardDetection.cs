using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class TestKeyBoardDetection : MonoBehaviour
{
    //引用
    MyInputSystemManager input => MyInputSystemManager.Instance;
    //实时
    public Vector2 MoveActionValue;


    private void Update()
    {
        MoveActionValue = input.MoveActionValue;
    }


}
