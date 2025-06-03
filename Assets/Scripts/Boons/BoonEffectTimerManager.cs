using Unity.Netcode;
using UnityEngine;
using System.Collections;

public class BoonEffectTimerManager : NetworkBehaviour
{
    public void StartTimedEffect(GameObject target, BoonEffect effect)
    {
        StartCoroutine(HandlesTimedEffect(target, effect));
    }

    private IEnumerator HandlesTimedEffect(GameObject target, BoonEffect effect)
    {
        IBoonEffectHandler handler = target.GetComponent<IBoonEffectHandler>();
        handler.ApplyBoonEffect(target, effect.duration);
        yield return new WaitForSeconds(effect.duration);
        handler.RemoveEffect(target);
    }
}
