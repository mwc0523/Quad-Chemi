using System;
using System.Collections.Generic;

// 1. 재화 종류를 Enum으로 관리 (추가될 때마다 여기에 이름만 넣으면 됩니다)
public enum CurrencyType
{
    Ticket,     // 입장 티켓
    Essence,    // 정수 (기본 재화)
    Aether      // 에테르 (유료 재화)
} 

// 2. 유닛 개별 정보 (확장성을 고려한 구조)
[Serializable]
public class UnitSaveData
{
    public string unitID;     // 유닛 고유 ID (예: "FireNemo", "WaterNemo")
    public int level;         // 유닛 레벨
    public int count;         // 보유 개수 (합성/진화 등에 사용)
    public string weaponID;   // 나중에 전용 무기가 생길 경우를 대비한 확장 변수

    public UnitSaveData(string id)
    {
        unitID = id;
        level = 1;
        count = 0;
        weaponID = "";
    }
}

// 3. 설정 정보
[Serializable]
public class SettingsData
{
    public float masterVolume = 1f;
    public bool isPushNotificationOn = true;
}

// 4. 최상위 유저 데이터 (서버나 파일로 저장될 '본체')
[Serializable]
public class UserProfile
{
    public string nickname = "Player";
    public int playerLevel = 1;
    public int currentExp = 0;
    public int totalExp = 0;

    // 재화 관리는 Dictionary가 편하지만, Unity 기본 JsonUtility는 Dictionary를 
    // 기본 지원하지 않으므로 List나 별도의 직렬화 패키지(Newtonsoft.Json)를 권장합니다.
    // 여기서는 확장성을 위해 List 형태의 보관함을 사용하거나 직접 명시합니다.
    public int ticket = 10;
    public long essence = 0; // 골드류는 나중에 수치가 커질 수 있어 long 권장
    public int aether = 0;

    [NonSerialized] // 이 변수는 JsonUtility로 저장하지 않습니다.
    public int currentRunReachedWave;
    [NonSerialized]
    public bool isCurrentRunClear;

    // 티켓 회복을 위한 마지막 접속 시간 기록
    public string lastTicketChargeTime;

    // 보유 유닛 목록
    public List<UnitSaveData> unitList = new List<UnitSaveData>();

    // 설정 데이터
    public SettingsData settings = new SettingsData();
}