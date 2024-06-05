using System;
using System.Collections;
using System.Collections.Generic;
using AukiHandTrackerSample;
using UnityEngine;
using UnityEngine.UI;

using UINullableToggle = UISwitcher.UINullableToggle;

namespace ARHandball.UI
{
    public class UiManager : MonoBehaviour
    {
        [SerializeField] private Text sessionText;
        [SerializeField] private Text stateText;
        [SerializeField] Button qrCodeButton;
        [SerializeField] UINullableToggle occlusionToggle;
        [SerializeField] UINullableToggle handLandmarksToggle;
        [SerializeField] UINullableToggle isFieldToggle;
        [SerializeField] UINullableToggle debugUIToggle; // plane mesh

        


        public void Initialize(Action toggleLighthouse, Action toggleOcclusion, Action toggleHandLandmarks, Action toggleSelectPrefab, Action toggleDebugUI)
        {
            qrCodeButton.onClick.AddListener(() => {toggleLighthouse?.Invoke();});
            occlusionToggle.onValueChanged.AddListener((bool value) => toggleOcclusion?.Invoke());
            handLandmarksToggle.onValueChanged.AddListener((bool value) => toggleHandLandmarks?.Invoke());
            isFieldToggle.onValueChanged.AddListener((bool value) => {toggleSelectPrefab?.Invoke();});
            debugUIToggle.onValueChanged.AddListener((bool value) => toggleDebugUI?.Invoke());
        }

        public void SetSessionId(string id, string participantId)
        {
            sessionText.text = id != "" ? id + " - " + participantId : "";
        }

        public void UpdateState(string state)
        {
            stateText.text = state;
        }

        public void SetInteractables(bool interactable)
        {
            if (qrCodeButton) qrCodeButton.interactable = interactable;
            if (handLandmarksToggle) handLandmarksToggle.interactable = interactable;
            if (occlusionToggle) occlusionToggle.interactable = interactable;
            if (isFieldToggle) isFieldToggle.interactable = interactable;
            if (debugUIToggle) debugUIToggle.interactable = interactable;
        }

       
        

        









    }
}

