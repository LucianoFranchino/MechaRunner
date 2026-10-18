using UnityEngine;

/// <summary>
/// Efecto de partículas pooleable: se reproduce en cada spawn y vuelve al pool
/// cuando termina. Fuerza Stop Action = Callback porque "Destroy" sacaría el
/// objeto del pool y dejaría una referencia destruida adentro.
/// </summary>
[RequireComponent(typeof(ParticleSystem), typeof(PooledObject))]
public class PooledParticle : MonoBehaviour, IPoolable
{
    private ParticleSystem particles;
    private PooledObject pooledObject;

    private void Awake()
    {
        particles = GetComponent<ParticleSystem>();
        pooledObject = GetComponent<PooledObject>();

        var main = particles.main;
        main.stopAction = ParticleSystemStopAction.Callback;
    }

    public void OnSpawn()
    {
        particles.Clear(true);
        particles.Play(true);
    }

    public void OnDespawn()
    {
        particles.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
    }

    private void OnParticleSystemStopped()
    {
        if (gameObject.activeInHierarchy)
            pooledObject.Despawn();
    }
}
