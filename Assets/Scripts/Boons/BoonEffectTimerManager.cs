using Unity.Netcode;
using UnityEngine;
using System.Collections;

public class BoonEffectTimerManager : NetworkBehaviour
{
    public static BoonEffectTimerManager Instance { get; private set; }
    
    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }
    
    public void StartTimedEffect(GameObject target, BoonEffect effect)
    {
        if (IsServer)
        {
            StartCoroutine(HandleTimedEffect(target, effect));
        }
    }
    
    private IEnumerator HandleTimedEffect(GameObject target, BoonEffect effect)
    {
        IBoonEffectHandler handler = target.GetComponent<IBoonEffectHandler>();
        if (handler != null)
        {
            handler.ApplyBoonEffect(target, effect.duration);
            yield return new WaitForSeconds(effect.duration);
            handler.RemoveEffect(target);
        }
    }
}
