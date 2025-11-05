using UnityEngine;
using System.Collections;
using UnityEngine.AI;

[RequireComponent(typeof(NavMeshAgent))]
public class NPCMovement : MonoBehaviour
{
    public Transform target;
    public float updateSpeed = 0.1f;

    private NavMeshAgent myAgent;

    private void Awake()
    {
        myAgent = GetComponent<NavMeshAgent>();
    }

    private void Start()
    {
        StartCoroutine(FollowTarget());
    }

    private IEnumerator FollowTarget()
    {
        WaitForSeconds _wait = new WaitForSeconds(updateSpeed);

        while (enabled)
        {
            myAgent.SetDestination(target.transform.position);

            yield return _wait;
        }
    }
}
