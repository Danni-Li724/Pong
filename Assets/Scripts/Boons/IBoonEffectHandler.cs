using UnityEngine;

public interface IBoonEffectHandler
{
   void ApplyBoonEffect(GameObject target, float duration);
   void RemoveEffect(GameObject target);
}
