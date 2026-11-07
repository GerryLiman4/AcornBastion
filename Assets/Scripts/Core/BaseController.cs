using UnityEngine;

public class BaseController : MonoBehaviour
{
    [SerializeField] public HealthController hpController;
    [SerializeField] public FactionId factionId;
    [SerializeField] public Sprite healthySprite;
    [SerializeField] public Sprite damagedSprite;
    [SerializeField] public SpriteRenderer model;

    private void Start()
    {
        hpController.Damaged += OnDamaged;
        hpController.Died += OnDied;
        hpController.Initialize(factionId);
        model.sprite = healthySprite;
    }

    private void OnDied()
    {
        SignalManager.PlayerBaseDestroyed();
        Destroy(gameObject);
    }

    private void OnDamaged()
    {
        if(hpController.currentHp <= hpController.maxHp / 2)
        {
            model.sprite = damagedSprite;
        }
    }

    private void OnDestroy()
    {
        if (hpController == null) return;
        hpController.Damaged -= OnDamaged;
        hpController.Died -= OnDied;
    }
}
