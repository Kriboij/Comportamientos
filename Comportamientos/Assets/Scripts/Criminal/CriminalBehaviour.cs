using BehaviourAPI.Core;
using BehaviourAPI.Core.Actions;
using BehaviourAPI.Core.Perceptions;
using BehaviourAPI.StateMachines;
using DG.Tweening;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.InteropServices.WindowsRuntime;
using UnityEngine;
using UnityEngine.AI;

public class CriminalBehaviour : MonoBehaviour
{

    [SerializeField] Vision vision;
    FSM fsm;

    [SerializeField] float fleeTime;

    [Header("Health")]
    [SerializeField]
    private int health = 100;

    [Header("Patrol")]
    [SerializeField]
    private List<Transform> patrolPositions;
    private int currentPatrolIndex = 0;
    

    [Header("Thinking bubble")]
    [SerializeField]
    ThinkingCloudBehaviour thinkingCloudBehaviour;

    private NavMeshAgent agent;
    private Animator animator;


    // Start is called before the first frame update
    private void Awake()
    {
        agent = GetComponent<NavMeshAgent>();
        animator = GetComponent<Animator>();

        fsm = new FSM();

        FunctionalAction patrollingAction = new FunctionalAction(StartPatrolling, Patrolling, null); //Estado
        State patrolling = fsm.CreateState(patrollingAction);

        FunctionalAction watchPoliceAction = new FunctionalAction(StartWatchPolice, WatchPolice, null); //Estado
        State watchPolice = fsm.CreateState(watchPoliceAction);

        ConditionPerception checkPolice = new ConditionPerception(null, IsWatchingPoliceman, null); //Trancisión para la conexión
        fsm.CreateTransition(patrolling, watchPolice, checkPolice, statusFlags: StatusFlags.Running); // 1.Dde donde partimos, 2. Estado al que pasamos, 3. Condicion 1, 4. Condicion 2

        FunctionalAction fleeAction = new FunctionalAction(StartFlee, Fleeing, null); //Estado
        State flee = fsm.CreateState(fleeAction);

        TimerPerception fleeTimer = new TimerPerception(fleeTime);

        fsm.CreateTransition(watchPolice, flee, fleeTimer, statusFlags: StatusFlags.Running); //Transicion Ver Policia y Huir


        ConditionPerception noCheckPolice = new ConditionPerception(null,()=> { return !IsWatchingPoliceman(); }, null);
        AndPerception timeAndNoWatch = new AndPerception(noCheckPolice, fleeTimer);

        fsm.CreateTransition(flee, patrolling, timeAndNoWatch, statusFlags: StatusFlags.Running); //Transicion huir y patrullar

        fsm.SetEntryState(patrolling);
        fsm.Start();
    }

    private void Update()
    {
        fsm.Update();
    }

    #region 1. PATROL STATE
    void StartPatrolling()
    {
        thinkingCloudBehaviour.UpdateCloud(0);
        agent.isStopped = false;
    }

    public Status Patrolling()
    {
        if (isPathComplete())
        {
            ChangePatrolPoint(1);
        }
        return Status.Running;

    }
    #endregion


    #region 2. WATCH STATE
    void StartWatchPolice()
    {
        thinkingCloudBehaviour.UpdateCloud(1);
        agent.isStopped = true;
    }

    public Status WatchPolice()
    {
        return Status.Running;
    }

    bool IsWatchingPoliceman()
    {
        foreach(var trigger in vision.VisibleTriggers)
        {
            var police = trigger.GetComponent<PoliceBehaviour>();
            if(police != null)
            {
                return true;
            }
        }
        return false;
    }
    #endregion


    #region 3. FLEE STATE

    void StartFlee()
    {
        thinkingCloudBehaviour.UpdateCloud(2);
        agent.isStopped = false;
        ChangePatrolPoint(-1);
    }

    public Status Fleeing()
    {
        if(isPathComplete())
        {
            ChangePatrolPoint(-1);
        }
        return Status.Running;
    }
    #endregion

    void ChangePatrolPoint(int change)
    {
        if(patrolPositions.Count == 0)
        {
            return;
        }
        agent.SetDestination(patrolPositions[currentPatrolIndex].position);
        currentPatrolIndex += change;
        currentPatrolIndex %= patrolPositions.Count;
        if(currentPatrolIndex < 0)
        {
            currentPatrolIndex = patrolPositions.Count - 1;
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
        Gizmos.DrawWireCube(transform.position + new Vector3(0, 1, 0), new Vector3(15, 1, 15));
    }

}
