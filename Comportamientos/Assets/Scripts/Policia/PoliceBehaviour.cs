using BehaviourAPI.UnityToolkit;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;
using DG.Tweening;
using BehaviourAPI.Core;
using System.Reflection;
using BehaviourAPI.UnityToolkit.GUIDesigner.Runtime;
using BehaviourAPI.StateMachines;
using Unity.VisualScripting;

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

public class PoliceBehaviour : AttackableEntity
{
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
    private Vision vision;

    [SerializeField]
    private PoliceStates state = PoliceStates.Patrol;


    public InvestigableObject investigableObject = null;
    private AttackableEntity attackableEntity = null;
    private bool enemyNotOnSight;
    private Tween enemyLostTween = null;
    private bool inCombat = false;

    // Start is called before the first frame update
    private void Awake()
    {
        agent = GetComponent<NavMeshAgent>();
        animator = GetComponent<Animator>();
    }

    #region Patrol
    public void Patrol()
    {
        enemyNotOnSight = true;
        state = PoliceStates.Patrol;
        thinkingCloudBehaviour.UpdateCloud(0);
        animator.SetBool("Investigate", false);
        animator.SetTrigger("Patrol");

        if (currentCorutine != null)
        {
            StopCoroutine(currentCorutine);
            currentCorutine = null;
            agent.SetDestination(transform.position);
        }
        currentCorutine = StartCoroutine(PatrolCorutine());

        IEnumerator PatrolCorutine()
        {
            while (true)
            {
                agent.stoppingDistance = 0.2f;
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
            agent.stoppingDistance = 0.2f;
            agent.SetDestination(investigatePostion); //Go to investigate position
            yield return new WaitUntil(() => { return isPathComplete(); }); //Wait for arrival at pos
            //Launch animation or sth and later return to patrol?
            transform.DOLookAt(investigableObject.transform.position, 0.5f, AxisConstraint.Y).OnComplete(() => { animator.SetBool("Investigate",true);});

            yield return new WaitForSeconds(investigableObject.investigateTime);
            Debug.Log("Finished investigating: " + investigableObject);
            investigableObject?.HasBeenInvestigated();
            vision.VisibleTriggers.Remove(investigableObject.transform);
            investigableObject = null;
            animator.SetBool("Investigate", false);
        }
    }
    #endregion

    #region Heal and Reinforcements

    public void Heal() 
    {
        thinkingCloudBehaviour.UpdateCloud(6);
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
        thinkingCloudBehaviour.UpdateCloud(7);
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
        thinkingCloudBehaviour.UpdateCloud(2);
        if (currentCorutine != null)
        {
            StopCoroutine(currentCorutine);
            currentCorutine = null;
            agent.SetDestination(transform.position);
        }

        currentCorutine = StartCoroutine(FleeCorutine());

        IEnumerator FleeCorutine()
        {
            agent.stoppingDistance = 0.2f;
            agent.SetDestination(FleePos.position);
            yield return new WaitUntil(() => { return isPathComplete(); });
            //GetComponent<EditorBehaviourRunner>().update missing
        }
    }

    #endregion

    #region Chase

    public void Chase() 
    {
        thinkingCloudBehaviour.UpdateCloud(3);
        inCombat = true;
        animator.SetInteger("Fear", paranoia);
        if (currentCorutine != null)
        {
            StopCoroutine(currentCorutine);
            currentCorutine = null;
            agent.SetDestination(transform.position);
        }

        currentCorutine = StartCoroutine(ChaseCorutine());

        IEnumerator ChaseCorutine()
        {
            if (attackableEntity != null)
            {
                Debug.Log("Chase");
                //TODO adjust stoppingDistance based on attack type
                agent.stoppingDistance = 5f;
                agent.SetDestination(attackableEntity.transform.position);
                animator.SetTrigger("Chase");
                yield return new WaitUntil(() => { return isPathComplete(); });
            }
        }
    }

    #endregion


    #region Attacks

    public void Hit() 
    {
        thinkingCloudBehaviour.UpdateCloud(4);
        inCombat = true;
        if (currentCorutine != null)
        {
            StopCoroutine(currentCorutine);
            currentCorutine = null;
            agent.SetDestination(transform.position);
        }
        agent.stoppingDistance = 2;
        currentCorutine = StartCoroutine(HitCorutine());

        IEnumerator HitCorutine()
        {
            //TODO adjust stoppingDistance based on attack type
            while (true)
            {
                
                if (attackableEntity != null && attackableEntity.isAlive)
                {
                    agent.SetDestination(attackableEntity.transform.position);
                    yield return new WaitUntil(() => { return isPathComplete(); });
                    //Hit animation and damage
                    
                    transform.LookAt(attackableEntity.transform.position);
                    animator.SetTrigger("Hit");
                    thinkingCloudBehaviour.UpdateCloud(4);
                    attackableEntity.ReceiveAttack(20);
                    yield return new WaitForSeconds(2);
                }
                else
                {
                    attackableEntity = null;
                    inCombat= false;
                    break;
                }
            }
        }

    }

    public void Shoot() 
    {
        thinkingCloudBehaviour.UpdateCloud(5);
        inCombat = true;
        if (currentCorutine != null)
        {
            StopCoroutine(currentCorutine);
            currentCorutine = null;
            agent.SetDestination(transform.position);
        }

        agent.stoppingDistance = 3;
        currentCorutine = StartCoroutine(ShootCorutine());

        IEnumerator ShootCorutine()
        {
            //TODO adjust stoppingDistance based on attack type
            while (true)
            {
                
                if (attackableEntity != null && attackableEntity.isAlive)
                {
                    agent.SetDestination(attackableEntity.transform.position);
                    yield return new WaitUntil(() => { return isPathComplete(); });
                    //Shoot animation and damage
                    animator.SetTrigger("Shoot");
                    attackableEntity.ReceiveAttack(50);
                    yield return new WaitForSeconds(2);
                }
                else 
                { 
                    attackableEntity = null;
                    inCombat= false;
                    break;
                } 
            }
        }

    }


    public override void ReceiveAttack(int damage)
    {
        base.ReceiveAttack(damage);
        animator.SetInteger("Health",currentHealth);
    }


    #endregion

    #region Perceptions
    public bool CheckInvestigate()
    {
        InvestigableObject temp;
        foreach (var trigger in vision.VisibleTriggers) 
        {
            if (trigger != null)
            {
                investigableObject = trigger.GetComponent<InvestigableObject>();
                if (investigableObject != null)
                {
                    if (investigableObject.ShouldInvestigate(paranoia))
                    {
                        return true;
                    }
                    else 
                    {
                        investigableObject = null;
                    }
                }
            }
        }
        return false;
    }

    public bool DetectEnemy()
    {
        foreach (var trigger in vision.VisibleTriggers)
        {
            if (trigger != null)
            {
                attackableEntity = trigger.GetComponent<Enemy>();
                if (attackableEntity != null)
                {
                    Debug.Log("Enemigo pillado");
                    enemyNotOnSight = false;
                    return true;
                }

            }
        }
        enemyNotOnSight = true;
        return false;
    }

    public Status DetectEnemyStatus()
    {
        foreach (var trigger in vision.VisibleTriggers)
        {
            attackableEntity = trigger.GetComponent<Enemy>();
            if (attackableEntity != null)
            {
                Debug.Log("Enemigo pillado success =======");
                enemyNotOnSight = false;
                return Status.Success;
            }
        }
        enemyNotOnSight = true;
        return Status.Failure;
    }

    public bool EnemyLost()
    {
        return enemyNotOnSight;
    }

    public Status EndBT() 
    {
        enemyNotOnSight = true;
        inCombat = false;
        vision.VisibleTriggers.Clear();
        return Status.Success;
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


    public Status CheckHealth() 
    {
        //Returns True if low on health
        if (currentHealth < 20) 
        {
            return Status.Success;
        }
        return Status.Failure;
    }

    public Status CheckDanger() 
    {
        //Returns True if low on danger
        if (paranoia < 70)
        {
            return Status.Success;
        }
        else
        {
            return Status.Failure;
        }
    }

    public Status CheckInCombat() 
    {
        if (!inCombat) 
        {
            Debug.Log("Finished Combat");
            return Status.Failure;
        }
        return Status.Running;
    }


    #endregion


    #region Other

    public bool isPathComplete()
    {
        return (!agent.pathPending &&
            agent.remainingDistance <= agent.stoppingDistance &&
            (!agent.hasPath || agent.velocity.sqrMagnitude == 0f));
    }

    public Status PathStatus()
    {
        if (!agent.pathPending &&
            agent.remainingDistance <= agent.stoppingDistance &&
            (!agent.hasPath || agent.velocity.sqrMagnitude == 0f))
        {
            return Status.Success;
        }
        return Status.Running;
    }


    /*
    private void OnDrawGizmos()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireCube(transform.position + new Vector3(0,1,0), new Vector3(15,1,15));
    }
    */
    #endregion
}
