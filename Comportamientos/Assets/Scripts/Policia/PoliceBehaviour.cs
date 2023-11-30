using BehaviourAPI.UnityToolkit;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

public class PoliceBehaviour : MonoBehaviour
{
    [Header("Health")]
    [SerializeField]
    private int health = 100;

    [Header("Patrol")]
    public List<Transform> patrolPositions;
    private int currentPatrolIndex = 0;
    private Coroutine patrolCorutine = null;


    public NavMeshAgent agent;

    // Start is called before the first frame update
    private void Awake()
    {
        agent = GetComponent<NavMeshAgent>();
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public void Patrol() 
    {
        if (patrolCorutine != null) 
        {
            StopCoroutine(patrolCorutine);
            patrolCorutine = null;
        }
        patrolCorutine = StartCoroutine(PatrolCorutine());

        IEnumerator PatrolCorutine() 
        {
            while (true)
            {
                agent.SetDestination(patrolPositions[currentPatrolIndex].position);
                yield return new WaitUntil(() => { return isPathComplete(); });
                currentPatrolIndex++;
                currentPatrolIndex %= patrolPositions.Count;
            }
        }
    }

    bool isPathComplete()
    {
        return (!agent.pathPending &&
            agent.remainingDistance <= agent.stoppingDistance &&
            (!agent.hasPath || agent.velocity.sqrMagnitude == 0f));
    }
}
