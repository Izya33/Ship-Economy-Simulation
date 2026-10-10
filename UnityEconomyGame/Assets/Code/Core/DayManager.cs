using System;
using UnityEngine;
using UnityEngine.InputSystem.Controls;

public class DayManager : MonoBehaviour
{
    [SerializeField] private float dayDuration = 60f;

    private float timer;
    public int CurrentDay { get; private set; } = 1;
    public event Action OnNewDay;

    void Update()
    {
        timer += Time.deltaTime;
        if(timer >= dayDuration)
        {
            timer=0;
            CurrentDay++; 
            Debug.Log("Today is "+CurrentDay);
            OnNewDay?.Invoke();
        }
    }
}
