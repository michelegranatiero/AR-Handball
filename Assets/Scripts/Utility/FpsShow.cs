using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class FpsShow : MonoBehaviour
{

    [SerializeField] private Text fpsText;
    private float deltaTime;

    // Start is called before the first frame update
    void Start()
    {
    }

    // Update is called once per frame
    void Update()
    {
        deltaTime += (Time.unscaledDeltaTime - deltaTime) * 0.1f;
        float fpsValue = 1.0f / deltaTime;
        fpsText.text = string.Format("{0:0.} fps", fpsValue);
    }
}
