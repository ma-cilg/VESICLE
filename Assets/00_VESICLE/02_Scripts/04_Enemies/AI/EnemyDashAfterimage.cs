//**적 돌진 잔상 효과**
//책임: 잔상 오브젝트 사전 생성/재사용 및 DOTween Fade
using System.Collections;
using DG.Tweening;
using UnityEngine;

public class EnemyDashAfterimage : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private SpriteRenderer sourceSprite;
    [SerializeField] private Transform poolParent;

    [Header("Afterimage")]
    [SerializeField, Range(1, 16)] private int poolSize = 8;
    [SerializeField, Min(0.01f)] private float spawnInterval = 0.04f;
    [SerializeField, Min(0.01f)] private float lifetime = 0.2f;
    [SerializeField, Range(0f, 1f)] private float alpha = 0.45f;

    private SpriteRenderer[] pool;
    private Tween[] fades;
    private int nextIndex;
    private Coroutine trailRoutine;

    private void Awake()
    {
        if (sourceSprite == null) return;
        pool = new SpriteRenderer[poolSize];
        fades = new Tween[poolSize];
        Transform parent = poolParent != null ? poolParent : transform.parent;

        for (int i = 0; i < poolSize; i++)
        {
            GameObject ghost = new GameObject("DashAfterimage_" + i);
            ghost.transform.SetParent(parent, false);
            SpriteRenderer r = ghost.AddComponent<SpriteRenderer>();
            r.sharedMaterial = sourceSprite.sharedMaterial;
            r.sortingLayerID = sourceSprite.sortingLayerID;
            r.sortingOrder = sourceSprite.sortingOrder - 1;
            ghost.SetActive(false);
            pool[i] = r;
        }
    }

    public void StartTrail()
    {
        StopTrail();
        if (pool == null || !isActiveAndEnabled) return;
        trailRoutine = StartCoroutine(TrailRoutine());
    }

    public void StopTrail()
    {
        if (trailRoutine == null) return;
        StopCoroutine(trailRoutine);
        trailRoutine = null;
    }

    private IEnumerator TrailRoutine()
    {
        WaitForSeconds wait = new WaitForSeconds(spawnInterval);
        while (true)
        {
            Spawn();
            yield return wait;
        }
    }

    private void Spawn()
    {
        if (sourceSprite == null || sourceSprite.sprite == null) return;
        int i = nextIndex;
        nextIndex = (nextIndex + 1) % pool.Length;
        fades[i]?.Kill();

        SpriteRenderer ghost = pool[i];
        ghost.sprite = sourceSprite.sprite;
        ghost.flipX = sourceSprite.flipX;
        ghost.sortingLayerID = sourceSprite.sortingLayerID;
        ghost.sortingOrder = sourceSprite.sortingOrder - 1;
        ghost.transform.SetPositionAndRotation(
            sourceSprite.transform.position, sourceSprite.transform.rotation);
        ghost.transform.localScale = sourceSprite.transform.lossyScale;

        Color color = sourceSprite.color;
        color.a *= alpha;
        ghost.color = color;
        ghost.gameObject.SetActive(true);
        fades[i] = ghost.DOFade(0f, lifetime).SetEase(Ease.OutQuad)
            .OnComplete(() =>
            {
                ghost.gameObject.SetActive(false);
                fades[i] = null;
            });
    }

    private void OnDisable()
    {
        StopTrail();
        if (pool == null) return;
        for (int i = 0; i < pool.Length; i++)
        {
            fades[i]?.Kill();
            fades[i] = null;
            if (pool[i] != null) pool[i].gameObject.SetActive(false);
        }
    }

    private void OnDestroy()
    {
        if (pool == null) return;
        foreach (SpriteRenderer r in pool)
            if (r != null) Destroy(r.gameObject);
    }
}
