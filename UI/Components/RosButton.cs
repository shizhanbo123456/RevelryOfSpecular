using System;
using UnityEngine;
using UnityEngine.UI;

//调用Set添加回调、设置是否可交互
public class RosButton : MonoBehaviour
{
    [SerializeField] private Button m_button;
    private Action onClickCallback;
    private bool initialized = false;
    public void SetCallback(Action action)
    {
        if (!initialized)
        {
            initialized = true;
            m_button.onClick.AddListener(() => { onClickCallback?.Invoke(); });
        }
        onClickCallback = null;
        onClickCallback += action;
    }
    public void SetInteractable(bool interactable)
    {
        m_button.interactable = interactable;
    }
}