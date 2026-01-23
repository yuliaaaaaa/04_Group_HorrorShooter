using UnityEngine;
using UnityEngine.AI;

public class NPCControllerAI : MonoBehaviour
{
    private enum State { Idle, Patrol, Chase, Attack, Death }
    
    [SerializeField] private Transform player;
    private NavMeshAgent agent;
    private Animator animator;
    private NPCHealth health;
    
    public Transform[] patrolPoints;
    [SerializeField] private float vaitOnPoint = 1.5f; 
    private float _waitTimer;
    
    public float detectDistance = 8f;
    [SerializeField] private float loseDistanceMultiplier = 1.5f;
    public float attackEnterDistance = 1.5f;
    public float attackExitDistance = 2.5f;
    
    public float walkSpeed = 5f;
    public float runSpeed = 8f;
    
    public int damage = 10;
    public float attackCalldown = 1.5f;

    private State _state;
    private int _patrolIndex;
    private float _nextAttackTime;

    private void Awake()
    {
        agent = GetComponent<NavMeshAgent>();
        animator = GetComponent<Animator>();
        health = GetComponent<NPCHealth>();

        if (health != null)
            health.OnDied += OnDeath;

        _state = (patrolPoints != null && patrolPoints.Length > 0) ? State.Patrol : State.Idle;
    }

    private void Start()
    {
        if (player == null)
        {
            GameObject p = GameObject.FindGameObjectWithTag("Player");
            if (p != null) player = p.transform;
        }

        if (_state == State.Patrol)
        {
            SetWalk();
            GoToPatrolPoint();
        }
        else
        {
            SetIdle();
        }
    }

    private void Update()
    {
        if (_state == State.Death) return;
        if (health != null && health.IsDead) return;
        if (player == null) return;

        float dist = Vector3.Distance(transform.position, player.position);
        float loseDistance = detectDistance * loseDistanceMultiplier;

        switch (_state)
        {
            case State.Idle:
                UpdateIdle(dist);
                break;

            case State.Patrol:
                UpdatePatrol(dist);
                break;

            case State.Chase:
                UpdateChase(dist, loseDistance);
                break;

            case State.Attack:
                UpdateAttack(dist);
                break;
        }
    }

    private void UpdateIdle(float dist)
    {
        if (patrolPoints != null && patrolPoints.Length > 0)
        {
            ChangeState(State.Patrol);
            return;
        }
        
        if (dist <= detectDistance)
        {
            ChangeState(State.Chase);
        }
    }

    private void UpdatePatrol(float dist)
    {
        if (dist <= detectDistance)
        {
            ChangeState(State.Chase);
            return;
        }

        if (patrolPoints == null || patrolPoints.Length == 0)
        {
            ChangeState(State.Idle);
            return;
        }
        
        /*
        if (!agent.pathPending && agent.remainingDistance <= agent.stoppingDistance + 0.1f)
        {
            _waitTimer += Time.deltaTime;

            SetIdle(); 

            if (_waitTimer >= vaitOnPoint)
            {
                _waitTimer = 0f;
                GoToNextPatrolPoint();
                SetWalk();
            }
        }*/
        if (!agent.pathPending && agent.remainingDistance <= agent.stoppingDistance + 0.1f)
        {
            _waitTimer += Time.deltaTime;

            SetIdle(); 

            if (_waitTimer >= vaitOnPoint)
            {
                _waitTimer = 0f;
                GoToNextPatrolPoint();
                SetWalk();
            }
        }
        
    }

    private void UpdateChase(float dist, float loseDistance)
    {
        if (dist > loseDistance)
        {
            ChangeState(State.Patrol);
            return;
        }
        
        if (dist <= attackEnterDistance)
        {
            ChangeState(State.Attack);
            return;
        }
        
        agent.isStopped = false;
        agent.speed = runSpeed;
        agent.SetDestination(player.position);

        SetRun();
    }

    private void UpdateAttack(float dist)
    {
        if (dist >= attackExitDistance)
        {
            ChangeState(State.Chase);
            return;
        }

        agent.isStopped = true;
        FaceTarget(player.position);
        
        if (Time.time >= _nextAttackTime)
        {
            _nextAttackTime = Time.time + attackCalldown;
            
            animator.SetTrigger("Attack");
            
            IDamageable dmg = player.GetComponentInParent<IDamageable>();
            if (dmg != null)
                dmg.TakeDamage(damage);
        }

        SetIdle(); 
    }

    private void ChangeState(State newState)
    {
        _state = newState;

        if (_state == State.Patrol)
        {
            agent.isStopped = false;
            agent.speed = walkSpeed;
            SetWalk();

            _waitTimer = 0f;
            GoToPatrolPoint();
        }
        else if (_state == State.Chase)
        {
            agent.isStopped = false;
            agent.speed = runSpeed;
            SetRun();
        }
        else if (_state == State.Attack)
        {
            agent.isStopped = true;
            SetIdle();
        }
        else if (_state == State.Idle)
        {
            agent.isStopped = true;
            SetIdle();
        }
    }

    private void GoToPatrolPoint()
    {
        if (patrolPoints == null || patrolPoints.Length == 0) return;

        _patrolIndex = Mathf.Clamp(_patrolIndex, 0, patrolPoints.Length - 1);
        agent.SetDestination(patrolPoints[_patrolIndex].position);
    }

    private void GoToNextPatrolPoint()
    {
        if (patrolPoints == null || patrolPoints.Length == 0) return;

        _patrolIndex = (_patrolIndex + 1) % patrolPoints.Length;
        agent.SetDestination(patrolPoints[_patrolIndex].position);
    }

    private void FaceTarget(Vector3 targetPos)
    {
        Vector3 dir = (targetPos - transform.position);
        dir.y = 0f;

        if (dir.sqrMagnitude < 0.01f) return;

        Quaternion rot = Quaternion.LookRotation(dir);
        transform.rotation = Quaternion.Slerp(transform.rotation, rot, Time.deltaTime * 10f);
    }

    private void OnDeath()
    {
        _state = State.Death;
        agent.isStopped = true;

        SetIdle();
    }
    
    private void SetIdle()
    {
        animator.SetFloat("MoveSpeed", 0f);
    }

    private void SetWalk()
    {
        animator.SetFloat("MoveSpeed", 0.5f);
    }

    private void SetRun()
    {
        animator.SetFloat("MoveSpeed", 1f);
    }
}
