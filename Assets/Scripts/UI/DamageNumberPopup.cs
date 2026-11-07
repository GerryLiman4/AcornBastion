using System;
using TMPro;
using UnityEngine;

[RequireComponent(typeof(TextMeshPro))]
public class DamageNumberPopup : MonoBehaviour
{
    [SerializeField] private TextMeshPro label;
    [SerializeField, Min(0.01f)] private float duration = 0.75f;
    [SerializeField, Min(0f)] private float riseDistance = 0.8f;
    [SerializeField, Min(0f)] private float horizontalDrift = 0.2f;

    private Action<DamageNumberPopup> returnToPool;
    private Vector3 startPosition;
    private float drift;
    private float elapsed;

    public void Initialize(Action<DamageNumberPopup> onFinished)
    {
        returnToPool = onFinished;
    }

    public void Show(Vector3 position, int damage, Color color)
    {
        Show(position, damage.ToString(), color);
    }

    public void Show(Vector3 position, string text, Color color)
    {
        startPosition = position;
        drift = UnityEngine.Random.Range(-horizontalDrift, horizontalDrift);
        elapsed = 0f;
        transform.position = position;
        transform.localScale = Vector3.one * 0.65f;
        label.text = text;
        label.color = color;
        label.alpha = 1f;
        gameObject.SetActive(true);
    }

    private void Update()
    {
        elapsed += Time.deltaTime;
        float progress = Mathf.Clamp01(elapsed / duration);
        float rise = 1f - (1f - progress) * (1f - progress);
        transform.position = startPosition + new Vector3(drift * progress, riseDistance * rise, 0f);

        float scale = progress < 0.18f
            ? Mathf.Lerp(0.65f, 1.15f, progress / 0.18f)
            : Mathf.Lerp(1.15f, 1f, (progress - 0.18f) / 0.82f);
        transform.localScale = Vector3.one * scale;
        label.alpha = 1f - Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(0.55f, 1f, progress));

        if (progress >= 1f) Stop();
    }

    public void Stop()
    {
        if (!gameObject.activeSelf) return;
        gameObject.SetActive(false);
        returnToPool?.Invoke(this);
    }
}
