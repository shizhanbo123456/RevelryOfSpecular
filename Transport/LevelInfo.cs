using System;
using System.Text;
using UnityEngine;

namespace Ros.Transport
{
    public class LevelInfo
    {
        public int difficulty=5;//0-5

        public int infectionDegree=0;//0-5
        public int playerCount=999;

        public LevelInfo() { }
        public LevelInfo(string data)
        {
            var s = data.Split('+');
            if (s.Length != 3) return;
            try
            {
                difficulty = int.Parse(s[0]);
                infectionDegree = int.Parse(s[1]);
                playerCount = int.Parse(s[2]);
            }
            catch(Exception e)
            {
                Debug.LogException(e);
            }
        }
        public override string ToString()
        {
            var sb=new StringBuilder();
            sb.Append(difficulty).Append("+");
            sb.Append(infectionDegree).Append("+");
            sb.Append(playerCount);
            return sb.ToString();
        }
    }

    public struct LevelInfoSerializer
    {
        public static bool Serialize(LevelInfo value, byte[] result, ref int indexStart)
        {
            bool hasValue = value != null;
            if (!BoolSerializer.Serialize(hasValue, result, ref indexStart)) return false;
            if (!hasValue) return true;

            return IntSerializer.Serialize(value.difficulty, result, ref indexStart) &&
                   IntSerializer.Serialize(value.infectionDegree, result, ref indexStart) &&
                   IntSerializer.Serialize(value.playerCount, result, ref indexStart);
        }

        public static LevelInfo Deserialize(byte[] data, ref int indexStart, int invalidIndex)
        {
            bool hasValue = BoolSerializer.Deserialize(data, ref indexStart, invalidIndex);
            if (!hasValue) return null;

            return new LevelInfo
            {
                difficulty = IntSerializer.Deserialize(data, ref indexStart, invalidIndex),
                infectionDegree = IntSerializer.Deserialize(data, ref indexStart, invalidIndex),
                playerCount = IntSerializer.Deserialize(data, ref indexStart, invalidIndex)
            };
        }
    }
}
