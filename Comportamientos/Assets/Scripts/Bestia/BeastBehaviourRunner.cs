using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using BehaviourAPI.Core;
using BehaviourAPI.UnityToolkit;
using BehaviourAPI.UnityToolkit.GUIDesigner.Runtime;


public class BeastBehaviourRunner : EditorBehaviourRunner
{
    // Use this method to modify the editor graph in code
    protected override void ModifyGraphs(Dictionary<string, BehaviourGraph> graphMap, Dictionary<string, PushPerception> pushPerceptionMap)
    {
        // Use graphMap["graphname"] to get a behaviourGraph called "graphname".
        // Use pushPerceptionMap["pushname"] to get a Pushperception called "pushname".
        // Use graphMap["graphname"].FindNode("nodename") to get a node called "nodename" in a graph called "graphname".
    }

    // Use this method instead of Awake
    protected override void Init()
    {
        base.Init();
    }

    // Use this method instead of OnDisable
    protected override void OnDisableSystem()
    {
        base.OnDisableSystem();
    }

    // Use this method instead of OnEnable
    protected override void OnEnableSystem()
    {
        base.OnEnableSystem();
    }

    // Use this method instead of Start
    protected override void OnStarted()
    {
        base.OnStarted();
    }

    // Use this method instead of Update
    protected override void OnUpdated()
    {
        base.OnUpdated();
    }
}

