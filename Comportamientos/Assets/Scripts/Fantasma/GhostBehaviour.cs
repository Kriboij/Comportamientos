using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEditor;
using UnityEditor.Timeline;
using UnityEngine;
using UnityEngine.AI;

public class GhostBehaviour : MonoBehaviour
{
    [Header("Patrol")]
    [SerializeField]
    private List<Transform> patrolPositions;
    private int currentPatrolIndex = 0;
    private Coroutine patrolCorutine = null;
    private int lastPosition;

    [Header("Thinking bubble")]
    [SerializeField]
    ThinkingCloudBehaviour thinkingCloudBehaviour;

    [Header("Scare")]
    [SerializeField]
    private Coroutine scareCorutine = null;



    private static readonly int IdleState = Animator.StringToHash("Base Layer.idle");
    private static readonly int MoveState = Animator.StringToHash("Base Layer.move");
    private static readonly int AttackState = Animator.StringToHash("Base Layer.attack_shift");


    private NavMeshAgent agent;
    private Animator animator;
    private Vision vision;
    private ScareObject entity;

    private void Awake()
    {
        agent = GetComponent<NavMeshAgent>();
        animator = GetComponent<Animator>();
        vision = GetComponentInChildren<Vision>();
    }

    #region PATROL
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

                currentPatrolIndex = GeneratePosition();
                agent.SetDestination(patrolPositions[currentPatrolIndex].position);
                yield return new WaitUntil(() => { return isPathComplete(); });
                yield return new WaitForSeconds(1);

                //animator.CrossFade(MoveState, 1f, 0, 0);
            }
        }

        animator.CrossFade(IdleState, 0.1f, 0, 0);

    }

    bool isPathComplete()
    {
        return (!agent.pathPending &&
            agent.remainingDistance <= agent.stoppingDistance &&
            (!agent.hasPath || agent.velocity.sqrMagnitude == 0f));
    }

    private int GeneratePosition()
    {
        int random = Random.Range(0, patrolPositions.Count);

        while (random == lastPosition)
        {
            random = Random.Range(0, patrolPositions.Count);
        }

        lastPosition = random;

        return random;
    }
    #endregion

    #region SCARE
    public void Scare()
    {
        if (scareCorutine != null)
        {
            StopCoroutine(scareCorutine);
            patrolCorutine = null;
        }
        scareCorutine = StartCoroutine(ScareCorutine());



        IEnumerator ScareCorutine()
        {
            while (true)
            {
                entity.Escape();
                yield return new WaitForSeconds(1);

                //animator.CrossFade(MoveState, 1f, 0, 0);
            }
        }
        animator.CrossFade(AttackState, 0.1f, 0, 0);

    }
    #endregion

    #region VISION

    public bool IsWatchingScareObject()
    {
        foreach (var trigger in vision.VisibleTriggers)
        {
            var entityAux = trigger.GetComponent<ScareObject>();
            if (entityAux != null)
            {
                entity = entityAux;
                return true;
                
            }
            Debug.Log("Viendo");

        }
        return false;
    }

    public bool IsNotWatchingScareObject()
    {
        foreach (var trigger in vision.VisibleTriggers)
        {
            var entityAux = trigger.GetComponent<ScareObject>();
            if (entityAux != null)
            {
                return false;
            }
            
        }
        return true;
    }
    #endregion


}
