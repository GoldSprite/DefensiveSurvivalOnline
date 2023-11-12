using System;
using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// 用于根据设备展示/隐藏触屏控件
/// </summary>
public class ShowWidgetController : MonoBehaviour
{
    public GameObject TouchWidgets;
    public bool ShowTouchWidgets;
    bool oldShowTouchWidgets;


    private void Start()
    {
        TouchWidgets.SetActive(oldShowTouchWidgets = ShowTouchWidgets = IsHandHeldDevice());
    }


    private void Update()
    {
        CGShow();
    }


    void CGShow()
    {
        if (oldShowTouchWidgets != ShowTouchWidgets)
            TouchWidgets.SetActive(oldShowTouchWidgets = ShowTouchWidgets);
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
