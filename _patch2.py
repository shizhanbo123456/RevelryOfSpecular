import io
# 1) BattlePage: 修复已删除常量引用
p='UI/BattlePage.cs'
s=io.open(p,encoding='utf-8').read()
s=s.replace('ShowFloating(string.IsNullOrEmpty(msg) ? Config.right_click_blocked_notice : msg, new Color(1f, 0.6f, 0.2f, 1f));',
            'ShowFloating(string.IsNullOrEmpty(msg) ? "该技能无法在此状态下使用" : msg, new Color(1f, 0.6f, 0.2f, 1f));')
io.open(p,'w',encoding='utf-8',newline='\n').write(s)

# 2) ClientEvent 注释修正
p='GlobalData/ClientEvent.cs'
s=io.open(p,encoding='utf-8').read()
s=s.replace('/// <summary>右键阻断提示（param=string 文案，见 Config.right_click_blocked_notice）。</summary>',
            '/// <summary>右键阻断提示（param=string 文案；新操作方案下由技能/状态校验触发）。</summary>')
io.open(p,'w',encoding='utf-8',newline='\n').write(s)

# 3) EntityData.OnDamaged：守护点受伤计分钩子
p='Entity/EntityComponents/EntityData.cs'
s=io.open(p,encoding='utf-8').read()
s=s.replace("""        finalDamage = Mathf.Max(0f, finalDamage);
        floatingAttribute.health = Mathf.Max(0f, floatingAttribute.health - finalDamage);""","""        finalDamage = Mathf.Max(0f, finalDamage);
        floatingAttribute.health = Mathf.Max(0f, floatingAttribute.health - finalDamage);
        if (type.category == EntityCategory.Beacon)
        {
            Tool.BattleManager?.AddBeaconDamage(finalDamage); // 进攻方得分 = 对守护点造成的总伤害
        }""")
io.open(p,'w',encoding='utf-8',newline='\n').write(s)

# 4) BattleManager：中心守护点被摧毁判负 + 防守方击杀计数
p='GameController/BattleManager.cs'
s=io.open(p,encoding='utf-8').read()
s=s.replace("""    /// <summary>销毁实体（服务器）。</summary>
    public bool DestroyEntity(ushort id)
    {
        if (!EntityContainer.Entities.TryGetObject(id, out var data)) return false;
        RemoveFromContainer(data);
        data.OnDestroyed();
        Destroy(data.gameObject);
        return true;
    }""","""    /// <summary>销毁实体（服务器）。</summary>
    public bool DestroyEntity(ushort id)
    {
        if (!EntityContainer.Entities.TryGetObject(id, out var data)) return false;
        RemoveFromContainer(data);
        data.OnDestroyed();
        bool wasCoreBeacon = data.type.category == EntityCategory.Beacon && data.type == EntityType.CoreBeacon;
        Destroy(data.gameObject);
        if (wasCoreBeacon && BattleStarted)
        {
            EndBattle(1); // 中心守护点被摧毁 → 进攻方必然获胜（策划案 17.2）
        }
        return true;
    }""")
s=s.replace("""            foreach (var entity in killed)
            {
                entity.OnKilled();
                // TODO: 击杀事件/分数/掉落/复活进度开始
                Tool.NetworkManager.SendBattleEvent(SCBattleEvent.Type.Kill, entity.id);""","""            foreach (var entity in killed)
            {
                entity.OnKilled();
                if (entity.camp == EntityCamp.Attack) DefenseKills++; // 防守方击杀数（得分公式用）
                // TODO: 掉落/复活进度开始（死亡即摧毁单位，复活时重建并回满）
                Tool.NetworkManager.SendBattleEvent(SCBattleEvent.Type.Kill, entity.id);""")
io.open(p,'w',encoding='utf-8',newline='\n').write(s)

# 5) SaveManager：升级经验表 + 获得经验=对水晶伤害
p='GameController/SaveManager.cs'
s=io.open(p,encoding='utf-8').read()
s=s.replace("""    /// <summary>给角色加经验（对局结算产出，TODO: 经验公式待设计，当前 1 局 = 100 经验占位）。</summary>
    public void AddCharacterExp(int index, int exp)
    {
        EnsureListSize(index);
        characterExp[index] += exp;
        int level = characterLevels[index];
        // TODO: 升级经验表待设计，当前简单公式：每级所需经验 = 100 * 等级
        while (level < Config.max_entity_level && characterExp[index] >= 100 * level)
        {
            characterExp[index] -= 100 * level;
            level++;
        }
        characterLevels[index] = level;
        Save();
    }""","""    /// <summary>
    /// 给角色加经验（策划案 17.3：获得经验 = 对水晶造成的伤害量；
    /// 升级所需经验表见 Config.level_up_exp，从 1→2 级起依次取用）。
    /// </summary>
    public void AddCharacterExp(int index, int exp)
    {
        EnsureListSize(index);
        characterExp[index] += exp;
        int level = characterLevels[index];
        while (level < Config.max_entity_level)
        {
            int need = Config.level_up_exp[level - 1];
            if (characterExp[index] < need) break;
            characterExp[index] -= need;
            level++;
        }
        characterLevels[index] = level;
        Save();
    }""")
s=s.replace("""    /// <summary>给玩家加经验（账号级，TODO: 升级公式待设计）。</summary>""","""    /// <summary>给玩家加经验（账号级）。</summary>""")
io.open(p,'w',encoding='utf-8',newline='\n').write(s)
print('ok')
