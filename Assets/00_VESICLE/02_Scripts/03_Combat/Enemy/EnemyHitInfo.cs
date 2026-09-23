//**적이 받은 공격 정보**
//책임: 공격 종류와 피격 방향을 하나의 데이터로 묶어서 전달

using UnityEngine;

//적이 어떤 종류의 공격에 맞았는지 구분
public enum EnemyHitType
{
    Melee,          //X 기본 검 공격
    ThrownSword     //C 투척 검 공격
}

//적에게 전달할 피격 정보 (구조체로 묶어서 사용)
public readonly struct EnemyHitInfo
{
    public EnemyHitType HitType { get; }    //어떤 종류의 공격인지
    public Vector2 HitDirection { get; }    //어느 방향으로 공격이 들어왔는지

    //EnemyHitInfo를 만들 때 공격 종류와 방향을 반드시 같이 넣도록 생성자 사용
    public EnemyHitInfo(EnemyHitType hitType, Vector2 hitDirection)
    {
        HitType = hitType;
        HitDirection = hitDirection.normalized;     //방향만 사용하도록 정규화 (길이 X)
    }
}
