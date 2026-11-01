using FairyGUI;
using UnityEngine;

namespace Ros.UI.Main
{
    // 伤害飘字：单个实例管自己的页码、散布、上升与淡出，到期自毁
    public partial class UI_DamageLabel
    {
        private const float Life = 0.8f;
        private const int PageCount = 12;              // 与控制器页数一致，枚举加值须同步加页
        private const float RisePerSecond = 60f;       // 存活期内每秒上升的设计像素
        private const float ScatterX = 50f;            // 屏幕空间散布：世界空间散布远则仍叠、近则偏移过大
        private const float ScatterY = 10f;

        private Vector3 anchor;
        private Vector2 scatterPx;
        private float risePx;
        private float defaultSizeDistance; // 飘字 scale=(1,1) 时的相机距离
        private Timer.TransitionHandle transition;

        // 生成一个飘字；类型越界属枚举与 FGUI 页数不匹配，直接丢弃并报错
        public static void Spawn(UI_empty_container layer, int value, CombatLabelType type, Vector3 anchor, float defaultSizeDistance)
        {
            if (layer == null) return;
            int page = (int)type;
            if (page < 0 || page >= PageCount)
            {
                Debug.LogError($"[战斗飘字] 未处理的飘字类型：{page}");
                return;
            }
            // 相机背后的点投影后 x/y 会镜像翻转，显示出来就是屏幕上"莫名其妙的位置"
            var cam = WorldLayer.ProjectionCamera;
            if (cam != null && cam.WorldToScreenPoint(anchor).z <= 0f) return;

            var label = CreateInstance();
            label.m_type.selectedIndex = page; // 枚举值即 FGUI 页号
            // 闪避 / 失效页不写数字，其余页统一写下发数值
            if (type != CombatLabelType.Evade && type != CombatLabelType.Invalid) label.m_num_common.text = value.ToString();
            layer.AddChild(label); // 挂在世界层：压在战斗界面各元素之下

            label.anchor = anchor;
            label.defaultSizeDistance = defaultSizeDistance;
            label.scatterPx = new Vector2(Random.Range(-ScatterX, ScatterX), Random.Range(-ScatterY, ScatterY));
            label.ApplyPosition();
            label.Play();
            if (Tool.CameraController != null) Tool.CameraController.OnCameraUpdated += label.OnCameraUpdated;
        }

        // 出生点与相机同步：相机插值收尾时用最终变换重投影，消除与渲染差一帧的震颤
        private void OnCameraUpdated() => ApplyPosition();

        private void ApplyPosition()
        {
            var cam = WorldLayer.ProjectionCamera;
            if (cam == null) return;
            if (cam.WorldToScreenPoint(anchor).z <= 0f)
            {
                visible = false;
                return;
            }
            visible = true;
            // 飘字画在屏幕面板上，不随距离自然缩小：与血条同口径按距离倒比缩放，默认距离处为 (1,1)
            float distance = Vector3.Distance(cam.transform.position, anchor);
            float scale = defaultSizeDistance / Mathf.Max(distance, 0.01f);
            SetScale(scale, scale);
            // 轴心左上角，减半个缩放后宽度让飘字正中落在锚点上；散布与上升量随缩放同比
            xy = WorldLayer.ToLayer(this, WorldLayer.WorldToPanel(anchor, cam, out _) + scatterPx * scale)
                - new Vector2(width * 0.5f * scale, 0f) + new Vector2(0f, -risePx * scale);
        }

        private void Play()
        {
            transition = Timer.AddTransition(0f, Life, (_, t01) =>
            {
                float age = t01 * Life;
                risePx = RisePerSecond * age;
                // 前半程完全不透明，后半程线性淡出
                alpha = age <= Life * 0.5f ? 1f : Mathf.Clamp01(1f - (age - Life * 0.5f) / (Life * 0.5f));
                if (t01 >= 1f) Release();
            });
        }

        private void Release()
        {
            if (Tool.CameraController != null) Tool.CameraController.OnCameraUpdated -= OnCameraUpdated;
            transition = null; // 自身已结束，避免在回调里取消自己
            Dispose();
        }
    }
}
