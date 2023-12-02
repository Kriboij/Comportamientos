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


    private static readonly int IdleState = Animator.StringToHash("Base Layer.idle");
    private static readonly int MoveState = Animator.StringToHash("Base Layer.move");
    private static readonly int AttackState = Animator.StringToHash("Base Layer.attack_shift");


    private NavMeshAgent agent;
    private Animator animator;

    private void Awake()
    {
        agent = GetComponent<NavMeshAgent>();
        animator = GetComponent<Animator>();
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

        while(random == lastPosition)
        {
            random = Random.Range(0, patrolPositions.Count);
        }

        lastPosition = random;

        return random;
    }



    // Start is called before the first frame update
    void Start()
    {
        Patrol();
    }



    // Update is called once per frame
    void Update()
    {
        
    }
}
