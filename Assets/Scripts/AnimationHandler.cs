using UnityEngine;
using UnityEngine.Events;

public class AnimationHandler : MonoBehaviour
{
    public UnityEvent<string> AnimationEvent;

    public void OnAnimationEvent(string eventName)
    {
        AnimationEvent?.Invoke(eventName);
    }

    private void OnDestroy()
    {
        AnimationEvent.RemoveAllListeners();
    }
}
