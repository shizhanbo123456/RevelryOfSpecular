using System;
using UnityEngine;
using UnityEngine.UI;

//调用Set添加回调、可设置是否可交互、设置是否被选中
//ToggleGroup需手动单独实现
public class RosToggle : MonoBehaviour
{
    [SerializeField] private Toggle m_toggle;
    private Action<bool> onClickCallback;
    private bool initialized = false;
    public void SetCallback(Action<bool> action)
    {
        if (!initialized)
        {
            initialized = true;
            m_toggle.onValueChanged.AddListener((b) => { onClickCallback?.Invoke(b); });
        }
        onClickCallback = null;
        onClickCallback += action;
    }
    public void SetInteractable(bool interactable)
    {
        m_toggle.interactable = interactable;
    }
    public void SetSelected(bool selected)
    {
        m_toggle.isOn = selected;
    }
}