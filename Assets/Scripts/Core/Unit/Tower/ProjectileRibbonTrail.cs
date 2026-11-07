using UnityEngine;
using UnityEngine.SceneManagement;

public class ProjectileRibbonTrail : MonoBehaviour
{
    [SerializeField] private TrailRenderer trailPrefab;
    [SerializeField] private Transform emissionPoint;
    [SerializeField] private SpriteRenderer model;

    private TrailRenderer activeTrail;

    private void OnEnable()
    {
        if (trailPrefab == null || emissionPoint == null) return;

        // Keep the ribbon independent of the projectile and its target so it can finish fading.
        activeTrail = Instantiate(trailPrefab, emissionPoint.position, Quaternion.identity);
        SceneManager.MoveGameObjectToScene(activeTrail.gameObject, gameObject.scene);
        if (model != null)
        {
            activeTrail.sortingLayerID = model.sortingLayerID;
            activeTrail.sortingOrder = model.sortingOrder - 1;
        }
        activeTrail.Clear();
        activeTrail.AddPosition(emissionPoint.position);
        activeTrail.emitting = true;
    }

    private void LateUpdate()
    {
        if (activeTrail != null && emissionPoint != null)
            activeTrail.transform.position = emissionPoint.position;
    }

    private void OnDisable()
    {
        if (activeTrail == null) return;
        if (emissionPoint != null) activeTrail.AddPosition(emissionPoint.position);
        activeTrail.emitting = false;
        Destroy(activeTrail.gameObject, activeTrail.time + .05f);
        activeTrail = null;
    }
}
