using UnityEngine;
using UnityEngine.UI;

public class BattleLabel : MonoBehaviour
{
    [SerializeField] private Text label;

    private RectTransform rectTransform;
    private Color baseColor = Color.white;
    private float createTime;
    private float lifeTime = 1.2f;
    private AnimationCurve alphaCurve;   // 透明度变化（x=进度 0~1，y=alpha）
    private AnimationCurve scaleCurve;   // 缩放变化（x=进度 0~1，y=缩放系数）
    private AnimationCurve riseCurve;    // y 上飘距离（x=进度 0~1，y=上飘高度）

    public RectTransform RectTransform
    {
        get
        {
            if (rectTransform == null) rectTransform = transform as RectTransform;
            return rectTransform;
        }
    }

    public bool Refresh(Vector2 screenPos, float scale)
    {
        float elapsed = Time.time - createTime;
        if (elapsed >= lifeTime) return false;

        float progress = Mathf.Clamp01(elapsed / lifeTime);

        // y 上飘：曲线值乘世界距离缩放（与原匀速上飘语义一致）
        float rise = riseCurve != null ? riseCurve.Evaluate(progress) : 0f;
        RectTransform.anchoredPosition = screenPos + Vector2.up * rise * scale;

        // 缩放：动画缩放系数 × 世界距离缩放
        float s = scaleCurve != null ? scaleCurve.Evaluate(progress) : 1f;
        RectTransform.localScale = Vector3.one * (scale * s);

        if (label != null)
        {
            var color = baseColor;
            color.a = alphaCurve != null ? alphaCurve.Evaluate(progress) : 1f;
            label.color = color;
        }
        return true;
    }

    public bool Refresh(string content, Color color, float startTime, float life,
        AnimationCurve alpha, AnimationCurve scale, AnimationCurve rise, Vector2 screenPos, float scaleFactor)
    {
        baseColor = color;
        createTime = startTime;
        lifeTime = life;
        alphaCurve = alpha;
        scaleCurve = scale;
        riseCurve = rise;
        if (label != null) label.text = content;
        return Refresh(screenPos, scaleFactor);
    }
}
