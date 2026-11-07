using DG.Tweening;
using UnityEngine;
using UnityEngine.SceneManagement;

public class CreepDeathFlight : MonoBehaviour
{
    private Sequence flightTween;

    public static void Play(Animator model, SpriteRenderer shadow, float duration, float spins)
    {
        var sprite = model.GetComponent<SpriteRenderer>();
        Vector3 center = sprite != null ? sprite.bounds.center : model.transform.position;
        float margin = sprite != null ? sprite.bounds.extents.magnitude + 1f : 2f;
        var body = new GameObject("Creep Death Flight");
        SceneManager.MoveGameObjectToScene(body, model.gameObject.scene);
        body.transform.position = center;
        model.enabled = false;
        if (shadow != null) shadow.enabled = false;
        model.transform.SetParent(body.transform, true);
        if (sprite != null) sprite.sortingOrder = Mathf.Max(sprite.sortingOrder, 50);

        float side = Random.value < 0.5f ? -1f : 1f;
        Camera camera = Camera.main;
        Vector3 right = camera != null ? camera.transform.right : Vector3.right;
        Vector3 up = camera != null ? camera.transform.up : Vector3.up;
        float angle = Random.Range(0f, Mathf.PI * 2f);
        Vector3 direction = right * Mathf.Cos(angle) + up * Mathf.Sin(angle);
        Vector3 perpendicular = -right * Mathf.Sin(angle) + up * Mathf.Cos(angle);
        Vector3 destination = camera != null
            ? GetOffscreenDestination(camera, center, direction, margin)
            : center + direction * 12f;
        float curve = Random.Range(-0.8f, 0.8f);

        duration = Mathf.Max(0.1f, duration);
        var flight = body.AddComponent<CreepDeathFlight>();
        flight.flightTween = DOTween.Sequence()
            .Append(DOVirtual.Float(0f, 1f, duration, progress =>
                body.transform.position = Vector3.Lerp(center, destination, progress)
                    + perpendicular * (Mathf.Sin(progress * Mathf.PI) * curve))
                .SetEase(Ease.OutQuad))
            .Join(body.transform.DORotate(new Vector3(0f, 0f, -side * 360f * spins), duration, RotateMode.LocalAxisAdd)
                .SetEase(Ease.Linear))
            .SetTarget(body.transform)
            .OnComplete(() => Destroy(body))
            .OnKill(() => flight.flightTween = null);
    }

    private static Vector3 GetOffscreenDestination(Camera camera, Vector3 center, Vector3 direction, float margin)
    {
        Vector3 viewport = camera.WorldToViewportPoint(center);
        Vector3 step = camera.WorldToViewportPoint(center + direction) - viewport;
        float distanceX = Mathf.Abs(step.x) < 0.000001f ? float.PositiveInfinity
            : ((step.x > 0f ? 1f : 0f) - viewport.x) / step.x;
        float distanceY = Mathf.Abs(step.y) < 0.000001f ? float.PositiveInfinity
            : ((step.y > 0f ? 1f : 0f) - viewport.y) / step.y;
        float distance = Mathf.Max(0f, Mathf.Min(distanceX, distanceY));
        // Add enough room beyond the chosen edge for the entire spinning sprite.
        Vector3 edgeNormal = distanceX < distanceY ? camera.transform.right : camera.transform.up;
        float alignment = Mathf.Max(0.001f, Mathf.Abs(Vector3.Dot(direction, edgeNormal)));
        return center + direction * (distance + margin / alignment);
    }

    private void OnDisable()
    {
        flightTween?.Kill();
        flightTween = null;
    }
}
