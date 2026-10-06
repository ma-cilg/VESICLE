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
    [SerializeField] private SpriteRenderer hitSprite;                          //피격 애니메이션용 SpriteRenderer
    [SerializeField] private Transform deathVisualRoot;                         //사망 시 중앙 수축 기준

    [SerializeField] private EnemyMarkType markType = EnemyMarkType.Normal;     //현재 적의 Mark 규칙

    [SerializeField, Min(1)] private int requiredHits = 2;                      //적이 완전히 Mark되기 위해 필요한 공격수

    [SerializeField, ColorUsage(false)]
    private Color partialMarkColor = Color.white;                               //한 번 맞았지만 아직 Mark 완료 전인 색 (인스펙터에서 HEX로 관리)
    [SerializeField, ColorUsage(false)]
    private Color markedColor = Color.white;                                    //Mark가 완전히 완료됐을 때의 색 (인스펙터에서 HEX로 관리)

    private int currentHits;                                                    //누적 Hit 횟수
    private Color initialEnemyColor;                                            //적 본체의 처음 색상
    private Color initialHitColor;                                              //피격 Sprite의 처음 색상

    public bool IsPrimed { get; private set; }                                  //트리거몹이 투척검에 맞아서 Mark가 가능해졌는지 확인
    public bool IsMarked { get; private set; }                                  //현재 완전히 Mark된 상태인지 확인
    public int CurrentHits => currentHits;
    public int RequiredHits => requiredHits;
    public EnemyMarkType MarkType => markType;
    public Transform DeathVisualRoot => deathVisualRoot;

    public event Action<EnemyMark> OnPrimed;                                    //트리거몹이 투척검에 맞아서 처음 활성화 될 때 이벤트 알림
    public event Action<EnemyMark> OnMarked;                                    //Mark가 완성되면 이벤트 알림
    public event Action OnMeleeBlocked;                                         //Trigger가 이동 공격을 막았을 때 알림

    private void Awake()
    {
        //Mark되기 전 원래 Sprite 색상을 저장
        if (enemySprite != null)
        {
            initialEnemyColor = enemySprite.color;
        }

        if (hitSprite != null)
        {
            initialHitColor = hitSprite.color;
        }
    }

    private void OnEnable()
    {
        ResetMarkState();

        hitReceiver.OnHitReceived += HandleHit;
    }

    private void OnDisable()
    {
        hitReceiver.OnHitReceived -= HandleHit;
    }

    //*재활성화 시 Mark 상태 초기화*
    private void ResetMarkState()
    {
        currentHits = 0;
        IsPrimed = false;
        IsMarked = false;

        //Mark 색상이 남지 않도록 처음 색상으로 복구
        if (enemySprite != null)
        {
            enemySprite.color = initialEnemyColor;
        }

        if (hitSprite != null)
        {
            hitSprite.color = initialHitColor;
        }
    }

    //*공격에 맞았을 때 Mark 규칙*
    private void HandleHit(EnemyHitInfo hitInfo)
    {
        if (IsMarked) return;

        //Trigger 적은 일반 적과 다른 규칙 사용
        if (markType == EnemyMarkType.Trigger)
        {
            HandleTriggerHit(hitInfo);
            return;
        }

        //Normal 적은 공격 횟수만큼 Mark 누적
        AddMarkHit();
    }

    //*Trigger 적 피격 규칙*
    private void HandleTriggerHit(EnemyHitInfo hitInfo)
    {
        //이미 검이 박힌 상태라면 추가 공격으로 상태가 변하지 않음
        if (IsPrimed) return;

        //투척검 이외의 공격은 Trigger 상태에 영향 없음
        if (hitInfo.HitType != EnemyHitType.ThrownSword) return;

        PrimeTriggerEnemy();
    }

    //*Trigger 적에 투척검이 처음 박혔을 때*
    private void PrimeTriggerEnemy()
    {
        if (IsPrimed) return;

        IsPrimed = true;                        //검이 박혀 폭발 가능한 상태
        currentHits = 0;                        //Trigger는 일반 타격 횟수를 사용하지 않음

        ApplyMarkColor(markedColor);            //검이 박히면 초록색으로 변경

        OnPrimed?.Invoke(this);                 //검 박힘 Visual 등에 상태 전달
    }

    //*Trigger 적이 이동 공격을 막았을 때*
    public void NotifyMeleeBlocked()
    {
        if (markType != EnemyMarkType.Trigger) return;
        if (IsMarked) return;

        OnMeleeBlocked?.Invoke();
    }

    //*Trigger 적 폭발 시도*
    public bool TryDetonate()
    {
        if (markType != EnemyMarkType.Trigger) return false;    //Trigger 적이 아니면 폭발 불가
        if (!IsPrimed) return false;                            //아직 검이 박히지 않았다면 폭발 불가
        if (IsMarked) return false;                             //이미 사망 처리된 상태면 중복 폭발 방지

        CompleteMark();                         //기존 OnMarked → EnemyDeath 흐름 사용

        return true;
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
        ApplyMarkColor(partialMarkColor);
    }

    //*현재 Mark 색상을 모든 적 Visual에 동일하게 적용*
    private void ApplyMarkColor(Color color)
    {
        if (enemySprite != null)
        {
            enemySprite.color = color;
        }

        if (hitSprite != null)
        {
            hitSprite.color = color;
        }
    }

    //*완전 Mark 처리*
    private void CompleteMark()
    {
        if (IsMarked) return;
        IsMarked = true;                        //완전 Mark 상태 저장
        currentHits = requiredHits;             //진행도 최대값에 맞춤

        ApplyMarkColor(markedColor);

        OnMarked?.Invoke(this);                 //외부에 Mark 완료 알림
    }
}
