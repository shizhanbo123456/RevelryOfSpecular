using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UIElements;

/// <summary>
/// 通用提示消息（右下角，自动消失）。
/// </summary>
public class NoticeUnit
{
    private VisualElement root;
    private Label label;
    private CoroutineRunner runner;

    /// <summary>初始化并挂载到 UI 根。</summary>
    public void Init(VisualElement parent)
    {
        var runnerGo = new GameObject("NoticeCoroutineRunner");
        runner = runnerGo.AddComponent<CoroutineRunner>();

        root = new VisualElement
        {
            style = { position = Position.Absolute, bottom = 44, right = 24, maxWidth = 460, display = DisplayStyle.None }
        };
        root.style.backgroundColor = UITheme.CardBg;
        UITheme.SetBorder(root, 1f, UITheme.Accent);
        UITheme.SetRadius(root, UITheme.Radius);
        UITheme.SetPadding(root, 12f);

        label = UITheme.Text("", UITheme.TextMain, 16, true);
        root.Add(label);
        parent.Add(root);
    }

    /// <summary>显示消息（默认 2.5 秒后消失）。</summary>
    public void Show(string msg, float duration = 2.5f)
    {
        if (root == null || label == null) return;
        label.text = msg;
        root.style.display = DisplayStyle.Flex;
        runner.StopAll();
        runner.Run(Hide(duration));
    }

    private IEnumerator Hide(float duration)
    {
        yield return new WaitForSeconds(duration);
        root.style.display = DisplayStyle.None;
    }

    /// <summary>立即隐藏。</summary>
    public void Hide()
    {
        runner.StopAll();
        if (root != null) root.style.display = DisplayStyle.None;
    }

    /// <summary>场景中的协程载体（避免依赖外部 MonoBehaviour）。</summary>
    private class CoroutineRunner : MonoBehaviour
    {
        private void Awake()
        {
            DontDestroyOnLoad(gameObject);
            gameObject.hideFlags = HideFlags.HideAndDontSave;
        }

        //不能叫 Start：带参的 Start 会触发 Unity 报错 Start() can not take parameters
        public void Run(IEnumerator routine) => StartCoroutine(routine);
        public void StopAll() => StopAllCoroutines();
    }
}
