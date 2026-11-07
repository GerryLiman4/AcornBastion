using UnityEngine;
using UnityEngine.EventSystems;

public class BaseButtonUI : MonoBehaviour, IPointerEnterHandler
{
    [SerializeField] public AudioClip hoverClip;
    [SerializeField] public AudioClip clickClip;

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (AudioManager.Instance != null) AudioManager.Instance.PlayButtonSound(hoverClip);
    }

    public void PlayAudioOnPressed()
    {
        if (AudioManager.Instance != null) AudioManager.Instance.PlayButtonSound(clickClip);
    }
}
