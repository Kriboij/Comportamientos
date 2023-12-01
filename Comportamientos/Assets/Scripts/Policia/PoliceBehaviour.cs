using BehaviourAPI.UnityToolkit;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;
using DG.Tweening;
using BehaviourAPI.Core;

enum PoliceStates 
{
        Patrol,
        Investigate
}

public class PoliceBehaviour : MonoBehaviour
{
    [Header("Health")]
    [SerializeField]
    private int health = 100;

    [Header("Patrol")]
    [SerializeField]
    private List<Transform> patrolPositions;
    private int currentPatrolIndex = 0;
    private Coroutine patrolCorutine = null;

    [Header("Paranoia/Thread")]
    [SerializeField]
    private int paranoia = 0;

    [Header("Investigate")]
    public InvestigableObject investigableObject = null;

    public NavMeshAgent agent;

    [SerializeField]
    private PoliceStates state = PoliceStates.Patrol;

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
        state = PoliceStates.Patrol;
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



    public void Investigate()
    {
        state = PoliceStates.Investigate;
        Vector3 investigatePostion = investigableObject.investigatePosition.position;

        //Stop patrolling
        if (patrolCorutine != null)
        {
            StopCoroutine(patrolCorutine);
            patrolCorutine = null;
            agent.SetDestination(transform.position);
        }

        //Start investigating
        StartCoroutine(InvestigateCorutine(investigatePostion));

        IEnumerator InvestigateCorutine(Vector3 investigatePostion)
        {
            agent.SetDestination(investigatePostion); //Go to investigate position
            yield return new WaitUntil(() => { return isPathComplete(); }); //Wait for arrival at pos
            //Launch animation or sth and later return to patrol?
            DOVirtual.DelayedCall(investigableObject.investigateTime, () => {
                Debug.Log("Finished investigating");
                investigableObject?.HasBeenInvestigated();
                investigableObject = null;
            });
        }
    }


    public bool CheckInvestigate()
    {

        if (investigableObject != null)
        {
            //Return success and in SFM launch investigate
            return investigableObject.ShouldInvestigate();
        }
        return false;
    }

    public bool CheckEndedInvestigate()
    {

        if (investigableObject == null)
        {
            return true;
        }
        return false;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.TryGetComponent(out investigableObject))
        {
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.TryGetComponent(out investigableObject))
        {
            investigableObject = null;
        }
    }



    private void OnDrawGizmos()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireCube(transform.position + new Vector3(0,1,0), new Vector3(15,1,15));
    }
}
