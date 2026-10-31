using FairyGUI;
using UnityEngine;

namespace Ros.UI.Main
{
    // 世界层元素（血条 / 飘字）的坐标换算：世界 → 屏幕 → 页面设计像素 → 挂点容器局部
    internal static class WorldLayer
    {
        public static Camera ProjectionCamera
            => Tool.CameraController != null && Tool.CameraController.WorldCamera != null
                ? Tool.CameraController.WorldCamera : Camera.main;

        public static float UiScale => Screen.height / 1080f;

        // Unity 屏幕 y 轴向上（原点左下），FGUI 局部 y 轴向下（原点左上），需翻转；behind = 锚点在相机背后
        public static Vector2 WorldToPanel(Vector3 world, Camera cam, out bool behind)
        {
            var screen = cam.WorldToScreenPoint(world);
            behind = screen.z <= 0f;
            float scale = UiScale;
            return new Vector2(screen.x / scale, (Screen.height - screen.y) / scale);
        }

        // 挂点元素（父节点即世界层容器）的局部坐标
        public static Vector2 ToLayer(GObject child, Vector2 panelPoint)
        {
            var container = child != null ? child.parent as GComponent : null;
            var root = container != null ? RootOf(container) : null;
            if (root == null) return panelPoint;
            return container.GlobalToLocal(root.LocalToGlobal(panelPoint));
        }

        // 页面根 = 直接挂在 GRoot 下的那个面板
        private static GComponent RootOf(GObject o)
        {
            for (GObject cur = o; cur != null; cur = cur.parent)
                if (cur.parent == GRoot.inst) return cur as GComponent;
            return null;
        }
    }
}
