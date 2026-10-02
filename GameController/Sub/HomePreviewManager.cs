using UnityEngine;

public class HomePreviewManager : ClientSubManager
{
    private GameObject attackerView;
    private GameObject defenserView;

    public void Refresh(int attackIndex, int defenseIndex)
    {
        var spawns = Tool.LandscapeSpawns;
        if (spawns == null || spawns.attackerPreviewPos == null
            || spawns.defenserPreviewPos == null || spawns.cameraPreviewPos == null) return;

        PlaceCamera(spawns.cameraPreviewPos);
        attackerView = Swap(attackerView, TryGetGraphic(EntityType.Attack(attackIndex)), spawns.attackerPreviewPos);
        defenserView = Swap(defenserView, TryGetGraphic(EntityType.Defense(defenseIndex)), spawns.defenserPreviewPos);
    }

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
