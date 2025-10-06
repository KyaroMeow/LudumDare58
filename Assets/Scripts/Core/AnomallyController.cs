using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AnomallyController : MonoBehaviour
{
    [SerializeField] private GameObject[] anomallyBalls;
    [SerializeField] private GameObject anomallyObject;
    private int positionId = 0;

    public void ClickOnBall()
    {
        if (positionId < anomallyBalls.Length - 1)
        {
            GoToNextPosition();
        }
        else
        {
            EndAnomally();
        }
    }
    public void StartAnomally()
    {
        anomallyBalls[positionId].SetActive(true);
        anomallyObject.SetActive(true);
    }
    private void EndAnomally()
    {
        anomallyBalls[positionId].SetActive(false);
        positionId = 0;
        
    }
    private void GoToNextPosition()
    {
        anomallyBalls[positionId].SetActive(false);
        positionId++;
        anomallyBalls[positionId].SetActive(true);
    }
}
