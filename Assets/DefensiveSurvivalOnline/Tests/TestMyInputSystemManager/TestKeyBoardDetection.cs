using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class TestKeyBoardDetection : MonoBehaviour
{
    //����
    MyInputSystemManager input => MyInputSystemManager.Instance;
    //ʵʱ
    public Vector2 MoveActionValue;


    private void Update()
    {
        MoveActionValue = input.MoveActionValue;
    }


}
