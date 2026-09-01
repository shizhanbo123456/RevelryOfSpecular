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
            style =
            {
                position = Position.Absolute,
                bottom = 40,
                right = 20,
                maxWidth = 500,
                backgroundColor = new Color(0f, 0f, 0f, 0.75f),
                paddingLeft = 14,
                paddingRight = 14,
                paddingTop = 8,
                paddingBottom = 8,
                unityFontStyleAndWeight = FontStyle.Bold,
                display = DisplayStyle.None,
            }
        };
        label = new Label { style = { color = Color.white, fontSize = 16 } };
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
        runner.Start(Hide(duration));
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

        public void Start(IEnumerator routine) => StartCoroutine(routine);
        public void StopAll() => StopAllCoroutines();
    }
}
