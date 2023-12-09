using BehaviourAPI.Core;
using BehaviourAPI.Core.Actions;
using BehaviourAPI.Core.Perceptions;
using BehaviourAPI.StateMachines;
using DG.Tweening;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.InteropServices.WindowsRuntime;
using UnityEditor;
using UnityEngine;
using UnityEngine.AI;

enum BeastStates 
{
    Hunt,
    Eating
}

public class BeastBehaviour : MonoBehaviour
{
    [SerializeField]
    private Vision vision;

    private int timeToHealSeconds = 20;

    [Header("Health")] 
    [SerializeField] 
    private int currentHealth = 100;
    public int maxHealth = 100;

    [Header("Flee")]
    [SerializeField]
    private Transform fleePos;
    
    [Header("Hunt")]
    [SerializeField]
    private List<Transform> huntPositions;
    private int currentHuntIndex = 0;
    private Coroutine currentCorutine = null;
    
    [Header("Thinking bubble")]
    [SerializeField]
    ThinkingCloudBehaviour thinkingCloudBehaviour;
    
    [SerializeField]
    private BeastStates state = BeastStates.Hunt;

    private Animator animator;
    private bool enemyNotOnSight;
    private AttackableEntity attackableEntity = null;
    private bool inCombat = false;
    private NavMeshAgent agent;
    
    // Start is called before the first frame update
    private void Awake()
    {
        Debug.Log("me despierto zzz");
        agent = GetComponent<NavMeshAgent>();
        animator = GetComponent<Animator>();
    }
    
    #region 1. HuntState

    public void Hunt()
    {
        Debug.Log("Estoy cazando");
        enemyNotOnSight = true;
        
        thinkingCloudBehaviour.UpdateCloud(1  );

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
                agent.SetDestination(huntPositions[currentHuntIndex].position);
                yield return new WaitUntil(() => { return IsPathComplete(); });
                currentHuntIndex++;
                currentHuntIndex %= huntPositions.Count;
            }
        }
    }

    #endregion
    
    #region 2. RestState

    public void Rest() 
    {
        thinkingCloudBehaviour.UpdateCloud(3);
        if (currentCorutine != null)
        {
            StopCoroutine(currentCorutine);
            currentCorutine = null;
            agent.SetDestination(transform.position);
        }

        currentCorutine = StartCoroutine(RestCorutine());

        IEnumerator RestCorutine() 
        {
            
            animator.SetTrigger("Idle");
            yield return new WaitForSeconds(timeToHealSeconds);
            currentHealth = maxHealth;
        }
    }
    #endregion
    
    #region 3. FleeState
    
    public void Flee() 
    {
        animator.SetTrigger("Run");
        thinkingCloudBehaviour.UpdateCloud(2);
        Debug.Log("Fleeing");
        if (currentCorutine != null)
        {
            StopCoroutine(currentCorutine);
            currentCorutine = null;
            agent.SetDestination(transform.position);
        }

        currentCorutine = StartCoroutine(FleeingCorutine());

        IEnumerator FleeingCorutine()
        {
                agent.stoppingDistance = 0.2f;
                agent.SetDestination(fleePos.position);
                yield return new WaitUntil(() => { return IsPathComplete(); });
            
        }
    }

    #endregion

    #region 4. FightState

    public void Fight() 
    {
        thinkingCloudBehaviour.UpdateCloud(2);
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
                    yield return new WaitUntil(() => { return IsPathComplete(); });
                    //Hit animation and damage
                    
                    animator.SetTrigger("Hit Attack");
                    
                    transform.LookAt(attackableEntity.transform.position);
                    Debug.Log("Gorilla paunch!");
                    thinkingCloudBehaviour.UpdateCloud(4);
                    attackableEntity.ReceiveAttack(50);
                    yield return new WaitForSeconds(2);
                }
                else
                {
                    Debug.Log("Ñam");
                    attackableEntity = null;
                    inCombat= false;
                    break;
                }
            }
        }

    }

    #endregion

    
    #region Perception

    
    public bool DetectEnemy()
    {
        foreach (var trigger in vision.VisibleTriggers)
        {
            if (trigger != null)
            {
                attackableEntity = trigger.GetComponent<PreyEntity>();
                if (attackableEntity != null)
                {
                    Debug.Log("Presa Localizada");
                    enemyNotOnSight = false;
                    return true;
                }

            }
        }
        enemyNotOnSight = true;
        return false;
    }
    
    
    public bool FinishedCombat()
    {
        return !inCombat;
    }
    
    #endregion
    


    public bool CheckFullHealth()
    {
        return currentHealth == 100;
    }

    public bool IsPathComplete()
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
}
