using System;
using UnityEngine;
using UnityEngine.InputSystem;

public class ShowWidgetController : MonoBehaviour
{
    public GameObject TouchWidgets;
    bool showTouchWidgets;
    public bool ShowTouchWidgets { 
        get { return showTouchWidgets; } 
        set
        {
            showTouchWidgets = value;
            TouchWidgets.SetActive(value);  //手动更新值时切换显示
        }
    }


    private void Start()
    {
        ShowTouchWidgets = IsHandHeldDevice();  //手持设备自动显示触屏控件
    }


    private bool IsHandHeldDevice()
    {
        return SystemInfo.deviceType == DeviceType.Handheld;
    }
}
