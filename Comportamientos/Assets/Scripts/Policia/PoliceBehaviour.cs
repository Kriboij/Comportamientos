using BehaviourAPI.UnityToolkit;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;
using DG.Tweening;
using BehaviourAPI.Core;

public class PoliceBehaviour : MonoBehaviour
{
    [Header("Health")]
    [SerializeField]
    private int health = 100;

    [Header("Patrol")]
    public List<Transform> patrolPositions;
    private int currentPatrolIndex = 0;
    private Coroutine patrolCorutine = null;


    [Header("Investigate")]
    public InvestigableObject InvestigableObject = null;

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



    public void Investigate() 
    {
        Vector3 investigatePostion = InvestigableObject.transform.position;

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
            DOVirtual.DelayedCall(2f, () => { InvestigableObject = null; Debug.Log("Ended investigating"); });
        }

    }


    public bool CheckInvestigate() 
    {
        if (InvestigableObject != null) 
        {
            //Return success and in SFM launch investigate
            return true;
        }
        return false;
    }

    private void OnTriggerEnter(Collider other)
    {
        Debug.Log("Trigger");
        if (other.TryGetComponent(out InvestigableObject)) 
        {
            Debug.Log("investigate");
        }
    }

}
