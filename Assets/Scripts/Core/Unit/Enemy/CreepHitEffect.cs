using System;
using UnityEngine;

[RequireComponent(typeof(ParticleSystem))]
public class CreepHitEffect : MonoBehaviour
{
    [SerializeField] private ParticleSystem particles;

    private Action<CreepHitEffect> returnToPool;

    public void Initialize(Action<CreepHitEffect> onFinished)
    {
        returnToPool = onFinished;
    }

    public void Play(Vector3 position)
    {
        particles.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        transform.SetPositionAndRotation(position, Quaternion.Euler(0f, 0f, UnityEngine.Random.Range(-20f, 20f)));
        gameObject.SetActive(true);
        particles.Play(true);
    }

    private void Update()
    {
        if (!particles.IsAlive(true)) Stop();
    }

    public void Stop()
    {
        if (!gameObject.activeSelf) return;
        particles.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        gameObject.SetActive(false);
        returnToPool?.Invoke(this);
    }
}
