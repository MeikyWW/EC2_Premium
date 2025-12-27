using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class HeroCollision : MonoBehaviour
{
    private void OnTriggerEnter(Collider other)
    {
        // if (other.CompareTag("portal"))
        // {
        //     other.GetComponent<NodePortal>().Proceed(transform);
        // }
        /*
        else if (other.CompareTag("dropItem"))
        {
            other.GetComponent<DropItem>().GetItem();
        }*/
    }

    

    /*
    private void OnCollisionEnter(Collision collision)
    {
        Debug.Log("On Collision Enter");
    }
    private void OnCollisionExit(Collision collision)
    {
        Debug.Log("On Collision Exit");
    }
    private void OnTriggerExit(Collider other)
    {
        Debug.Log("On Trigger Exit");
    }
    */
}
