using BehaviourAPI.Core;
using BehaviourAPI.Core.Actions;
using BehaviourAPI.Core.Perceptions;
using BehaviourAPI.StateMachines;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

public class CriminalBehaviour : MonoBehaviour
{

    [SerializeField] Vision vision;
    FSM fsm;
    ExplorerBehaviour explorer;

    [SerializeField] float fleeTime;
    [SerializeField] float killTime;
    [SerializeField] float distanciaMuerteExplroador;

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

    [SerializeField] float patrollSpeed;
    [SerializeField] float killSpeed;
    [SerializeField] float fleeSpeed;

    private NavMeshAgent agent;
    private Animator animator;


    // Start is called before the first frame update
    private void Awake()
    {
        agent = GetComponent<NavMeshAgent>();
        animator = GetComponent<Animator>();

        fsm = new FSM();

        //Patrullar a mirar al policia

        FunctionalAction patrollingAction = new FunctionalAction(StartPatrolling, Patrolling, null); //Estado
        State patrolling = fsm.CreateState(patrollingAction);

        FunctionalAction watchPoliceAction = new FunctionalAction(StartWatchPolice, WatchPolice, null); //Estado
        State watchPolice = fsm.CreateState(watchPoliceAction);

        ConditionPerception checkPolice = new ConditionPerception(null, vision.IsWatchingPoliceman, null); //Trancisión para la conexión
        fsm.CreateTransition(patrolling, watchPolice, checkPolice, statusFlags: StatusFlags.Running); // 1.Dde donde partimos, 2. Estado al que pasamos, 3. Condicion 1, 4. Condicion 2


        //Huir

        FunctionalAction fleeAction = new FunctionalAction(StartFlee, Fleeing, null); //Estado
        State flee = fsm.CreateState(fleeAction);

        TimerPerception fleeTimer = new TimerPerception(fleeTime);

        fsm.CreateTransition(watchPolice, flee, fleeTimer, statusFlags: StatusFlags.Running); //Transicion Ver Policia y Huir


        //De huir a patrullar

        ConditionPerception noCheckPolice = new ConditionPerception(null,()=> { return !vision.IsWatchingPoliceman(); }, null);
        AndPerception timeAndNoWatch = new AndPerception(noCheckPolice, fleeTimer);

        fsm.CreateTransition(flee, patrolling, timeAndNoWatch, statusFlags: StatusFlags.Running); //Transicion huir y patrullar

        //De patrullar a Mirar al explrador

        FunctionalAction watchExplorerAction = new FunctionalAction(StartWatchExplorer, WatchExplorer, null); //Estado
        State watchExplorer = fsm.CreateState(watchExplorerAction);

        ConditionPerception checkExplorer = new ConditionPerception(null, vision.IsWatchingExplorer, null);
        fsm.CreateTransition(patrolling, watchExplorer, checkExplorer, statusFlags: StatusFlags.Running);

        //De mirar al explorador a matar

        FunctionalAction killAction = new FunctionalAction(StartKill, Killing, null); //Estado
        State kill = fsm.CreateState(killAction);

        TimerPerception killTimer = new TimerPerception(fleeTime);

        fsm.CreateTransition(watchExplorer, kill, killTimer, statusFlags: StatusFlags.Running); //Transicion Ver Policia y Huir

        //De matar a patrullar

        ConditionPerception noCheckExplorer = new ConditionPerception(null, () => { return explorer == null; }, null);
        fsm.CreateTransition(kill, patrolling, noCheckExplorer, statusFlags: StatusFlags.Success); //Transicion huir y patrullar

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
        agent.speed = patrollSpeed;
        animator.SetTrigger("Patrulla");
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


    #region 2. WATCH POLICE STATE
    void StartWatchPolice()
    {
        thinkingCloudBehaviour.UpdateCloud(1);
        agent.isStopped = true;
    }

    public Status WatchPolice()
    {
        return Status.Running;
    }

    #endregion

    #region 3. FLEE STATE

    void StartFlee()
    {
        thinkingCloudBehaviour.UpdateCloud(2);
        agent.isStopped = false;
        ChangePatrolPoint(-1);
        animator.SetTrigger("Huir");
    }

    public Status Fleeing()
    {
        if(isPathComplete())
        {
            agent.speed = fleeSpeed;
            ChangePatrolPoint(-1);
        }
        return Status.Running;
    }
    #endregion

    #region 4. WATCH EXPLORER

    void StartWatchExplorer()
    {
        thinkingCloudBehaviour.UpdateCloud(1);
        agent.isStopped = true;
    }

    public Status WatchExplorer()
    {
        return Status.Running;
    }

    #endregion

    #region 5. KILL

    void StartKill()
    {
        thinkingCloudBehaviour.UpdateCloud(1);
        agent.isStopped = false;
        agent.speed = killSpeed;
        animator.SetTrigger("Perseguir");

        foreach (var visibleTrigger in vision.VisibleTriggers)
        {
            explorer = visibleTrigger.GetComponent<ExplorerBehaviour>();
            if(explorer != null)
            {
                return;
            }
        }
        

    }

    public Status Killing()
    {
        if(explorer != null)
        {
            agent.SetDestination(explorer.transform.position);

            if(Vector3.Distance(agent.transform.position,explorer.transform.position) < distanciaMuerteExplroador)
            {
                animator.SetTrigger("Kill");
                Destroy(explorer.gameObject);
            }

            return Status.Running;
        }
        else
        {
            return Status.Success;
        }
        
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

   

}
