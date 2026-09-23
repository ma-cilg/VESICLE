//**적의 Mark 누적과 완료 상태 처리**
//책임: 피격 정보 확인 → 적 타입에 따른 Mark 규칙 적용 → Mark 상태 및 시각 변화 전달
using System;       //Action 이벤트 사용
using UnityEngine;

//적이 어떤 Mark 규칙을 사용하는지 구분
public enum EnemyMarkType
{
    Normal,         //일반몹: 일반 공격으로 바로 Mark 누적 가능
    Trigger         //특수몹: 투척검을 먼저 맞아야 Mark 누적 시작
}

public class EnemyMark : MonoBehaviour
{
    [SerializeField] private EnemyHitReceiver hitReceiver;                      //피격 정보를 전달받는 컴포넌트
    [SerializeField] private SpriteRenderer enemySprite;                        //색상을 변경할 적 SpriteRenderer

    [SerializeField] private EnemyMarkType markType = EnemyMarkType.Normal;     //현재 적의 Mark 규칙

    [SerializeField, Min(1)] private int requiredHits = 2;                      //적이 완전히 Mark되기 위해 필요한 공격수

    [SerializeField, ColorUsage(false)]
    private Color partialMarkColor = Color.white;                               //한 번 맞았지만 아직 Mark 완료 전인 색 (인스펙터에서 HEX로 관리)
    [SerializeField, ColorUsage(false)]
    private Color markedColor = Color.white;                                    //Mark가 완전히 완료됐을 때의 색 (인스펙터에서 HEX로 관리)

    private int currentHits;                                                    //누적 Hit 횟수
    public bool IsPrimed { get; private set; }                                  //트리거몹이 투척검에 맞아서 Mark가 가능해졌는지 확인
    public bool IsMarked { get; private set; }                                  //현재 완전히 Mark된 상태인지 확인
    public int CurrentHits => currentHits;                                      //외부에서 현재 누적 횟수 제공
    public int RequiredHits => requiredHits;                                    //외부에서 필요한 공격 횟수 제공
    public EnemyMarkType MarkType => markType;                                  //외부 에서 필요한 적 타입 제공

    public event Action<EnemyMark> OnPrimed;                                    //트리거몹이 투척검에 맞아서 처음 활성화 될 때 이벤트 알림
    public event Action<EnemyMark> OnMarked;                                    //Mark가 완성되면 이벤트 알림

    private void OnEnable()
    {
        hitReceiver.OnHitReceived += HandleHit;
    }

    private void OnDisable()
    {
        hitReceiver.OnHitReceived -= HandleHit;
    }

    //*공격에 맞았을 때 Mark 규칙*
    private void HandleHit(EnemyHitInfo hitInfo)
    {
        if (IsMarked) return;

        //트리거 몹인데 투척검으로 활성화 안된 경우
        if (markType == EnemyMarkType.Trigger && !IsPrimed)
        {
            //일반 공격이면 Mark에는 아무 변화 없음
            if (hitInfo.HitType != EnemyHitType.ThrownSword) return;
            
            //투척검을 처음 맞으면 트리거몹 활성화
            PrimeTriggerEnemy();
            return;
        }

        //일반몹이거나 투척검으로 활성화된 트리거몹이면 /Mark 한 단계 증가
        AddMarkHit();
    }

    //*Trigger몹에 투척검이 처음 박혔을 때*
    private void PrimeTriggerEnemy()
    {
        if (IsPrimed) return;
        IsPrimed = true;            //투척검에 맞아 Mark 가능 상태
        currentHits = 1;            //투척검 자체를 첫 번째 Mark 타격으로 계산

        //만약 RequiredHits가 1인 특수 설정이면 바로 완전 Mark 처리
        if (currentHits >= requiredHits)
        {
            CompleteMark();
            return;
        }
        ApplyPartialMarkVisual();   //아직 완전 Mark 전이므로 중간색 적용
        OnPrimed?.Invoke(this);     //외부에 Trigger 활성화 알림
    }

    //*Mark 한 단계 증가*
    private void AddMarkHit()
    {
        //필요 횟수를 넘지 않도록 한 단계 증가
        currentHits = Mathf.Min(currentHits + 1, requiredHits);

        //아직 완전 Mark가 아니면 중간색 유지
        if (currentHits < requiredHits)
        {
            ApplyPartialMarkVisual();
            return;
        }
        CompleteMark();             //필요 횟수 도달
    }

    //*부분 Mark 표시*
    private void ApplyPartialMarkVisual()
    {
        if (enemySprite == null) return;
        enemySprite.color = partialMarkColor;   //중간색 적용
    }

    //*완전 Mark 처리*
    private void CompleteMark()
    {
        if (IsMarked) return;
        IsMarked = true;                        //완전 Mark 상태 저장
        currentHits = requiredHits;             //진행도 최대값에 맞춤
        if (enemySprite != null)
        {
            enemySprite.color = markedColor;    //완전 Mark 색 적용
        }
        OnMarked?.Invoke(this);                 //외부에 Mark 완료 알림
    }
}
