using BehaviourAPI.UnityToolkit;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;
using DG.Tweening;
using BehaviourAPI.Core;
using System.Reflection;

enum PoliceStates 
{
        Patrol,
        Investigate
}

//TODO adjust agent speed when walking or running
//TODO make all animations
//TODO make combat system
//TODO change police vision
//TODO Finish BT 

public class PoliceBehaviour : MonoBehaviour
{
    [Header("Health")]
    [SerializeField]
    private int maxHealth = 100;
    public int currentHealth = 100;
    [SerializeField]
    private int timeToHealSeconds = 10;


    [Header("Patrol")]
    [SerializeField]
    private List<Transform> patrolPositions;
    private int currentPatrolIndex = 0;
    private Coroutine currentCorutine = null;

    [Header("Paranoia/Thread")]
    [SerializeField]
    private int paranoia = 0;

    [Header("Investigate")]
    public InvestigableObject investigableObject = null;

    [Header("Reinforcements")]
    [SerializeField]
    private int numbeOfReinforcements;
    [SerializeField]
    private int timeToCall;
    [SerializeField]
    private int timeToSpawn;
    [SerializeField]
    private Transform reinforcementsSpawnPos;

    [Header("Flee")]
    [SerializeField]
    private Transform FleePos;

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

    #region Patrol
    public void Patrol()
    {
        state = PoliceStates.Patrol;
        thinkingCloudBehaviour.UpdateCloud(0);
        animator.SetBool("Investigate", false);

        if (currentCorutine != null)
        {
            StopCoroutine(currentCorutine);
            currentCorutine = null;
        }
        currentCorutine = StartCoroutine(PatrolCorutine());

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

    #endregion


    #region Investigate
    public void Investigate()
    {
        state = PoliceStates.Investigate;
        thinkingCloudBehaviour.UpdateCloud(1);

        Vector3 investigatePostion = investigableObject.investigatePosition.position;

        //Stop patrolling
        if (currentCorutine != null)
        {
            StopCoroutine(currentCorutine);
            currentCorutine = null;
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
    #endregion

    #region Heal and Reinforcements

    public void Heal() 
    {
        if (currentCorutine != null)
        {
            StopCoroutine(currentCorutine);
            currentCorutine = null;
            agent.SetDestination(transform.position);
        }

        currentCorutine = StartCoroutine(HealCorutine());

        IEnumerator HealCorutine() 
        {
            yield return new WaitForSeconds(timeToHealSeconds);
            currentHealth = maxHealth;
        }
    }

    public void Reinforcements() 
    {
        if (currentCorutine != null)
        {
            StopCoroutine(currentCorutine);
            currentCorutine = null;
            agent.SetDestination(transform.position);
        }

        currentCorutine = StartCoroutine(ReinforcementsCorutine());

        IEnumerator ReinforcementsCorutine()
        {

            yield return new WaitForSeconds(timeToCall);
            yield return new WaitForSeconds(timeToSpawn);
            for (int i = 0; i < numbeOfReinforcements; i++) 
            {
                PoliceBehaviour instance = Instantiate(this,reinforcementsSpawnPos);
                //TODO Asign instance same data as current police also make a method to shuffle instance patrol positions so that they dont go always together
            }
        }
    }

    #endregion

    #region Flee

    public void Flee() 
    {
        if (currentCorutine != null)
        {
            StopCoroutine(currentCorutine);
            currentCorutine = null;
            agent.SetDestination(transform.position);
        }

        currentCorutine = StartCoroutine(FleeCorutine());

        IEnumerator FleeCorutine()
        {
            agent.SetDestination(FleePos.position);
            yield return new WaitUntil(() => { return isPathComplete(); });
        }
    }

    #endregion

    #region Perceptions
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


    public bool checkHealth() 
    {
        //Returns True if low on health
        if (currentHealth < 20) 
        {
            return true;
        }
        return false;
    }

    #endregion


    #region Other
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



    bool isPathComplete()
    {
        return (!agent.pathPending &&
            agent.remainingDistance <= agent.stoppingDistance &&
            (!agent.hasPath || agent.velocity.sqrMagnitude == 0f));
    }


    private void OnDrawGizmos()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireCube(transform.position + new Vector3(0,1,0), new Vector3(15,1,15));
    }

    #endregion
}
