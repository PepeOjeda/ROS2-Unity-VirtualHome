using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SimulationSettings : MonoBehaviour
{
    [SerializeField] int targetFrameRate = 60;
    void Start()
    {
        Application.targetFrameRate = targetFrameRate;
    }

    void OnValidate()
    {
        Application.targetFrameRate = targetFrameRate;
    }

}
