using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EnemyHealth : MonoBehaviour
{
    [SerializeField] private float healthPoint = 10f;

    public float HealthPoint {get;}

    public void SetHealthPoint(float value){
        healthPoint = value;
    }

    public void AddHealthPoint(float value){
        healthPoint = value;
    }

    public void TakeDamage(float damagePoint){
        healthPoint -= damagePoint;
        CheckHP();
    }

    private void CheckHP(){
        if(healthPoint <= 0){
            Destroy(gameObject);
        }
    }
}
