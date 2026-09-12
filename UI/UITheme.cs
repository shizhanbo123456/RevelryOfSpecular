using UnityEngine;
using UnityEngine.UIElements;

/// <summary>
/// UI 统一外观：配色 / 字号 / 圆角 / 常用控件样式。改这里即全局换肤。
/// </summary>
public static class UITheme
{
    #region 配色
    /// <summary>页面底色。</summary>
    public static readonly Color PageBg = new Color(0.06f, 0.06f, 0.09f, 0.98f);
    /// <summary>卡片底色。</summary>
    public static readonly Color CardBg = new Color(0.11f, 0.11f, 0.15f, 0.98f);
    /// <summary>凹陷底色（条形槽 / 列表底）。</summary>
    public static readonly Color SunkenBg = new Color(0.07f, 0.07f, 0.1f, 1f);
    /// <summary>输入控件底色（必须同时用于内层 unity-text-input）。</summary>
    public static readonly Color FieldBg = new Color(0.14f, 0.14f, 0.19f, 1f);
    /// <summary>常规描边。</summary>
    public static readonly Color Border = new Color(0.24f, 0.24f, 0.31f, 1f);
    /// <summary>强调描边。</summary>
    public static readonly Color BorderStrong = new Color(0.42f, 0.42f, 0.5f, 1f);

    public static readonly Color TextMain = new Color(0.94f, 0.94f, 0.97f, 1f);
    public static readonly Color TextDim = new Color(0.68f, 0.68f, 0.76f, 1f);
    public static readonly Color TextFaint = new Color(0.48f, 0.48f, 0.56f, 1f);

    /// <summary>品牌强调色。</summary>
    public static readonly Color Accent = new Color(0.62f, 0.46f, 0.95f, 1f);
    /// <summary>进攻方（红）。</summary>
    public static readonly Color Attack = new Color(1f, 0.45f, 0.4f, 1f);
    /// <summary>防守方（蓝）。</summary>
    public static readonly Color Defense = new Color(0.4f, 0.72f, 1f, 1f);
    public static readonly Color Gold = new Color(1f, 0.82f, 0.4f, 1f);
    public static readonly Color Green = new Color(0.42f, 0.85f, 0.55f, 1f);
    public static readonly Color Warn = new Color(1f, 0.62f, 0.28f, 1f);
    public static readonly Color Danger = new Color(1f, 0.32f, 0.3f, 1f);

    public static readonly Color BtnBg = new Color(0.2f, 0.2f, 0.26f, 1f);
    public static readonly Color BtnHover = new Color(0.28f, 0.28f, 0.36f, 1f);
    public static readonly Color BtnPressed = new Color(0.15f, 0.15f, 0.2f, 1f);
    public static readonly Color BtnPrimary = new Color(0.44f, 0.31f, 0.78f, 1f);
    public static readonly Color BtnPrimaryHover = new Color(0.53f, 0.38f, 0.9f, 1f);
    public static readonly Color BtnPrimaryPressed = new Color(0.35f, 0.24f, 0.63f, 1f);
    public static readonly Color BtnDisabled = new Color(0.13f, 0.13f, 0.16f, 1f);

    /// <summary>HUD 条（顶栏 / 底栏）底色。</summary>
    public static readonly Color BarBg = new Color(0f, 0f, 0f, 0.45f);
    /// <summary>技能槽底色 / 选中底色。</summary>
    public static readonly Color SlotBg = new Color(0.15f, 0.15f, 0.2f, 0.95f);
    public static readonly Color SlotSelectedBg = new Color(0.28f, 0.21f, 0.44f, 0.98f);
    /// <summary>技能 CD 遮罩。</summary>
    public static readonly Color CdMask = new Color(0f, 0f, 0f, 0.65f);
    #endregion

    #region 尺寸
    public const int FontTitle = 28;
    public const int FontHeading = 20;
    public const int FontBody = 15;
    public const int FontSmall = 13;
    public const int FontTiny = 12;

    public const float Radius = 8f;
    public const float RadiusSmall = 5f;
    public const float BtnHeight = 36f;
    #endregion

    #region 元素
    /// <summary>页面容器：铺满 + 底色 + 统一内边距。</summary>
    public static VisualElement Page()
    {
        var page = new VisualElement { style = { flexGrow = 1 } };
        page.style.backgroundColor = PageBg;
        page.style.paddingLeft = 36;
        page.style.paddingRight = 36;
        page.style.paddingTop = 24;
        page.style.paddingBottom = 24;
        return page;
    }

    /// <summary>卡片：底色 + 描边 + 圆角 + 内边距。</summary>
    public static VisualElement Card(float width = 0f, float padding = 14f)
    {
        var card = new VisualElement();
        card.style.backgroundColor = CardBg;
        SetBorder(card, 1f, Border);
        SetRadius(card, Radius);
        SetPadding(card, padding);
        if (width > 0f) card.style.width = width;
        return card;
    }

    /// <summary>全屏遮罩（绝对铺满 + 居中，默认隐藏）。</summary>
    public static VisualElement Overlay(float alpha = 0.55f)
    {
        var overlay = new VisualElement
        {
            style =
            {
                position = Position.Absolute,
                left = 0, right = 0, top = 0, bottom = 0,
                alignItems = Align.Center,
                justifyContent = Justify.Center,
                display = DisplayStyle.None,
            }
        };
        overlay.style.backgroundColor = new Color(0f, 0f, 0f, alpha);
        return overlay;
    }

    /// <summary>文字。</summary>
    public static Label Text(string text, Color color, int fontSize = FontBody, bool bold = false)
    {
        var label = new Label(text);
        Apply(label, color, fontSize, bold);
        return label;
    }

    /// <summary>文字样式（Label 与 Button 都是 TextElement）。</summary>
    public static void Apply(TextElement element, Color color, int fontSize = FontBody, bool bold = false)
    {
        element.style.color = color;
        element.style.fontSize = fontSize;
        element.style.whiteSpace = WhiteSpace.Normal;
        if (bold) element.style.unityFontStyleAndWeight = FontStyle.Bold;
    }

    /// <summary>页面主标题。</summary>
    public static Label Title(string text) => Text(text, TextMain, FontTitle, true);
    /// <summary>页面副标题。</summary>
    public static Label Subtitle(string text) => Text(text, TextDim, FontBody);
    /// <summary>分组小标题。</summary>
    public static Label Section(string text) => Text(text, Gold, FontBody, true);

    /// <summary>进度条外槽（圆角 + 裁剪）。</summary>
    public static VisualElement BarTrack(float height = 10f)
    {
        var track = new VisualElement { style = { height = height } };
        track.style.backgroundColor = SunkenBg;
        track.style.overflow = Overflow.Hidden;
        SetRadius(track, height * 0.5f);
        return track;
    }

    /// <summary>进度条填充（宽度由调用方设置）。</summary>
    public static VisualElement BarFill(Color color, float height = 10f)
    {
        var fill = new VisualElement { style = { height = height, width = Length.Percent(100f) } };
        fill.style.backgroundColor = color;
        fill.pickingMode = PickingMode.Ignore;
        return fill;
    }
    #endregion

    #region 控件
    /// <summary>按钮：统一尺寸/圆角/描边，并自带悬停与按下反馈。</summary>
    public static Button StyleButton(Button button, bool primary = false, float width = 0f, float height = BtnHeight)
    {
        var skin = new ButtonSkin(primary);
        button.userData = skin;
        button.style.height = height;
        if (width > 0f) button.style.width = width;
        button.style.paddingLeft = 16;
        button.style.paddingRight = 16;
        button.style.unityTextAlign = TextAnchor.MiddleCenter;
        button.style.justifyContent = Justify.Center;
        SetRadius(button, RadiusSmall);
        SetBorder(button, 1f, skin.Border);
        Apply(button, TextMain, FontBody);
        ApplySkin(button, skin.Normal);
        button.RegisterCallback<PointerEnterEvent>(_ => { if (button.enabledSelf) ApplySkin(button, skin.Hover); });
        button.RegisterCallback<PointerLeaveEvent>(_ => ApplySkin(button, button.enabledSelf ? skin.Normal : skin.Disabled));
        button.RegisterCallback<PointerDownEvent>(_ => { if (button.enabledSelf) ApplySkin(button, skin.Pressed); });
        button.RegisterCallback<PointerUpEvent>(_ => ApplySkin(button, button.enabledSelf ? skin.Hover : skin.Disabled));
        return button;
    }

    /// <summary>列表项按钮（整行、左对齐，选中用强调色）。</summary>
    public static Button StyleListItem(Button button, bool selected, float height = 34f)
    {
        StyleButton(button, selected, 0f, height);
        button.style.unityTextAlign = TextAnchor.MiddleLeft;
        button.style.justifyContent = Justify.FlexStart;
        button.style.marginBottom = 4;
        return button;
    }

    /// <summary>启用/禁用按钮（内联样式会压过主题的禁用态，故需同步换色）。</summary>
    public static void SetButtonEnabled(Button button, bool enabled)
    {
        button.SetEnabled(enabled);
        if (button.userData is ButtonSkin skin) ApplySkin(button, enabled ? skin.Normal : skin.Disabled);
    }

    /// <summary>输入框：配色必须作用在内层 unity-text-input，否则会被内层背景盖住。</summary>
    public static TextField StyleField(TextField field, float width = 0f)
    {
        StyleFieldBase(field, width);
        return field;
    }

    /// <summary>整数输入框（同上）。</summary>
    public static IntegerField StyleField(IntegerField field, float width = 0f)
    {
        StyleFieldBase(field, width);
        return field;
    }

    /// <summary>开关：统一文字样式（勾选框外观仍由主题绘制）。</summary>
    public static Toggle StyleToggle(Toggle toggle)
    {
        var label = toggle.Q<Label>();
        if (label != null) Apply(label, TextMain, FontBody);
        return toggle;
    }
    #endregion

    #region 基础属性
    /// <summary>四边圆角。</summary>
    public static void SetRadius(VisualElement element, float radius)
    {
        element.style.borderTopLeftRadius = radius;
        element.style.borderTopRightRadius = radius;
        element.style.borderBottomLeftRadius = radius;
        element.style.borderBottomRightRadius = radius;
    }

    /// <summary>四边描边。</summary>
    public static void SetBorder(VisualElement element, float width, Color color)
    {
        element.style.borderTopWidth = element.style.borderBottomWidth = element.style.borderLeftWidth = element.style.borderRightWidth = width;
        element.style.borderTopColor = element.style.borderBottomColor = element.style.borderLeftColor = element.style.borderRightColor = color;
    }

    /// <summary>四边等距内边距。</summary>
    public static void SetPadding(VisualElement element, float pad)
    {
        element.style.paddingLeft = element.style.paddingRight = element.style.paddingTop = element.style.paddingBottom = pad;
    }
    #endregion

    #region//Local
    private static void StyleFieldBase(VisualElement field, float width)
    {
        field.style.height = 32f;
        if (width > 0f) field.style.width = width;
        field.style.backgroundColor = FieldBg;
        SetBorder(field, 1f, Border);
        SetRadius(field, RadiusSmall);

        var input = field.Q("unity-text-input") ?? field.Q<TextElement>();
        if (input == null) return;
        input.style.backgroundColor = FieldBg;
        input.style.color = TextMain;
        input.style.fontSize = FontBody;
        input.style.flexGrow = 1f;
        input.style.paddingLeft = 8f;
        input.style.paddingRight = 8f;
    }

    private static void ApplySkin(Button button, Color color)
    {
        button.style.backgroundColor = color;
        button.style.color = button.enabledSelf ? TextMain : TextFaint;
    }

    /// <summary>按钮配色组合。</summary>
    private sealed class ButtonSkin
    {
        public readonly Color Normal;
        public readonly Color Hover;
        public readonly Color Pressed;
        public readonly Color Disabled;
        public readonly Color Border;

        public ButtonSkin(bool primary)
        {
            Normal = primary ? BtnPrimary : BtnBg;
            Hover = primary ? BtnPrimaryHover : BtnHover;
            Pressed = primary ? BtnPrimaryPressed : BtnPressed;
            Disabled = BtnDisabled;
            Border = primary ? Accent : UITheme.Border;
        }
    }
    #endregion
}
