using UnityEngine;

/// <summary>
/// 初始界面与组队大厅的角色预览（Sub/HomePreview）：把当前选中的进攻/防守角色图形复制到 LandscapeSpawns
/// 的预览锚点展示，并把主相机摆到预览机位（CameraController 在 lookTarget 为空/已销毁时不驱动相机，摆一次即保持）。
/// 生命周期随页面：HomePage / LobbyPage 的 Enter 调 Refresh 重建，BattlePage 的 Enter 调 Hide 清理
/// （预览模型不带父物体、直接挂在锚点世界坐标，不清会残留在地图上）。
/// </summary>
public class HomePreviewManager : ClientSubManager
{
    private GameObject attackerView;
    private GameObject defenserView;

    /// <summary>刷新预览：按当前选择重建两角色模型并就位相机（锚点未配置时保持现状）。</summary>
    public void Refresh(int attackIndex, int defenseIndex)
    {
        var spawns = Tool.LandscapeSpawns;
        if (spawns == null || spawns.attackerPreviewPos == null
            || spawns.defenserPreviewPos == null || spawns.cameraPreviewPos == null) return;

        PlaceCamera(spawns.cameraPreviewPos);
        attackerView = Swap(attackerView, TryGetGraphic(EntityType.Attack(attackIndex)), spawns.attackerPreviewPos);
        defenserView = Swap(defenserView, TryGetGraphic(EntityType.Defense(defenseIndex)), spawns.defenserPreviewPos);
    }

    /// <summary>销毁预览模型（相机留在原地，随后由 CameraController 接管或由下次 Refresh 重新摆位）。</summary>
    public void Hide()
    {
        if (attackerView != null) Object.Destroy(attackerView);
        if (defenserView != null) Object.Destroy(defenserView);
        attackerView = null;
        defenserView = null;
    }

    private static GameObject TryGetGraphic(EntityType type)
    {
        return Tool.AssetsManager != null && Tool.AssetsManager.TryGetGraphic(type, out var graphic) ? graphic : null;
    }

    private static GameObject Swap(GameObject old, GameObject prefab, Transform anchor)
    {
        if (old != null) Object.Destroy(old);
        if (prefab == null) return null;
        return Object.Instantiate(prefab, anchor.position, anchor.rotation);
    }

    private static void PlaceCamera(Transform anchor)
    {
        var cam = Tool.CameraController;
        if (cam != null) cam.transform.SetPositionAndRotation(anchor.position, anchor.rotation);
    }

    protected override void UnbindEvents() => Hide(); // Dispose 时顺手清理预览模型
}
