-- 默认 Lua 状态机：站立 / 移动到位置 / 攻击敌人 / 逃跑 / 寻找珊瑚 / 追击珊瑚 / 攻击珊瑚
-- 感知来源 = 小地图（雷达半径内）；参数为暂定值，实测后调整
-- 攻击意图每拍重发是「持续出招」的节拍；移动/站立同值由 C# 侧去重，不刷包

local CHASE_RANGE = 12        -- 追击阈值：敌人进入该距离转入攻击
local BREAK_RANGE = 18        -- 脱离阈值：攻击/逃跑中目标拉远到该距离就放弃
local AGGRESSION = 0.5        -- 攻击性 0~1：越大越倾向攻击，越小越倾向逃跑
local ATTACK_RANGE = 8        -- 认为够得着目标（按主技能射程暂定，超距会空放）
local PULSE = 0.2             -- 意图重发节拍（相当于人机的决策间隔）

local CAMP_ATTACK, CAMP_DEFENSE, CAMP_NEUTRAL = 1, 2, 4
local CAT_CORAL = 5

local state = "idle"
local clock = 0

local function my_camp()
    return ai.self_camp() == "Attack" and CAMP_ATTACK or CAMP_DEFENSE
end

-- 小地图一圈：敌人 = 与我敌对的非中立单位；珊瑚单独收集
local function scan()
    local enemies, corals = {}, {}
    local mine = my_camp()
    for i = 0, ai.map_count() - 1 do
        if ai.map_cat(i) == CAT_CORAL then
            corals[#corals + 1] = { id = ai.map_id(i), x = ai.map_x(i), z = ai.map_z(i) }
        else
            local camp = ai.map_camp(i)
            if camp ~= mine and camp ~= CAMP_NEUTRAL then
                enemies[#enemies + 1] = { id = ai.map_id(i), x = ai.map_x(i), z = ai.map_z(i) }
            end
        end
    end
    return enemies, corals
end

local function nearest(list)
    local sx, sz = ai.self_x(), ai.self_z()
    local best, dist
    for _, e in ipairs(list) do
        local dx, dz = e.x - sx, e.z - sz
        local d = math.sqrt(dx * dx + dz * dz)
        if not dist or d < dist then best, dist = e, d end
    end
    return best, dist or math.huge
end

-- 朝背离敌人的方向撤到脱离距离外
local function flee_from(ex, ez)
    local sx, sz = ai.self_x(), ai.self_z()
    local dx, dz = sx - ex, sz - ez
    local len = math.sqrt(dx * dx + dz * dz)
    if len < 0.01 then dx, dz, len = 1, 0, 1 end
    ai.move_to(sx + dx / len * BREAK_RANGE, sz + dz / len * BREAK_RANGE)
end

function on_tick(dt)
    clock = clock + dt
    if clock < PULSE then return end
    clock = 0

    -- 未联网或没有战斗实体：不发任何意图
    if not ai.connected() or ai.self_id() == 0 then return end

    local enemies, corals = scan()
    local enemy, enemyDist = nearest(enemies)
    local coral, coralDist = nearest(corals)

    -- 逃跑：血量跌破攻击性允许线且附近有敌人；拉开到脱离距离外或脱战即结束
    local fleeHp = 1 - AGGRESSION * 0.7
    local lowHp = ai.self_max_hp() > 0 and ai.self_hp() / ai.self_max_hp() < fleeHp
    if state == "flee" then
        if enemy == nil or enemyDist >= BREAK_RANGE or not lowHp then
            state = "idle"
        else
            flee_from(enemy.x, enemy.z)
            return
        end
    elseif lowHp and enemy ~= nil then
        state = "flee"
        flee_from(enemy.x, enemy.z)
        return
    end

    -- 攻击敌人：进入追击阈值就打（先贴近到攻击距离），拉出到脱离阈值回站立
    if enemy ~= nil then
        if enemyDist <= CHASE_RANGE then
            state = "attack"
            if enemyDist <= ATTACK_RANGE then
                ai.attack(enemy.id)
            else
                ai.move_to(enemy.x, enemy.z)
            end
            return
        end
        if state == "attack" then state = "idle" end
    elseif state == "attack" then
        state = "idle"
    end

    -- 珊瑚链：视野内有珊瑚就追过去打（珊瑚是顺手收益，来了真敌人上面已优先）
    if coral ~= nil then
        if coralDist <= ATTACK_RANGE then
            state = "attack_coral"
            ai.attack(coral.id)
        else
            state = "chase_coral"
            ai.move_to(coral.x, coral.z)
        end
        return
    end
    if state == "attack_coral" or state == "chase_coral" then state = "idle" end

    -- 站立（移动到位置由命令行直接下发，状态机激活期间会被本节拍覆盖）
    ai.stand()
end
