using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AnomallyController : MonoBehaviour
{
    [SerializeField] private GameObject[] anomallyBalls;
    [SerializeField] private GameObject cube;
    [SerializeField] private GameObject sphere;
    [SerializeField] private GameObject endCube;

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
        if (positionId == 2) {
            cube.SetActive(false);
            endCube.SetActive(true);
        }
        anomallyBalls[positionId].SetActive(true);
    }
}
