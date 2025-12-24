using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class HeroFallChecker : MonoBehaviour
{
    public HeroControl hero;
    public int triggerCount;

    private void OnTriggerEnter(Collider other)
    {
        triggerCount++;

        //Debug.Log("trigger enter : " + triggerCount + " > " + other.name);
        if (triggerCount > 0) hero.SetFall(false);
    }
    private void OnTriggerExit(Collider other)
    {
        triggerCount--; if (triggerCount < 0) triggerCount = 0;

        //Debug.Log("trigger exit : " + triggerCount + " > " + other.name);
        if (triggerCount <= 0) hero.SetFall(true);
    }

    public void ResetTrigger()
    {
        triggerCount = 0;
        //Debug.Log("reset trigger");
    }
}
