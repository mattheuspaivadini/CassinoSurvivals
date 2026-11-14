using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Pool;

/// <summary>Wrapper fino sobre UnityEngine.Pool.ObjectPool&lt;T&gt; com pré-aquecimento (prewarm) para evitar Instantiate em gameplay.</summary>
public sealed class PrefabPool<T> where T : Component
{
    readonly ObjectPool<T> pool;

    public int CountActive => pool.CountActive;

    public PrefabPool(T prefab, Transform parent, int prewarm, int maxSize)
    {
        pool = new ObjectPool<T>(
            () => Object.Instantiate(prefab, parent),
            x => x.gameObject.SetActive(true),
            x => x.gameObject.SetActive(false),
            x => Object.Destroy(x.gameObject),
            false, prewarm, maxSize);

        // Cria tudo no carregamento da cena, não durante o combate.
        var tmp = new List<T>(prewarm);
        for (int i = 0; i < prewarm; i++) tmp.Add(pool.Get());
        for (int i = 0; i < tmp.Count; i++) pool.Release(tmp[i]);
    }

    public T Get() => pool.Get();
    public void Release(T item) => pool.Release(item);
}
