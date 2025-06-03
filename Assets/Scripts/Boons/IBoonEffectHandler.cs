using UnityEngine;

public interface IBoonEffectHandler
{
   void ApplyBoonEffect(GameObject boon, float duration);
   void RemoveEffect(GameObject target);
}
