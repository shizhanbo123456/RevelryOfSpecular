using FairyGUI;
using UnityEngine;

namespace Ros.UI.Main
{
    // 悬浮提示摆位：锚点正上方居中，顶部放不下就改到下方，左右夹在页面内
    internal static class TipLayout
    {
        public static void PlaceAbove(GComponent tip, GObject anchor)
        {
            if (anchor == null) return;
            var root = tip.parent as GComponent;
            if (root == null) return; // 还没挂到页面上，等调用方加完子节点再摆
            Vector2 local = root.GlobalToLocal(anchor.LocalToGlobal(Vector2.zero));
            float x = Mathf.Clamp(local.x + anchor.width * 0.5f - tip.width * 0.5f, 0f, Mathf.Max(0f, root.width - tip.width));
            float y = local.y - tip.height - 8f;
            if (y < 0f) y = local.y + anchor.height + 8f;
            tip.SetXY(x, y);
            tip.visible = true;
        }
    }
}
