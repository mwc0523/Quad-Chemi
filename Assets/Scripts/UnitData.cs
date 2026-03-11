using System.Collections.Generic;
using UnityEngine;

// 스킬이 발동되는 시점을 정의합니다.
public enum SkillTrigger
{
    OnAttack,       // 기본 공격 시 확률 발동 (기존 방식)
    PassiveAura,    // 패시브 (주변 아군 버프, 적 디버프 등)
    OnKill,         // 적 처치 시 발동
    OnAttackCount,  // N회 공격 시 발동 (라그나로크 스킬3 등)
    ReplaceBasicAttack // 기본 공격 자체를 교체 (심판 스킬1 등)
}

// 스킬이 주는 효과의 종류를 정의합니다.
public enum SkillEffectType
{
    DamageArea,         // 범위 데미지 (용암, 모래 등)
    DamageProjectile,   // 발사체 데미지 (관통, 단일 등)
    ChainLightning,     // 연쇄 번개 (전기, 뇌전 등)
    Stun,               // 기절
    Slow,               // 둔화
    DOT,                // 지속 데미지 (독, 나무 등)
    BuffAlly,           // 아군 버프 (공속 증가, 스킬 확률 증가 등)
    DebuffEnemy,        // 적 디버프 (방어력 감소, 받는 피해 증가 등)
    Execution,          // 처형 (블랙홀, 심연)
    PermanentStatIncrease, // 영구 능력치 상승 (종말 스킬1)
    SpawnEntity,        // 독립적인 개체 소환 (태양, 강철벽, 해일 등)
    TsunamiLauncher     //쓰나미
}

[System.Serializable]
public struct SkillEffect
{
    public SkillEffectType effectType;
    public float value;             // 데미지 배율, 버프 수치 등 다목적
    public float duration;          // 지속 시간
    public int count;               // 연쇄 횟수, 소환 개수 등
    public GameObject effectPrefab; // 파티클이나 투사체 프리팹
}

[System.Serializable]
public struct SkillInfo
{
    public string skillName;
    public string description;
    public SkillTrigger trigger;
    [Range(0f, 1f)] public float triggerChance; // OnAttack 등에서 발동 확률
    public int triggerCount;        // OnAttackCount용 (예: 20회 공격마다)
    public float range;             // 스킬 적용 범위 (0이면 기본 사거리 사용)

    // 하나의 스킬이 여러 효과를 가질 수 있습니다 (예: 데미지 + 스턴)
    public List<SkillEffect> effects;
}

public enum UnitGrade
{
    Low,      // 하급, 흰색
    Middle,   // 중급, 연두색
    High,     // 상급, 파란색
    Epic,     // 서사, 보라색
    Legend,   // 전설, 노란색
    Myth      // 신화, 빨간색
}

[CreateAssetMenu(fileName = "NewUnitData", menuName = "ScriptableObjects/UnitData")]
public class UnitData : ScriptableObject
{
    public string unitName;    // 유닛 이름
    public UnitGrade grade;    // 등급
    public float damage;       // 공격력
    public float attackRange;  // 사거리
    public float attackSpeed;  // 공격 속도
    public Sprite unitSprite;  // 유닛 외형(이미지)
    public GameObject projectilePrefab; // 기본 공격 발사체 프리펩

    [Header("스킬 리스트")]
    public List<SkillInfo> skills;
}


//딜미터기 관련 데이터 저장소
public class UnitStatistics
{
    public float totalDamage = 0; // 누적 데미지
    public int killCount = 0;    // 처치 수
}