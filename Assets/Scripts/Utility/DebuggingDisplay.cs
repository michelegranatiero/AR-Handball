using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class ZzzLog : MonoBehaviour
{
    uint qsize = 15;  // number of messages to keep
    Queue<string> myErrorQueue = new Queue<string>();
    Vector2 scrollPosition = Vector2.zero;

    void Start()
    {
        Debug.Log("Started up logging.");
    }

    void OnEnable()
    {
        Application.logMessageReceived += HandleLog;
    }

    void OnDisable()
    {
        Application.logMessageReceived -= HandleLog;
    }

    void HandleLog(string logString, string stackTrace, LogType type)
    {
        if (type == LogType.Error || type == LogType.Exception)
        {
            myErrorQueue.Enqueue("[" + type + "] : " + logString);
            if (type == LogType.Exception)
                myErrorQueue.Enqueue(stackTrace);
            while (myErrorQueue.Count > qsize)
                myErrorQueue.Dequeue();
        }
    }

    void OnGUI()
    {
        GUILayout.BeginArea(new Rect(Screen.width - 400, 0, 400, Screen.height));

        // Update the scroll position to the maximum scroll position
        scrollPosition.y = Mathf.Infinity;

        scrollPosition = GUILayout.BeginScrollView(scrollPosition);

        // Display error logs
        GUILayout.Label("\n" + string.Join("\n", myErrorQueue.ToArray()));

        GUILayout.EndScrollView();
        GUILayout.EndArea();
    }
}
