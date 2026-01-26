using UnityEngine;
using UnityEngine.AI;

public class NPCControllerAI : MonoBehaviour
{
    private enum State { Idle, Patrol, Chase, Attack, Death }
    
    [SerializeField] private Transform player;
    
    [SerializeField] private Transform[] patrolPoints;
    [SerializeField] private float waitOnPoint = 1.5f;
    
    [SerializeField] private float detectDistance = 8f;
    [SerializeField] private float loseDistanceMultiplier = 1.5f;
    [SerializeField] private float attackEnterDistance = 1.5f;
    [SerializeField] private float attackExitDistance = 2.5f;
    
    [SerializeField] private float walkSpeed = 5f;
    [SerializeField] private float runSpeed = 8f;
    
    [SerializeField] private int damage = 10;
    [SerializeField] private float attackCooldown = 1.5f;

    private NavMeshAgent _agent;
    private Animator _animator;
    private NPCHealth _health;

    private State _state;
    private int _patrolIndex;
    private float _waitTimer;
    private float _nextAttackTime;

    private void Awake()
    {
        _agent = GetComponent<NavMeshAgent>();
        _animator = GetComponentInChildren<Animator>();
        _health = GetComponent<NPCHealth>();

        if (_animator != null)
            _animator.applyRootMotion = false;

        if (_health != null)
            _health.OnDied += OnDeath;

        _state = (patrolPoints != null && patrolPoints.Length > 0) ? State.Patrol : State.Idle;
    }

    private void Start()
    {
        if (player == null)
        {
            GameObject p = GameObject.FindGameObjectWithTag("Player");
            if (p != null) player = p.transform;
        }

        if (_state == State.Patrol) EnterPatrol();
        else EnterIdle();
    }

    private void Update()
    {
        if (_state == State.Death) return;
        if (_health != null && _health.IsDead) return;
        if (player == null) return;

        float dist = Vector3.Distance(transform.position, player.position);
        float loseDist = detectDistance * loseDistanceMultiplier;

        switch (_state)
        {
            case State.Idle:
                UpdateIdle(dist);
                break;
            case State.Patrol:
                UpdatePatrol(dist);
                break;
            case State.Chase:
                UpdateChase(dist, loseDist);
                break;
            case State.Attack:
                UpdateAttack(dist);
                break;
        }
    }
    
    private void UpdateIdle(float dist)
    {
        if (dist <= detectDistance)
        {
            ChangeState(State.Chase);
            return;
        }

        if (patrolPoints != null && patrolPoints.Length > 0)
        {
            ChangeState(State.Patrol);
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
        
        if (!_agent.pathPending && _agent.remainingDistance <= _agent.stoppingDistance + 0.1f)
        {
            _waitTimer += Time.deltaTime;
            SetIdle();

            if (_waitTimer >= waitOnPoint)
            {
                _waitTimer = 0f;
                GoToNextPatrolPoint();
                SetWalk();
            }
        }
        else
        {
            SetWalk();
        }
    }

    private void UpdateChase(float dist, float loseDist)
    {
        if (dist > loseDist)
        {
            ChangeState(State.Patrol);
            return;
        }

        if (dist <= attackEnterDistance)
        {
            ChangeState(State.Attack);
            return;
        }

        _agent.isStopped = false;
        _agent.speed = runSpeed;
        _agent.SetDestination(player.position);

        SetRun();
    }

    private void UpdateAttack(float dist)
    {
        if (dist >= attackExitDistance)
        {
            ChangeState(State.Chase);
            return;
        }
        
        FaceTarget(player.position);
        SetIdle();

        if (Time.time >= _nextAttackTime)
        {
            _nextAttackTime = Time.time + attackCooldown;

            if (_animator != null)
                _animator.SetTrigger("Attack");

            IDamageable dmg = player.GetComponent<IDamageable>();
            if (dmg != null)
            {
               //dmg.TakeDamage(damage);
            }
            PlayerHealth curHealth = player.GetComponent<PlayerHealth>();
            curHealth.TakeDamage(damage);
               
        }
    }
    

    private void ChangeState(State newState)
    {
        if (_state == newState) return;
        _state = newState;

        switch (_state)
        {
            case State.Idle:
                EnterIdle();
                break;

            case State.Patrol:
                EnterPatrol();
                break;

            case State.Chase:
                EnterChase();
                break;

            case State.Attack:
                EnterAttack();
                break;

            case State.Death:
                EnterDeath();
                break;
        }
    }

    private void EnterIdle()
    {
        StopAndSnapAgent();
        SetIdle();
    }

    private void EnterPatrol()
    {
        if (patrolPoints == null || patrolPoints.Length == 0)
        {
            EnterIdle();
            return;
        }

        _agent.updateRotation = true;
        _agent.isStopped = false;
        _agent.speed = walkSpeed;

        _waitTimer = 0f;
        
        _agent.ResetPath();
        _patrolIndex = GetClosestPatrolIndex();
        GoToPatrolPoint();

        SetWalk();
    }

    private void EnterChase()
    {
        _agent.updateRotation = true;
        _agent.isStopped = false;
        _agent.speed = runSpeed;
        
        _agent.ResetPath();

        SetRun();
    }

    private void EnterAttack()
    {
        _agent.updateRotation = false;
        StopAndSnapAgent();
        SetIdle();
        
    }

    private void EnterDeath()
    {
        _agent.updateRotation = false;
        StopAndSnapAgent();
        SetIdle();
    }
    

    private void StopAndSnapAgent()
    {
        if (_agent == null) return;

        _agent.isStopped = true;
        _agent.ResetPath();
        
        _agent.nextPosition = transform.position;
    }
    
    private int GetClosestPatrolIndex()
    {
        int best = 0;
        float bestDist = float.PositiveInfinity;

        for (int i = 0; i < patrolPoints.Length; i++)
        {
            if (patrolPoints[i] == null) continue;

            float d = (patrolPoints[i].position - transform.position).sqrMagnitude;
            if (d < bestDist)
            {
                bestDist = d;
                best = i;
            }
        }

        return best;
    }

    private void GoToPatrolPoint()
    {
        if (patrolPoints == null || patrolPoints.Length == 0) return;
        if (patrolPoints[_patrolIndex] == null) return;

        _patrolIndex = Mathf.Clamp(_patrolIndex, 0, patrolPoints.Length - 1);
        _agent.SetDestination(patrolPoints[_patrolIndex].position);
    }

    private void GoToNextPatrolPoint()
    {
        if (patrolPoints == null || patrolPoints.Length == 0) return;

        int tries = 0;
        do
        {
            _patrolIndex = (_patrolIndex + 1) % patrolPoints.Length;
            tries++;
        } while (tries < patrolPoints.Length && patrolPoints[_patrolIndex] == null);

        GoToPatrolPoint();
    }
    

    private void FaceTarget(Vector3 targetPos)
    {
        Vector3 dir = targetPos - transform.position;
        dir.y = 0f;

        if (dir.sqrMagnitude < 0.0001f) return;

        Quaternion rot = Quaternion.LookRotation(dir);
        transform.rotation = Quaternion.Slerp(transform.rotation, rot, Time.deltaTime * 10f);
    }

    private void OnDeath()
    {
        ChangeState(State.Death);
    }

    private void SetIdle()
    {
        if (_animator != null) _animator.SetFloat("MoveSpeed", 0f);
    }

    private void SetWalk()
    {
        if (_animator != null) _animator.SetFloat("MoveSpeed", 0.5f);
    }

    private void SetRun()
    {
        if (_animator != null) _animator.SetFloat("MoveSpeed", 1f);
    }
}
