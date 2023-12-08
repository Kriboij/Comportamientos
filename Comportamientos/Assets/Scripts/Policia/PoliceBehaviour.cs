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

    [Header("Thinking bubble")]
    [SerializeField]
    ThinkingCloudBehaviour thinkingCloudBehaviour;

    private NavMeshAgent agent;
    private Animator animator;

    [SerializeField]
    private PoliceStates state = PoliceStates.Patrol;

    // Start is called before the first frame update
    private void Awake()
    {
        agent = GetComponent<NavMeshAgent>();
        animator = GetComponent<Animator>();
    }


    public void Patrol()
    {
        state = PoliceStates.Patrol;
        thinkingCloudBehaviour.UpdateCloud(0);
        animator.SetBool("Investigate", false);

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
        thinkingCloudBehaviour.UpdateCloud(1);

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
            transform.DOLookAt(investigableObject.transform.position, 0.5f, AxisConstraint.Y).OnComplete(() => { animator.SetBool("Investigate",true);});
            
            DOVirtual.DelayedCall(investigableObject.investigateTime, () => {
                Debug.Log("Finished investigating: " + investigableObject);
                investigableObject?.HasBeenInvestigated();
                investigableObject = null;
                animator.SetBool("Investigate", false);
            });
        }
    }


    public bool CheckInvestigate()
    {

        if (investigableObject != null)
        {
            if (investigableObject.ShouldInvestigate(paranoia)) 
            {
                //If should be investigated activate transition
                return true;
            }
            //If not delete reference
            investigableObject = null;
        }
        return false;
    }

    public bool CheckEndedInvestigate()
    {

        if (investigableObject == null)
        {
            //If object reference has been deleted transition to patrol
            return true;
        }
        return false;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.TryGetComponent(out investigableObject))
        {
            //TODO Maybe change collider to sphere and add a raycast check
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
