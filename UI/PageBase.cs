using UnityEngine.UIElements;

/// <summary>
/// UI 页面基类（UI Toolkit）。
/// 页面 = 一个顶层 VisualElement，由 UIManager 统一挂载与切换（Enter/Exit）。
/// </summary>
public abstract class PageBase
{
    /// <summary>页面根元素（挂载于 UI 根下）。</summary>
    protected VisualElement Root { get; private set; }

    /// <summary>所属 UIManager。</summary>
    protected UIManager Owner { get; private set; }

    /// <summary>初始化：构建页面结构（子类实现）。</summary>
    public void Init(UIManager owner, VisualElement parent)
    {
        Owner = owner;
        Root = new VisualElement { name = GetType().Name };
        Root.style.flexGrow = 1f;
        parent.Add(Root);
        Build(Root);
        Exit();
    }

    /// <summary>子类在此构建页面元素。</summary>
    protected abstract void Build(VisualElement root);

    /// <summary>页面显示。</summary>
    public virtual void Enter()
    {
        if (Root != null) Root.style.display = DisplayStyle.Flex;
    }

    /// <summary>页面隐藏。</summary>
    public virtual void Exit()
    {
        if (Root != null) Root.style.display = DisplayStyle.None;
    }

    /// <summary>每帧刷新（UIManager Update 调用，仅当前页）。</summary>
    public virtual void OnUpdate() { }

    /// <summary>事件订阅（UIManager 页面切换时调用）。</summary>
    public virtual void OnEnable() { }

    /// <summary>事件反订阅。</summary>
    public virtual void OnDisable() { }
}
