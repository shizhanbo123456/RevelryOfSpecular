using System.Collections.Generic;

public class Damageable
{
    public interface IDamageable
    {
        bool CanDamage(ushort id);
        bool InDamageWindow(float time);
        IDamageable AvailableLifeTime(float left,float right);
    }
    public class onceDamageable : IDamageable
    {
        private HashSet<ushort>m_set=new();
        private float m_left;
        private float m_right;
        public onceDamageable()
        {
            m_left=0f;
            m_right = 999f;
        }
        public bool CanDamage(ushort id)
        {
            if (m_set.Contains(id)) return false;
            m_set.Add(id);
            return true;
        }
        public bool InDamageWindow(float time)
        {
            return time>m_left && time<m_right;
        }
        public IDamageable AvailableLifeTime(float left, float right)
        {
            m_left = left;
            m_right= right;
            return this;
        }
    }
    public class cdDamageable : IDamageable
    {
        private Dictionary<ushort,float>m_time=new();
        private float cd;
        private float m_left;
        private float m_right;
        public cdDamageable(float cd)
        {
            this.cd = cd;
            m_left = 0f;
            m_right = 999f;
        }
        public bool CanDamage(ushort id)
        {
            if(!m_time.ContainsKey(id))
            {
                m_time.Add(id, UnityEngine.Time.time);
                return true;
            }
            else
            {
                if (m_time[id] < UnityEngine.Time.time - cd)
                {
                    m_time[id] = UnityEngine.Time.time;
                    return true;
                }
                else
                {
                    return false;
                }
            }
        }
        public bool InDamageWindow(float time)
        {
            return time > m_left && time < m_right;
        }
        public IDamageable AvailableLifeTime(float left, float right)
        {
            m_left = left;
            m_right = right;
            return this;
        }
    }
    public static IDamageable Once()
    {
        return new onceDamageable();
    }
    public static IDamageable CD(float cd)
    {
        return new cdDamageable(cd);
    }
}