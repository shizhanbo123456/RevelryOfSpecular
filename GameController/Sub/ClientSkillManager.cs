using Ros.Transport;

/// <summary>
/// 技能表现子管理器（客户端逻辑）：服务器广播的技能施放，按技能 id 取技能实例并用上下文重建轨迹播放。
/// 客户端技能表现的唯一入口（后续音效、本地预测等都从这里扩展）。
/// </summary>
public class ClientSkillManager : ClientSubManager
{
    /// <summary>接收服务器技能施放广播，用与服务器相同的轨迹构建函数重建表现。</summary>
    public void OnSkillCast(int skillId, SkillContext context)
    {
        if (context == null) return;
        SkillManager.PlayVFX(skillId, context);
    }
}
