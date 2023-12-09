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



public class BeastBehaviour : MonoBehaviour
{
    [SerializeField]
    private Vision vision;
    private Animator animator;

    [Header("Health")] 
    [SerializeField] 
    private int currentHealth = 500;
    public int maxHealth = 500;
    private int timeToHealSeconds = 20;

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
    

    private AttackableEntity attackableEntity = null;
    private NavMeshAgent agent;
    
    //Bools
    private bool inCombat = false;
    private bool enemyNotOnSight;
    
    private void Awake()
    {
        agent = GetComponent<NavMeshAgent>();
        animator = GetComponent<Animator>();
    }
    
    #region 1. HuntState

    public void Hunt()
    {
        
        enemyNotOnSight = true;
        animator.SetTrigger("Hunt");
        
        //Thinking cloud caminar
        thinkingCloudBehaviour.UpdateCloud(1  );
        if (currentCorutine != null)
        {
            StopCoroutine(currentCorutine);
            currentCorutine = null;
            agent.SetDestination(transform.position);
        }

        currentCorutine = StartCoroutine(HuntCorutine());

        IEnumerator HuntCorutine()
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
        Debug.Log("BestiaDescansa");
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
        Debug.Log("Bestia huye");
        animator.SetTrigger("Run");
        thinkingCloudBehaviour.UpdateCloud(2);
        Debug.Log("Beast Fleeing");
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
        Debug.Log("Bestia ataca!");
        agent.stoppingDistance = 0.2f;
        animator.SetTrigger("Run");
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
                    animator.SetBool("Hit Attack", true);
                    transform.LookAt(attackableEntity.transform.position);
                    thinkingCloudBehaviour.UpdateCloud(4);
                    attackableEntity.ReceiveAttack(50);
                    yield return new WaitForSeconds(2);
                }
                else
                {
                    
                    agent.stoppingDistance = 0.2f;
                    animator.SetTrigger("Run");
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
                    enemyNotOnSight = false;
                    return true;
                }

            }
        }
        enemyNotOnSight = true;
        return false;
    }
    
    public bool GetScared()
    {
        foreach(var trigger in vision.VisibleTriggers)
        {
            GhostBehaviour ghost = null;
            if (trigger != null)
            {
                ghost = trigger.GetComponent<GhostBehaviour>();
                if (ghost != null)
                {
                    return true;
                }
            }
        }

        return false;
    }
    
    public bool FinishCombat() 
    {
        if (!inCombat) 
        {
            return true;
        }
        return false;
    }


    public bool CheckFullHealth()
    {
        return currentHealth == 100;
    }
    
    #endregion

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
