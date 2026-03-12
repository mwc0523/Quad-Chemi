using System.Collections.Generic;
using UnityEngine;

/// 어떤 종류의 스탯인지 구분
public enum StatType
{
    Attack,         // 공격력
    AttackSpeed,    // 공격 속도
    Range,          // 사거리
    CritChance,     // 치명타 확률
    CritDamage      // 치명타 배율
    // 필요해지면 여기 계속 추가 (방어력, 관통력 등)
}

/// 스탯이 어떻게 더해지는지 방식
public enum StatModifierType
{
    Flat,        // 고정 수치 +10 처럼
    PercentAdd,  // (1 + 0.10) 처럼 합산되는 % (여러 개 더해진 뒤 한 번에 적용)
    PercentMul   // (1 + 0.10) * (1 + 0.20) 처럼 곱연산 버프
}

/// 버프/업그레이드 1개를 표현
public class StatModifier
{
    public StatType statType;
    public float value;
    public StatModifierType modifierType;
    public object source; // 이 버프의 주인 (로비업글, 스킬 등 구분용)

    public StatModifier(StatType statType, float value, StatModifierType type, object source)
    {
        this.statType = statType;
        this.value = value;
        this.modifierType = type;
        this.source = source;
    }
}

/// 특정 스탯(공격력 하나 등)의 계산 로직
public class Stat
{
    public float baseValue;
    private readonly List<StatModifier> modifiers = new List<StatModifier>();

    public float FinalValue
    {
        get
        {
            float result = baseValue;
            float percentAdd = 0f;
            float percentMul = 1f;

            foreach (var m in modifiers)
            {
                switch (m.modifierType)
                {
                    case StatModifierType.Flat:
                        result += m.value;
                        break;
                    case StatModifierType.PercentAdd:
                        percentAdd += m.value;
                        break;
                    case StatModifierType.PercentMul:
                        percentMul *= (1f + m.value);
                        break;
                }
            }

            result *= (1f + percentAdd);
            result *= percentMul;
            return result;
        }
    }

    public void AddModifier(StatModifier mod)
    {
        modifiers.Add(mod);
    }
    public void RemoveModifiersFromSource(object source)
    {
        modifiers.RemoveAll(m => m.source == source);
    }
    public void RemoveModifier(StatModifier mod)
    {
    modifiers.Remove(mod);
    }
}

/// 유닛 하나가 갖는 전체 스탯 묶음
public class UnitStats
{
    private readonly Dictionary<StatType, Stat> stats = new Dictionary<StatType, Stat>();

    public float Get(StatType type)
    {
        if (!stats.ContainsKey(type))
        {
            return 0f;
        }
        return stats[type].FinalValue;
    }

    public void SetBase(StatType type, float value)
    {
        if (!stats.ContainsKey(type))
        {
            stats[type] = new Stat();
        }
        stats[type].baseValue = value;
    }

    public void AddModifier(StatModifier mod)
    {
        if (!stats.ContainsKey(mod.statType))
        {
            stats[mod.statType] = new Stat();
        }
        stats[mod.statType].AddModifier(mod);
    }

    public void RemoveModifiersFromSource(object source)
    {
        foreach (var kv in stats)
        {
            kv.Value.RemoveModifiersFromSource(source);
        }
    }
    public void RemoveModifier(StatModifier mod)
    {
        if (mod == null) return;
        if (!stats.ContainsKey(mod.statType)) return;

        stats[mod.statType].RemoveModifier(mod);
    }
}