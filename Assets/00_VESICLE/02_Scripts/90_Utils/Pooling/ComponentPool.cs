//**공용 제네릭 풀**
//책임: Component 생성 → 보관 → Get → Return
using System.Collections.Generic;   //Queue<T> 컬렉션 사용
using UnityEngine;

public class ComponentPool<T> where T : Component
{
    private readonly T prefab;                      //생성할 원본 Prefab
    private readonly Transform poolRoot;            //사용하지 않는 오브젝트들을 정리해서 둘 부모 Transform
    private readonly Queue<T> available = new();    //현재 사용하지 않고 대기 중인 Component 보관

    //*생성자*
    public ComponentPool(T prefab, Transform poolRoot, int initialSize)
    {
        this.prefab = prefab;                       //전달받은 Prefab 저장
        this.poolRoot = poolRoot;                   //전달받은 Pool 부모 저장
        for (int i = 0; i < initialSize; i++)       //처음 사용할 개수만큼 미리 생성
        {
            T instance = CreateInstance();          //새 Component 생성
            available.Enqueue(instance);            //사용하지 않는 상태로 Queue에 넣어둠
        }
    }

    //*Pool에서 사용할 Component 가져오기*
    public T Get()
    {
        if (available.Count > 0)                    //대기 중인 객체가 있으면 기존 객체 사용
        {
            T instance = available.Dequeue();
            instance.gameObject.SetActive(true);
            return instance;
        }
        T newInstance = CreateInstance();           //대기 중인 객체 없으면 추가
        newInstance.gameObject.SetActive(true);     //활성화
        return newInstance;
    }

    //*사용 끝난 Component를 Pool에 반환*
    public void Return(T instance)
    {
        if (instance == null) return;
        instance.transform.SetParent(poolRoot);     //Pool 전용 부모 아래로 다시 이동
        instance.gameObject.SetActive(false);       //비활성화
        available.Enqueue(instance);                //Queue에 보관
    }

    //*새로운 Component 생성*
    private T CreateInstance()
    {
        T instance = Object.Instantiate(prefab, poolRoot);  //Prefab 원본으로부터 새 인스턴스 생성
        instance.gameObject.SetActive(false);               //처음에는 안 사용하니까 비활성화
        return instance;
    }

}
