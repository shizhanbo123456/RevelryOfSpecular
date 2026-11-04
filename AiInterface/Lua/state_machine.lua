-- AI 状态机入口：激活后每帧被调用 on_tick(dt)
-- 可用 API：ai.stand() / ai.move_to(x,z) / ai.attack(id) / ai.stop() / ai.cast_skill(id)
--           ai.signal(type) / ai.signal_at(type,x,z) / ai.interrupt(文本)
--           读状态：ai.self_* / ai.map_* / ai.slot_* / ai.held_* / ai.guard_progress(i) / ai.is_night()

function on_tick(dt)
    -- 默认状态机（站立 / 移动到位置 / 攻击敌人 / 逃跑 / 寻找珊瑚 / 追击珊瑚 / 攻击珊瑚）
    -- 的切换条件与参数在实现时按需补充；当前为空 = 一直站立
end
