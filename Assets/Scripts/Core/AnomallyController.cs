using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AnomallyController : MonoBehaviour
{
    [SerializeField] private GameObject[] anomallyBalls;
    [SerializeField] private GameObject cube;
    [SerializeField] private GameObject sphere;

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
        cube.SetActive(true);
    }
    private void EndAnomally()
    {
        anomallyBalls[positionId].SetActive(false);
        positionId = 0;
        sphere.SetActive(true);
    }
    private void GoToNextPosition()
    {
        anomallyBalls[positionId].SetActive(false);
        positionId++;
        anomallyBalls[positionId].SetActive(true);
    }
}
