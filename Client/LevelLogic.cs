using Ros.Transport;
using System.Collections.Generic;
using UnityEngine;

public partial class BattleManager
{
    public const ushort maxDamage = 10000;
    private static LevelInfo levelInfo;
    private static InteractablePropInfo interactablePropInfo;
    private static class Chunk
    {
        public static int x;
        public static int y;
        public static Vector2Int index => new Vector2Int(x, y);
        public static bool valid => IsValid(index);
        public static Vector2 center => Landscape.chunkSize * new Vector2(0.5f + x, 0.5f + y);
        public static float infectionValue
        {
            get => _infectionValue[x, y];
            set => _infectionValue[x, y] = Mathf.Clamp(value, Config.infection_min_value, Config.infection_max_value);
        }
        public static float infectionGrowth
        {
            get => _infectionGrowth[x, y];
            set => _infectionGrowth[x, y] = value;
        }
        public static bool randomInfection => Random.value < NormalizedInfection(infectionValue);
        private static float[,] _infectionValue = new float[Landscape.chunkCount, Landscape.chunkCount];
        private static float[,] _infectionGrowth = new float[Landscape.chunkCount, Landscape.chunkCount];

        public static bool TrySet(Vector2Int chunk)
        {
            if (!IsValid(chunk)) return false;
            x = chunk.x;
            y = chunk.y;
            return true;
        }

        public static bool TrySet(Vector3 worldPos)
        {
            return TrySet(ChunkSearcher<int>.GetChunkIndex(worldPos));
        }

        public static bool IsValid(Vector2Int chunk)
        {
            if (chunk.x < 0 || chunk.x >= Landscape.chunkCount ||
                chunk.y < 0 || chunk.y >= Landscape.chunkCount)
            {
                return false;
            }
            return Landscape.instance == null || Landscape.instance._validChunkCache.Contains(chunk);
        }

        public static float GetInfectionValue(Vector2Int chunk)
        {
            return IsValid(chunk) ? _infectionValue[chunk.x, chunk.y] : Config.infection_min_value;
        }

        public static void AddInfection(Vector2Int chunk, float delta)
        {
            if (!TrySet(chunk)) return;
            infectionValue += delta;
        }

        public static float NormalizedInfection(float value)
        {
            return Mathf.InverseLerp(Config.infection_min_value, Config.infection_max_value, value);
        }
    }

    protected override void _Start()
    {
        levelInfo = new LevelInfo();
        if (Landscape.instance != null)
        {
            InitLevelLogic();
        }
        else
        {
            Debug.LogError("无地形数据，测试环境可忽略");
        }
    }

    //撤离->印记
    //可拾取角色道具->角色道具
    //击杀玩家->击杀印记、经验
    //击杀->笔记、经验
    //max撤离：3*10印记，角色道具、击杀印记、笔记、经验
    //普通撤离：2*8印记，角色道具、笔记、经验
    //min撤离：1*5印记，笔记、经验
    //迷失：经验
    private Dictionary<Vector3, EntityData> spawnPlant = new();
    private Dictionary<Vector3, EntityData> spawnOre = new();
    private Dictionary<Vector3, EntityData> spawnInfection = new();
    private Dictionary<Vector3, float> spawnPlantCd = new();
    private Dictionary<Vector3, float> spawnOreCd = new();
    private Dictionary<Vector3, float> spawnInfectionCd = new();
    private static readonly HashSet<EntityData> chunkNpcBuffer = new();

    public static float GetChunkInfection(Vector3 worldPos)
    {
        var chunk = ChunkSearcher<int>.GetChunkIndex(worldPos);
        return Chunk.GetInfectionValue(chunk);
    }

    public static void AddChunkInfection(Vector3 worldPos, float delta)
    {
        var chunk = ChunkSearcher<int>.GetChunkIndex(worldPos);
        Chunk.AddInfection(chunk, delta);
    }

    public static int GetZombieCountInChunk(Vector2Int chunk)
    {
        chunkNpcBuffer.Clear();
        EntityContainer.Npcs.GetObjectsInChunk(chunk, chunkNpcBuffer);
        int count = chunkNpcBuffer.Count;
        chunkNpcBuffer.Clear();
        return count;
    }

    public static bool TryGetZombieMigrationTarget(Vector3 from, out Vector3 target)
    {
        target = from;
        var origin = ChunkSearcher<int>.GetChunkIndex(from);
        if (!Chunk.IsValid(origin)) return false;

        Vector2Int best = origin;
        float bestWeight = float.MinValue;
        for (int dx = -1; dx <= 1; dx++)
        {
            for (int dy = -1; dy <= 1; dy++)
            {
                if (dx == 0 && dy == 0) continue;
                var next = new Vector2Int(origin.x + dx, origin.y + dy);
                if (!Chunk.IsValid(next)) continue;
                float weight = Chunk.GetInfectionValue(next) - GetZombieCountInChunk(next);
                if (weight <= bestWeight) continue;
                bestWeight = weight;
                best = next;
            }
        }

        if (best == origin) return false;
        var center = Landscape.chunkSize * new Vector2(best.x + 0.5f, best.y + 0.5f);
        float jitter = Landscape.chunkSize * 0.35f;
        target = new Vector3(
            center.x + Random.Range(-jitter, jitter),
            from.y,
            center.y + Random.Range(-jitter, jitter));
        return true;
    }

    private static int LevelByInfection(float infectionValue, float rate)
    {
        return Mathf.Clamp(
            Mathf.RoundToInt(infectionValue * rate) + Config.min_entity_level,
            Config.min_entity_level,
            Config.max_entity_level);
    }

    private static bool ShouldSpawnInfectedResource(float infectionValue)
    {
        if (infectionValue < Config.infected_resource_min_infection) return false;
        if (infectionValue >= Config.infected_resource_full_infection) return true;
        float chance = Mathf.InverseLerp(
            Config.infected_resource_min_infection,
            Config.infected_resource_full_infection,
            infectionValue);
        return Random.value < chance;
    }

    private static bool RollByInfection(float infectionValue, float disappear, float appear)
    {
        if (Mathf.Approximately(disappear, appear)) return infectionValue >= appear;
        float chance = Mathf.InverseLerp(disappear, appear, infectionValue);
        return Random.value < Mathf.Clamp01(chance);
    }
    private void InitLevelLogic()
    {
        System.DateTime start= System.DateTime.Now;
        Landscape.instance.RefreshChunkCache();

        float max = -99999;
        float min = 9999999;
        foreach(var v in Landscape.instance._validChunkCache)
        {
            Chunk.x = v.x;
            Chunk.y = v.y;
            Vector2 chunkCenter = Chunk.center;
            foreach (var t in Landscape.instance.ExitPortList)
            {
                float factor = Landscape.chunkSize / (Mathf.Abs(chunkCenter.x - t.position.x) + Mathf.Abs(chunkCenter.y - t.position.z) + Landscape.chunkSize);
                Chunk.infectionGrowth += 60f * factor;
            }
            if (Chunk.infectionGrowth < min) min = Chunk.infectionGrowth;
            if (Chunk.infectionGrowth > max) max = Chunk.infectionGrowth;
        }
        float dmax = Config.infection_converge_target + 35f;
        float dmin = Config.infection_converge_target - 35f;
        float range = max - min;
        float k = range <= 0.0001f ? 0f : (dmax - dmin) / range;
        float b = range <= 0.0001f ? Config.infection_converge_target : dmax - k * max;
        foreach (var v in Landscape.instance._validChunkCache)
        {
            Chunk.infectionGrowth=Chunk.infectionGrowth * k + b;
            Chunk.infectionValue = Chunk.infectionGrowth;
        }

        foreach(var t in Landscape.instance.PlantSpawnAnchors)
        {
            spawnPlant.Add(t, null);
            spawnPlantCd.Add(t, 0f);
        }
        foreach (var t in Landscape.instance.OreSpawnAnchors)
        {
            spawnOre.Add(t, null);
            spawnOreCd.Add(t, 0f);
        }
        foreach (var t in Landscape.instance.InfectionSpawnAnchors)
        {
            spawnInfection.Add(t, null);
            spawnInfectionCd.Add(t, 0f);
        }
        interactablePropInfo = RandomInteractableProp();

        var span=System.DateTime.Now - start;
        Debug.Log($"Level logic initialized! Time used for: {span.TotalMilliseconds} ms");
    }
    private float timeLock_infectionValueGrow = 0f;
    private float timeLock_portRefresh = 0f;
    private float timeLock_spawnZombies = 0f;
    private float timeLock_spawnPlants = 0f;
    private float timeLock_spawnOres = 0f;
    private float timeLock_spawnInfections = 0f;
    private void GameLogic()
    {
        if (Landscape.instance == null) return;

        timeLock_infectionValueGrow += Time.deltaTime;
        if (timeLock_infectionValueGrow > Config.infection_update_interval)
        {
            timeLock_infectionValueGrow = 0;
            float totalInfection = 0f;
            foreach (var v in Landscape.instance._validChunkCache)
            {
                Chunk.x = v.x;
                Chunk.y = v.y;
                float next = Chunk.infectionValue;
                if (next < Config.infection_converge_target)
                {
                    // 策划案：感染<100 每轮 +2（不收敛到 100，自然震荡，仅受硬边界 [10,300] 限制）
                    next += Config.infection_converge_up_delta;
                }
                else if (next > Config.infection_converge_target)
                {
                    // 策划案：感染>100 每轮 -1.5
                    next -= Config.infection_converge_down_delta;
                }

                next += GetZombieCountInChunk(v) *
                    Config.zombie_alive_infection_per_second *
                    Config.infection_update_interval;
                Chunk.infectionValue = next;
                totalInfection += Chunk.infectionValue;
            }
            if (Landscape.instance._validChunkCache.Count > 0)
            {
                levelInfo.infectionDegree = Mathf.RoundToInt(totalInfection / Landscape.instance._validChunkCache.Count);
            }
            levelInfo.playerCount = RoomInfoList.Count;
        }
        timeLock_portRefresh += Time.deltaTime;
        if (timeLock_portRefresh > Config.interactable_prop_refresh_interval)
        {
            timeLock_portRefresh = 0;
            var info=RandomInteractableProp();
            interactablePropInfo = info;
            Tool.NetworkManager.SyncTripRpc(info);
        }
        timeLock_spawnZombies += Time.deltaTime;
        if (timeLock_spawnZombies > Config.zombie_spawn_interval)
        {
            timeLock_spawnZombies = 0;
            int max = Mathf.Max(0, Landscape.instance.zombieSpawnCount);
            foreach (var v in Landscape.instance.ZombieSpawnAnchors)
            {
                if (NpcData.count >= max) break;
                var chunkIndex=ChunkSearcher<int>.GetChunkIndex(v);
                if (!Chunk.TrySet(chunkIndex)) continue;
                if (GetZombieCountInChunk(chunkIndex) >= 200) continue;
                if (Random.value < Chunk.NormalizedInfection(Chunk.infectionValue))
                {
                    var type = EntityTypeExt.RandomNpc;
                    var id = SpawnEntity(type, LevelByInfection(Chunk.infectionValue, Config.zombie_level_infection_rate), v);
                    Chunk.infectionValue += Config.zombie_spawn_infection_delta;
                    if (EntityContainer.Npcs[id] is NpcData npc)
                    {
                        npc.MoveTo(v + new Vector3(Random.Range(-10f, 10f), 0f, Random.Range(-10f, 10f)));
                    }
                }
            }
        }
        timeLock_spawnPlants += Time.deltaTime;
        if (timeLock_spawnPlants > Config.resource_spawn_check_interval)
        {
            timeLock_spawnPlants = 0;
            foreach (var v in Landscape.instance.PlantSpawnAnchors)
            {
                if (!Chunk.TrySet(v)) continue;
                if (spawnPlant[v] != null)
                {
                    if (!spawnPlant[v].Alive)
                    {
                        spawnPlantCd[v] = Time.time;
                        spawnPlant[v] = null;
                    }
                }
                else
                {
                    if (Time.time > Config.resource_respawn_cd + spawnPlantCd[v])
                    {
                        var type = ShouldSpawnInfectedResource(Chunk.infectionValue) ? EntityType.InfectedPlant : EntityTypeExt.RandomPlant;
                        var id = SpawnEntity(type, LevelByInfection(Chunk.infectionValue, Config.resource_level_infection_rate), v);
                        spawnPlant[v] = EntityContainer.OresAndPlants[id];
                    }
                }
            }
        }
        timeLock_spawnOres += Time.deltaTime;
        if (timeLock_spawnOres > Config.resource_spawn_check_interval)
        {
            timeLock_spawnOres = 0;
            foreach (var v in Landscape.instance.OreSpawnAnchors)
            {
                if (!Chunk.TrySet(v)) continue;
                if (spawnOre[v] != null)
                {
                    if (!spawnOre[v].Alive)
                    {
                        spawnOreCd[v] = Time.time;
                        spawnOre[v] = null;
                    }
                }
                else
                {
                    if (Time.time > Config.resource_respawn_cd + spawnOreCd[v])
                    {
                        var type = ShouldSpawnInfectedResource(Chunk.infectionValue) ? EntityType.InfectedOre : EntityTypeExt.RandomOre;
                        var id = SpawnEntity(type, LevelByInfection(Chunk.infectionValue, Config.ore_level_infection_rate), v);
                        spawnOre[v] = EntityContainer.OresAndPlants[id];
                    }
                }
            }
        }
        timeLock_spawnInfections += Time.deltaTime;
        if (timeLock_spawnInfections > Config.infection_spawn_check_interval)
        {
            timeLock_spawnInfections = 0;
            foreach (var v in Landscape.instance.InfectionSpawnAnchors)
            {
                if (!Chunk.TrySet(v)) continue;
                if (spawnInfection[v] != null)
                {
                    if (!spawnInfection[v].Alive)
                    {
                        spawnInfectionCd[v] = Time.time;
                        spawnInfection[v] = null;
                    }
                }
                else
                {
                    if (Time.time > Config.infection_respawn_cd + spawnInfectionCd[v] &&
                        Chunk.infectionValue >= Config.infection_entity_spawn_min_infection &&
                        Random.value < Chunk.NormalizedInfection(Chunk.infectionValue))
                    {
                        var id = SpawnEntity(EntityTypeExt.RandomInfection, LevelByInfection(Chunk.infectionValue, Config.infection_level_infection_rate), v);
                        Chunk.infectionValue += Config.infection_spawn_infection_delta;
                        spawnInfection[v] = EntityContainer.Infections[id];
                    }
                }
            }
        }
    }
    private InteractablePropInfo RandomInteractableProp()
    {
        InteractablePropInfo.CharacterUnlockProp unlock = InteractablePropInfo.CharacterUnlockProp.None;
        for(int i = 0; i < Landscape.instance.CharacterUnlockPropPos.Count; i++)
        {
            var p = Landscape.instance.CharacterUnlockPropPos[i];
            var c = ChunkSearcher<int>.GetChunkIndex(p.position);
            if (!Chunk.TrySet(c)) continue;
            if (Random.value < 0.1f + 0.4f * Chunk.NormalizedInfection(Chunk.infectionValue))
            {
                unlock |= (InteractablePropInfo.CharacterUnlockProp)(1 << i);
            }
        }
        InteractablePropInfo.ExitPort max = InteractablePropInfo.ExitPort.None;
        InteractablePropInfo.ExitPort normal = InteractablePropInfo.ExitPort.None;
        InteractablePropInfo.ExitPort min = InteractablePropInfo.ExitPort.None;
        for (int i = 0; i < Landscape.instance.ExitPortList.Count && i < 16; i++)
        {
            var p = Landscape.instance.ExitPortList[i];
            var c = ChunkSearcher<int>.GetChunkIndex(p.position);
            if (!Chunk.TrySet(c)) continue;
            var flag = (InteractablePropInfo.ExitPort)(1 << i);
            // 单一锚点列表：按所在区块感染值落入的区间，以线性插值概率决定开启哪个等级
            // 初级 [50,100] 越低越高；中级 [120,180] 越高越高；高级 [200,300] 越高越高
            float v = Chunk.infectionValue;
            if (v >= Config.exit_max_appear_infection_degree)
            {
                float prob = (v - Config.exit_max_disappear_infection_degree) /
                    (Config.exit_max_appear_infection_degree - Config.exit_max_disappear_infection_degree);
                if (Random.value < prob) max |= flag;
            }
            else if (v >= Config.exit_normal_appear_infection_degree)
            {
                float prob = (v - Config.exit_normal_disappear_infection_degree) /
                    (Config.exit_normal_appear_infection_degree - Config.exit_normal_disappear_infection_degree);
                if (Random.value < prob) normal |= flag;
            }
            else if (v >= Config.exit_min_appear_infection_degree)
            {
                float prob = (Config.exit_min_disappear_infection_degree - v) /
                    (Config.exit_min_disappear_infection_degree - Config.exit_min_appear_infection_degree);
                if (Random.value < prob) min |= flag;
            }
        }
        return new InteractablePropInfo() { characterUnlockProp = unlock, maxExitPort = max, normalExitPort = normal, minExitPort = min };
    }

    public partial LevelInfo GetLevelInfo()
    {
        return levelInfo;
    }
    public InteractablePropInfo GetInteractablePropInfo()
    {
        return interactablePropInfo;
    }
    public bool BulletHit(Bullet b,EntityData e)
    {
        var p1 = b.LastPosition;
        var p2 = b.Position;
        var q1 = e.transform.position + Vector3.up * e.colliderInfo.bottom;
        var q2 = e.transform.position + Vector3.up * e.colliderInfo.top;
        float dist = b.radius + e.colliderInfo.radius;
        float d;
        if ((p1 - p2).sqrMagnitude < 0.1f)
        {
            d = PointToSegmentDistance(p1, q1, q2);
        }
        else
        {
            d = SegmentToSegmentDistance(p1, p2, q1, q2);
        }
        return d < dist;
    }
    #region//bulletHitDetect
    private static float SegmentToSegmentDistance(Vector3 A, Vector3 B, Vector3 C, Vector3 D)
    {
        Vector3 u = B - A;
        Vector3 v = D - C;
        Vector3 w = A - C;

        float a = Vector3.Dot(u, u); // |u|²
        float b = Vector3.Dot(u, v);
        float c = Vector3.Dot(v, v); // |v|²
        float d = Vector3.Dot(u, w);
        float e = Vector3.Dot(v, w);
        float det = a * c - b * b;

        float s, t;

        // 两线段不平行
        if (det > Mathf.Epsilon)
        {
            s = (b * e - c * d) / det;
            t = (a * e - b * d) / det;

            // 限制s、t在[0,1]线段区间内
            s = Mathf.Clamp01(s);
            t = Mathf.Clamp01(t);
        }
        else
        {
            // 平行退化，固定s=0，找最优t
            s = 0f;
            t = Mathf.Clamp01(e / c);
        }

        // 第一次约束后重新校准（边界修正）
        Vector3 P1 = A + s * u;
        Vector3 P2 = C + t * v;
        float dist = Vector3.Distance(P1, P2);

        // 边界兜底：四个端点互相比对
        float dAC = Vector3.Distance(A, C);
        float dAD = Vector3.Distance(A, D);
        float dBC = Vector3.Distance(B, C);
        float dBD = Vector3.Distance(B, D);

        float minDist = Mathf.Min(dist, dAC, dAD, dBC, dBD);
        return minDist;
    }
    private static float PointToSegmentDistance(Vector3 point, Vector3 lineStart, Vector3 lineEnd)
    {
        Vector3 ab = lineEnd - lineStart;
        Vector3 ap = point - lineStart;

        // 线段长度平方，避免多次开根号
        float abSqr = ab.sqrMagnitude;
        // 线段为一个点的特殊情况
        if (abSqr < Mathf.Epsilon)
        {
            return ap.magnitude;
        }

        // 投影系数t
        float t = Vector3.Dot(ap, ab) / abSqr;
        t = Mathf.Clamp01(t);

        // 线段上最近点
        Vector3 closestPoint = lineStart + ab * t;
        return Vector3.Distance(point, closestPoint);
    }
    #endregion

    public ushort FigureDamage(Bullet b, EntityData defenser,out bool strike)
    {
        return FigureDamage(b.attribute, defenser, b.rate, out strike);
    }

    public ushort FigureDamage(EntityAttribute attacker, EntityData defenser, float rate, out bool strike)
    {
        strike= false;
        // 注意：attack 与分母都是 int，必须转 float 做除法，
        // 否则 int/int 整数除法会截断（如 200/400=0），导致 source damage 恒为 0。
        float damage = (float)attacker.attack /
            Mathf.Max(100, defenser.floatingAttribute.defense + attacker.attack) *
            attacker.attack;
        if (attacker.strikeRate - defenser.floatingAttribute.strikeRateResistance > Random.Range(0, 100))
        {
            strike = true;
            int r = attacker.strikeDamage - defenser.floatingAttribute.strikeDamageResistance;
            if (r > 100)
            {
                damage *= r * 0.01f;
            }
        }
        damage *= rate;
        if (damage > maxDamage) return maxDamage;
        else if (damage < 1) return 1;
        return (ushort)damage;
    }
}
