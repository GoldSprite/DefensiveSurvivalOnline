using System;
using UnityEngine;
using UnityEngine.InputSystem;

public class MyInputSystemManager : MonoBehaviour
{
    //配置
    public static InputActions InputActionInstance { get; private set; }
    public static MyInputSystemManager Instance;
    public Joystick joystick;
    //实时
    public Vector2 MoveActionValue { get; private set; }
    Vector2 oldJoystickMoveActionValue;


    private void Awake()
    {
        if (Instance == null) Instance = this;
        initInputActions();
        RegisterAllActionListener();
        GetJoystick();
    }

    private void OnDisable()
    {
        InputActionInstance.Disable();
    }

    private void Update()
    {
        UpdateValues();
    }


    /// <summary>
    /// 更新同步所有Action值
    /// </summary>
    private void UpdateValues()
    {
        UpdateJoystickValue();
    }


    /// <summary>
    /// 更改时<para/>
    /// 刷新Joystick轮盘输入
    /// </summary>
    private void UpdateJoystickValue()
    {
        var move = MoveActionValue;
        move.x = joystick.Horizontal;
        move.y = joystick.Vertical;
        if (oldJoystickMoveActionValue != move)
        {
            MoveActionValue = oldJoystickMoveActionValue = move;
            //Debug.Log("刷新摇杆值");
        }
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


    private void GetJoystick()
    {
        joystick = GameObject.FindObjectOfType<Joystick>().GetComponent<Joystick>();
    }


}
