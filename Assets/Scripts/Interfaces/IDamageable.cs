using Unity.VisualScripting.Antlr3.Runtime.Misc;
using UnityEngine;

public interface IDamageable
{
    void ReceiveDamage(DamageInfo damageInfo);
}
