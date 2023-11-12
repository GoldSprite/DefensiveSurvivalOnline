using System;
using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// 用于根据设备展示/隐藏触屏控件
/// </summary>
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


    /// <summary>
    /// 系统检测<para/>
    /// 是否手持设备
    /// </summary>
    /// <returns></returns>
    private bool IsHandHeldDevice()
    {
        return SystemInfo.deviceType == DeviceType.Handheld;
    }
}
