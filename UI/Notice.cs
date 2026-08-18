using UnityEngine;
using UnityEngine.UI;
using System.Collections;

public class Notice : MonoBehaviour
{
    [SerializeField] private Transform MiddleTemplate;
    [SerializeField] private Transform SideTemplate;

    public void Init()
    {
        // 初始化隐藏模板本体
        MiddleTemplate.gameObject.SetActive(false);
        SideTemplate.gameObject.SetActive(false);
        gameObject.SetActive(true);

        EventManager.AddEvent<string>(ClientEvent.ShowNotice, ShowNotice);
        EventManager.AddEvent<string>(ClientEvent.ShowSideNotice, ShowSideNotice);
    }

    /// <summary>
    /// 中央弹窗提示：原地出现，向上飘，2秒销毁
    /// </summary>
    public void ShowNotice(string msg)
    {
        // 实例化，和模板同父级
        Transform instance = Instantiate(MiddleTemplate, MiddleTemplate.parent);
        instance.gameObject.SetActive(true);

        // 设置文本
        Text txt = GetMiddleTemplateText(instance);
        txt.text = msg;

        // 动画：向上浮动，2秒后销毁
        StartCoroutine(MiddleAnim(instance));

        // ===== LeanTween 版本（有LeanTween插件用这段，注释协程即可）=====
        // float targetY = instance.localPosition.y + 120f; // 上飘距离可自行调整
        // LeanTween.moveLocalY(instance.gameObject, targetY, 2f).setOnComplete(() => Destroy(instance.gameObject));
    }

    /// <summary>
    /// 侧边网格提示：GridLayout自动排列，1s后渐变透明，总时长2s销毁
    /// </summary>
    public void ShowSideNotice(string msg)
    {
        // 实例化，和侧边模板同父级
        Transform instance = Instantiate(SideTemplate, SideTemplate.parent);
        instance.gameObject.SetActive(true);

        // 设置文本
        Text txt = GetSideTemplateText(instance);
        txt.text = msg;

        // 获取CanvasGroup用于透明度渐变
        CanvasGroup cg = instance.GetComponent<CanvasGroup>();
        if (cg == null)
        {
            Debug.LogWarning("侧边模板缺少CanvasGroup组件！自动添加");
            cg = instance.gameObject.AddComponent<CanvasGroup>();
        }
        cg.alpha = 1f;

        StartCoroutine(SideFadeAnim(instance, cg));

        // ===== LeanTween 版本（有LeanTween插件用这段，注释协程即可）=====
        // LeanTween.delayedCall(1f, () =>
        // {
        //     LeanTween.alpha(instance.gameObject, 0f, 1f).setOnComplete(() => Destroy(instance.gameObject));
        // });
    }

    #region 纯协程动画（无需第三方插件，默认启用）
    /// 中央提示上浮协程，总时长2秒
    private IEnumerator MiddleAnim(Transform target)
    {
        float duration = 2f;
        float moveRange = 120f; // 向上飘动距离，按需修改
        Vector2 startPos = target.localPosition;
        float timer = 0;

        while (timer < duration)
        {
            timer += Time.deltaTime;
            float t = timer / duration;
            // 线性向上移动，可换成平滑曲线 Mathf.SmoothStep(0,1,t)
            float offsetY = Mathf.Lerp(0, moveRange, t);
            target.localPosition = new Vector3(startPos.x, startPos.y + offsetY, 0);
            yield return null;
        }

        Destroy(target.gameObject);
    }

    /// 侧边渐变协程：等待1秒 → 1秒渐变透明，总时长2秒销毁
    private IEnumerator SideFadeAnim(Transform target, CanvasGroup cg)
    {
        // 先等待1秒
        yield return new WaitForSeconds(1f);

        float fadeDuration = 1f;
        float timer = 0;
        while (timer < fadeDuration)
        {
            timer += Time.deltaTime;
            cg.alpha = Mathf.Lerp(1f, 0f, timer / fadeDuration);
            yield return null;
        }

        cg.alpha = 0f;
        Destroy(target.gameObject);
    }
    #endregion

    // 你原有文本获取方法保留不变
    private Text GetMiddleTemplateText(Transform t)
    {
        return t.GetChild(3).GetComponent<Text>();
    }

    private Text GetSideTemplateText(Transform t)
    {
        return t.GetChild(2).GetComponent<Text>();
    }
}
