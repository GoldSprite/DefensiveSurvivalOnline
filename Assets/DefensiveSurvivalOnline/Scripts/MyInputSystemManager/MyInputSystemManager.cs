using System;
using UnityEngine;
using UnityEngine.InputSystem;

public class MyInputSystemManager : MonoBehaviour
{
    //配置
    public static InputActions InputActionInstance { get; private set; }
    public static MyInputSystemManager Instance;
    //实时
    public Vector2 MoveActionValue { get; private set; }


    private void Awake()
    {
        if (Instance == null) Instance = this;
        initInputActions();
        RegisterAllActionListener();
    }

    private void OnDisable()
    {
        InputActionInstance.Disable();
    }


    /// <summary>
    /// 全局唯一<para/>
    /// 初始化输入核心类
    /// </summary>
    private void initInputActions()
    {
        if (InputActionInstance != null) return;

        DontDestroyOnLoad(gameObject);
        InputActionInstance = new InputActions();
        InputActionInstance.Enable();
    }


    /// <summary>
    /// 在初始化时<para/>
    /// 注册所有Action的监听器<para/>
    /// PS: performed-更改时, started-仅按下, canceled-取消时
    /// </summary>
    private void RegisterAllActionListener()
    {
        Action<InputAction.CallbackContext> MoveListener = (callbacks) =>
        {
            MoveActionValue = InputActionInstance.GamePlay.Move.ReadValue<Vector2>();
            //Debug.Log(MoveActionValue);
        };
        InputActionInstance.GamePlay.Move.performed += MoveListener;
        InputActionInstance.GamePlay.Move.canceled += MoveListener;
    }


}
