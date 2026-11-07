using System;
using DG.Tweening;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class CardButtonUI : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    [SerializeField] public Button button;
    [SerializeField] public int id;
    [SerializeField] public int price = 100;
    [SerializeField] private TMPro.TMP_Text towerNameLabel;
    [SerializeField] private TMPro.TMP_Text priceLabel;

    [Header("Animation")]
    [SerializeField, Min(1f)] private float hoverScale = 1.08f;
    [SerializeField, Range(.5f, 1f)] private float clickScale = .94f;
    [SerializeField, Min(.01f)] private float animationDuration = .15f;

    private Vector3 normalScale;
    private Tween scaleTween;
    private bool isHovered;

    public event Action<int> ButtonPressed;

    public void Configure(TowerDefinition definition)
    {
        if (definition == null) return;
        price = definition.cost;
        priceLabel.text = price.ToString();
        towerNameLabel.text = definition.name;
    }

    private void OnEnable()
    {
        normalScale = transform.localScale;
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (!CanInteract()) return;
        isHovered = true;
        StopAnimation();
        scaleTween = transform.DOScale(normalScale * hoverScale, animationDuration)
            .SetEase(Ease.OutBack).SetUpdate(true).OnKill(() => scaleTween = null);
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        isHovered = false;
        StopAnimation();
        if (!isActiveAndEnabled) return;
        scaleTween = transform.DOScale(normalScale, animationDuration)
            .SetEase(Ease.OutQuad).SetUpdate(true).OnKill(() => scaleTween = null);
    }

    private bool CanInteract()
    {
        return isActiveAndEnabled && (button == null || (button.IsActive() && button.IsInteractable()));
    }

    private void StopAnimation()
    {
        scaleTween?.Kill();
        scaleTween = null;
    }

    private void OnDisable()
    {
        StopAnimation();
        isHovered = false;
        transform.localScale = normalScale;
    }

    public void Initialize(int designatedId)
    {
        id = designatedId;
    }
    public void OnButtonPressed()
    {
        if (!CanInteract()) return;

        if (AudioManager.Instance != null) AudioManager.Instance.PlaySelectCard();
        StopAnimation();
        Vector3 restingScale = normalScale * (isHovered ? hoverScale : 1f);
        scaleTween = DOTween.Sequence()
            .Append(transform.DOScale(normalScale * clickScale, animationDuration * .5f).SetEase(Ease.OutQuad))
            .Append(transform.DOScale(restingScale, animationDuration).SetEase(Ease.OutBack))
            .SetTarget(transform).SetUpdate(true).OnKill(() => scaleTween = null);
        ButtonPressed?.Invoke(id);
    }
}
