using System;
using ApoGirl.Core.State;

namespace ApoGirl.Core.Definitions
{
    // 정의 데이터용 작은 값 묶음. [Serializable] + public 필드 → Unity Inspector와 JSON 모두에서 그대로 쓴다.

    [Serializable]
    public sealed class StatAmount
    {
        public StatType Stat;
        public int Amount;

        public StatAmount()
        {
        }

        public StatAmount(StatType stat, int amount)
        {
            Stat = stat;
            Amount = amount;
        }
    }

    [Serializable]
    public sealed class VitalAmount
    {
        public VitalType Vital;
        public int Amount;

        public VitalAmount()
        {
        }

        public VitalAmount(VitalType vital, int amount)
        {
            Vital = vital;
            Amount = amount;
        }
    }

    [Serializable]
    public sealed class ResourceAmount
    {
        public ResourceType Resource;
        public int Amount;

        public ResourceAmount()
        {
        }

        public ResourceAmount(ResourceType resource, int amount)
        {
            Resource = resource;
            Amount = amount;
        }
    }

    [Serializable]
    public sealed class HiddenAmount
    {
        public HiddenType Hidden;
        public int Amount;

        public HiddenAmount()
        {
        }

        public HiddenAmount(HiddenType hidden, int amount)
        {
            Hidden = hidden;
            Amount = amount;
        }
    }

    /// <summary>엔딩 태그 포인트.</summary>
    [Serializable]
    public sealed class TagAmount
    {
        public string Tag = "";
        public int Amount;

        public TagAmount()
        {
        }

        public TagAmount(string tag, int amount)
        {
            Tag = tag;
            Amount = amount;
        }
    }

    /// <summary>NPC 관계 변화. SetStatus가 Unknown이면 상태는 바꾸지 않는다.</summary>
    [Serializable]
    public sealed class NpcChange
    {
        public string NpcId = "";
        public int Affinity;
        public int Trust;
        public NpcStatus SetStatus;
        public string SetRole = "";
    }

    [Serializable]
    public sealed class ItemGrant
    {
        public string ItemId = "";
        public int Count = 1;

        public ItemGrant()
        {
        }

        public ItemGrant(string itemId, int count)
        {
            ItemId = itemId;
            Count = count;
        }
    }

    /// <summary>자원 획득 범위 (양 끝 포함).</summary>
    [Serializable]
    public sealed class ResourceRange
    {
        public ResourceType Resource;
        public int Min;
        public int Max;

        public ResourceRange()
        {
        }

        public ResourceRange(ResourceType resource, int min, int max)
        {
            Resource = resource;
            Min = min;
            Max = max;
        }
    }
}
