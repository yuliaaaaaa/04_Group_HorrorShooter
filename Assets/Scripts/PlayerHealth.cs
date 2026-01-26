using System;
using UnityEngine;

public class PlayerHealth : MonoBehaviour
{
    [SerializeField] private GameObject LoosePanel;
    
    [SerializeField] private int _maxHp;
    private int _hp;
    
    public int CurrentHP => _hp;

    private void Awake()
    { 
        _hp = _maxHp;
        LoosePanel.SetActive(false);
    }

    public void TakeDamage(int damage)
    {
        _hp -= damage;
        if (_hp <= 0)
        {
            _hp = 0;
            Die();
        }
    }

    private void Die()
    {
        LoosePanel.SetActive(true);
        Time.timeScale = 0;
    }
}
