using System;
using UnityEngine;
using UnityEngine.AI;

public class NPCHealth : MonoBehaviour, IDamageable
{
    [SerializeField] private int maxHP;

    private int _hp;
    private bool _isDead;
    
    public bool IsDead => _isDead;

    public event Action OnDied;

    private Animator _animator;
    private NavMeshAgent _agent;
    private Collider _collider;

    private void Awake()
    {
        _hp = maxHP; 
        
        _animator = GetComponent<Animator>();
        _agent = GetComponent<NavMeshAgent>();
        _collider = GetComponent<Collider>();
    }

    public void TakeDamage(int damage)
    {
        if(_isDead) return;
        _hp -= damage;

        if (_hp <= 0)
        {
            _hp = 0;
            Die();
        }
    }

    private void Die()
    {
        _isDead = true;

        if (_agent != null)
        {
            _agent.isStopped = true;
            _agent.enabled = false;
        }

        _animator.SetTrigger("Die");
        OnDied?.Invoke();
        Destroy(this.gameObject);
    }
}

