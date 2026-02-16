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

    private bool _hordeMode = false;

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

        if (!_hordeMode && GameTimer.Instance != null && GameTimer.Instance.IsHordeActive)
        {
            ActivateHordeMode();
        }

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

        UpdateAnimator();
    }

    public void ActivateHordeMode()
    {
        if (_hordeMode) return;

        _hordeMode = true;
        Debug.Log($"{gameObject.name} увійшов в режим орди!");
        if (_state != State.Attack && _state != State.Death)
        {
            ChangeState(State.Chase);
        }
    }

    private void UpdateIdle(float dist)
    {
        if (_hordeMode)
        {
            ChangeState(State.Chase);
            return;
        }

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
        if (_hordeMode)
        {
            ChangeState(State.Chase);
            return;
        }

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

            if (_waitTimer >= waitOnPoint)
            {
                _waitTimer = 0f;
                GoToNextPatrolPoint();
            }
        }
    }

    private void UpdateChase(float dist, float loseDist)
    {
        if (!_hordeMode && dist > loseDist)
        {
            ChangeState(State.Patrol);
            return;
        }

        if (dist <= attackEnterDistance)
        {
            ChangeState(State.Attack);
            return;
        }

        if (_agent == null || !_agent.enabled || !_agent.isOnNavMesh) return;

        _agent.isStopped = false;
        _agent.SetDestination(player.position);
    }

    private void UpdateAttack(float dist)
    {
        if (dist >= attackExitDistance)
        {
            ChangeState(State.Chase);
            return;
        }

        if (_agent != null && _agent.enabled && _agent.isOnNavMesh)
        {
            _agent.isStopped = true;
        }

        FaceTarget(player.position);

        if (Time.time >= _nextAttackTime)
        {
            _nextAttackTime = Time.time + attackCooldown;

            if (_animator != null)
                _animator.SetTrigger("Attack");

            PlayerHealth playerHealth = player.GetComponent<PlayerHealth>();
            if (playerHealth != null)
            {
                playerHealth.TakeDamage(damage);
            }
        }
    }

    private void UpdateAnimator()
    {
        if (_animator == null) return;

        float targetSpeed = 0f;

        switch (_state)
        {
            case State.Idle:
                targetSpeed = 0f;
                break;

            case State.Patrol:
                if (_agent != null && _agent.enabled && _agent.isOnNavMesh)
                {
                    if (_agent.velocity.sqrMagnitude > 0.1f)
                        targetSpeed = 0.5f;
                    else
                        targetSpeed = 0f;
                }
                break;

            case State.Chase:
                if (_agent != null && _agent.enabled && _agent.isOnNavMesh)
                {
                    if (_agent.velocity.sqrMagnitude > 0.1f)
                        targetSpeed = 1f;
                    else
                        targetSpeed = 0f;
                }
                break;

            case State.Attack:
                targetSpeed = 0f;
                break;

            case State.Death:
                targetSpeed = 0f;
                break;
        }

        float currentSpeed = _animator.GetFloat("MoveSpeed");
        float smoothSpeed = Mathf.Lerp(currentSpeed, targetSpeed, Time.deltaTime * 10f);
        _animator.SetFloat("MoveSpeed", smoothSpeed);
    }

    private void ChangeState(State newState)
    {
        if (_state == newState) return;

        ExitState(_state);
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

    private void ExitState(State oldState)
    {
        switch (oldState)
        {
            case State.Attack:
                if (_agent != null && _agent.enabled && _agent.isOnNavMesh)
                {
                    _agent.updateRotation = true;
                    _agent.isStopped = false;
                }
                break;
        }
    }

    private void EnterIdle()
    {
        StopAndSnapAgent();
    }

    private void EnterPatrol()
    {
        if (patrolPoints == null || patrolPoints.Length == 0)
        {
            EnterIdle();
            return;
        }

        if (_agent == null || !_agent.enabled || !_agent.isOnNavMesh) return;

        _agent.isStopped = false;
        _agent.updateRotation = true;
        _agent.speed = walkSpeed;

        _agent.ResetPath();

        _waitTimer = 0f;
        _patrolIndex = GetClosestPatrolIndex();
        GoToPatrolPoint();
    }

    private void EnterChase()
    {
        if (_agent == null || !_agent.enabled || !_agent.isOnNavMesh)
        {
            Debug.LogError("Cannot enter Chase - agent is not valid!");
            return;
        }

        _agent.isStopped = false;
        _agent.updateRotation = true;
        _agent.speed = runSpeed;

        _agent.ResetPath();
    }

    private void EnterAttack()
    {
        if (_agent == null || !_agent.enabled || !_agent.isOnNavMesh) return;

        _agent.isStopped = true;
        _agent.updateRotation = false;
        _agent.ResetPath();
    }

    private void EnterDeath()
    {
        StopAndSnapAgent();

        if (_agent != null && _agent.enabled)
        {
            _agent.enabled = false;
        }
    }

    private void StopAndSnapAgent()
    {
        if (_agent == null || !_agent.enabled || !_agent.isOnNavMesh) return;

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
        if (_agent == null || !_agent.enabled || !_agent.isOnNavMesh) return;

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
}