using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TimeScaler : MonoBehaviour
{
    [SerializeField] float timeScale = 1;
    void Start()
    {
        Time.timeScale = timeScale;
    }

    void OnValidate()
    {
        Time.timeScale = timeScale;
    }
}
