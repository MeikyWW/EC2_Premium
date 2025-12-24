using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(HeroStatus))]
public class HeroStatusEffect : MonoBehaviour
{
    public Dictionary<string, StatusEffectNotification> allStatus;

    private HeroStatus status;

    private void Awake()
    {
        allStatus = new Dictionary<string, StatusEffectNotification>();
        status = GetComponent<HeroStatus>();
    }

    public void SetNotification(string statusId, string text)
    {
        SetNotification(statusId, text, 0);
    }
    public void SetNotification(string statusId, string text, int stack)
    {
        if (allStatus.ContainsKey(statusId))
            allStatus[statusId].SetNotification(statusId, text, stack);
        else
        {
            if (status.statusBar)
            {
                var notif = status.statusBar.CreateNotif(statusId, text, stack);
                allStatus.Add(statusId, notif);
            }
        }
    }

    public void RemoveNotification(string statusId)
    {
        if (allStatus.ContainsKey(statusId))
        {
            //remove notifnya dulu, baru remove data
            if(allStatus[statusId])
                if (allStatus[statusId].gameObject) Destroy(allStatus[statusId].gameObject);
            allStatus.Remove(statusId);

            if (status.statusBar)
                status.statusBar.Reposition();
        }
    }

}
