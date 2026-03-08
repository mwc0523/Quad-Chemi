using UnityEngine;


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

    [Header("스킬 설정")]
    public GameObject skillProjectilePrefab; // 스킬용 이미지 (다른 프리팹!)
    public float skillChance = 0.12f;        // 12% 확률
    public float skillDamageMultiplier = 5f; // 5배 데미지
}

