using System;
using System.Collections.Generic;
using UnityEngine;

namespace EventList.Adapters;

/// <summary>Gives the page watch a step every frame. A failure is logged the first time it is seen and the next frame carries on.</summary>
public sealed class ListTicker : MonoBehaviour
{
    private const int MostFailuresLogged = 20;

    internal static ListPage? Page;
    internal static Action<string> Log = _ => { };

    private static readonly HashSet<string> ReportedFailures = new();

    public ListTicker(IntPtr pointer) : base(pointer)
    {
    }

    private void Update()
    {
        if (Page == null) return;

        try
        {
            Page.Tick(Time.unscaledTime);
        }
        catch (Exception ex)
        {
            if (ReportedFailures.Count < MostFailuresLogged && ReportedFailures.Add(ex.GetType().Name + ": " + ex.Message)) Log("events list step failed (each failure logged once): " + ex);
        }
    }
}
