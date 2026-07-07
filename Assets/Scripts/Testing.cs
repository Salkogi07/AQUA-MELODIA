using System;
using UnityEngine;

public class Testing : MonoBehaviour
{
    [Range(0f, 5f)] public float gameTimeSet = 1;

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.T))
        {
            Time.timeScale = gameTimeSet;
        }
    }
}
