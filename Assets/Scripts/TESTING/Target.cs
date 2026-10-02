using UnityEngine;

public class Target : MonoBehaviour, ITargetable
{
    public Transform TargetTransform => this.transform;
}
