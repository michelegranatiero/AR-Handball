using System.Collections;
using System.Collections.Generic;
using System.Drawing;
using System.Net;
using Unity.Collections;
using Unity.XR.CoreUtils;
using UnityEditor;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.EnhancedTouch;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.ARSubsystems; // for occlusion culling

using UnityEngine.Events;

using Auki.ConjureKit;
using UnityEngine.UI;
using Auki.ConjureKit.Manna;
using Auki.ConjureKit.Vikja;
using Auki.ConjureKit.Grund;
using Auki.Integration.ARFoundation;
using Auki.Ur;
using Auki.Util;

using System;
using System.Threading.Tasks;
using System.Linq;
using UnityEngine.XR;

using ARHandball.Models;
using ARHandball.UI;

public class GameManager : MonoBehaviour
{
    [SerializeField] private UiManager uiManager;
    
    [SerializeField] private Camera arCamera;
    private ARPlaneManager aRPlaneManager;
    private ARRaycastManager aRRaycastManager;
    private bool raycastEnabled = false;
    private List<ARRaycastHit> hits = new List<ARRaycastHit>();

    //Walls (DISABLED FOR NOW)
    [SerializeField] private GameObject wallPrefab;
    /*private BuildWallsOnBoundary buildWallsOnBoundary;
    private Dictionary<ARPlane, List<GameObject>> planeCubesMap = new Dictionary<ARPlane, List<GameObject>>();*/

    TouchInput touchControls;
    bool isPressed;

    //Ball
    [SerializeField] private GameObject spherePrefab;
    private GameObject mySphere;
    private uint mySphereEntityId;
    private bool isMyBallSpawned = false;

    //Field
    [SerializeField] private GameObject fieldPrefab;
    private GameObject myField;
    private bool isMyFieldSpawned = false;
    private bool isFieldSelected = false;


    //CONJURE KIT
    private State _currentState;
    private Session _session;
    private uint _myId;


    private IConjureKit _conjureKit;
    private Vikja _vikja;
    private Grund _grund;
    private Manna _manna;
    private CameraFrameProvider _arCameraFrameProvider;
    private bool qrCodeBool;


    //UR module for hand tracking
    private HandTracker _handTracker;
    [SerializeField] ARSession arSession;

    [SerializeField] private GameObject fingertipPrefab;
    private GameObject fingertipLandmark;
    [SerializeField] Material landMarkMaterial;

    private bool landmarksVisualizeBool = true;
    private Dictionary<int, GameObject> _handLandmarks = new();
    private Dictionary<int, (Vector3, Vector3)> _handVelocities = new(); // prev,actual velocity



    //Occlusion culling
    [SerializeField] AROcclusionManager arOcclusionManager;
    private bool occlusionBool = false;

    //Debug UI
    private bool debugUIBool = true; //plane mesh
    private GameObject planeDefaultPrefab;
    [SerializeField] GameObject planeDebugPrefab;


    //ECS
    private GameEventController _gameEventController = new();

    private BallSystem _ballSystem;
    private Dictionary<uint, Rigidbody> _spheres = new Dictionary<uint, Rigidbody>();
    private Dictionary<uint, uint> _myHand = new Dictionary<uint, uint>();

    public UnityEvent<Rigidbody, Collider, String> onTriggerEnter_Ball;
    public UnityEvent<Rigidbody, Collider, String> onCollisionEnter_Ball;



    private void Awake()
    {

        aRRaycastManager = GetComponent<ARRaycastManager>();
        aRPlaneManager = GetComponent<ARPlaneManager>();
        planeDefaultPrefab = aRPlaneManager.planePrefab;

        touchControls = new TouchInput();

        //when event is triggered, set isPressed to corresponding value
        touchControls.TouchControl.touch.performed += _ => isPressed = true;
        touchControls.TouchControl.touch.canceled += _ => isPressed = false;

        //initialize buildWallsOnBoundary attributes
        /*buildWallsOnBoundary = BuildWallsOnBoundary.Instance;
        BuildWallsOnBoundary.Instance.setWallPrefab(wallPrefab);
        BuildWallsOnBoundary.Instance.setPlaneCubesMap(planeCubesMap);*/
        

    }

    private void Start()
    {
        Screen.sleepTimeout = SleepTimeout.NeverSleep;

        /*aRPlaneManager.planesChanged += buildWallsOnBoundary.OnPlanesChanged;*/

        //CONJURE KIT
        _conjureKit = new ConjureKit(
            arCamera.transform,
            "CONJUREKIT APP KEY",
            "CONJUREKIT APP SECRET");

        _manna = new Manna(_conjureKit);
        _vikja = new Vikja(_conjureKit);
        _grund = new Grund(_conjureKit, _vikja); //optional parameter: max margin, default is 0.1f

        _arCameraFrameProvider = CameraFrameProvider.GetOrCreateComponent();
        #if !UNITY_EDITOR
        _arCameraFrameProvider.OnNewFrameReady += frame => _manna.ProcessVideoFrameTexture(frame.Texture, frame.ARProjectionMatrix, frame.ARWorldToCameraMatrix);
        #endif

        EventInit();

        onTriggerEnter_Ball.AddListener(ballColliderListener);
        onCollisionEnter_Ball.AddListener(ballColliderListener);
        uiManager.Initialize(ToggleLightHouse, ToggleOcclusion, ToggleHandLandmarks, ToggleSelectPrefab, ToggleDebugUI);

        _gameEventController.Initialize(_conjureKit, _vikja);

        _conjureKit.Connect();


        //HAND TRACKER INIZIALIZATION
        #if !UNITY_EDITOR
        _handTracker = HandTracker.GetInstance();
        _handTracker.SetARSystem(arSession, arCamera, aRRaycastManager);

        _handTracker.SetMaterialOverride(landMarkMaterial);


        // ALL LANDMARKS
        _handTracker.OnUpdate += /*async*/ (landmarks, translations, isRightHand, score) =>
        {
            var h = 0;
            if (score[h] > 0 && landmarksVisualizeBool)
            {
                var handPosition = new Vector3( // "translations" contains all hands x,y,z
                    translations[h * 3 + 0],
                    translations[h * 3 + 1],
                    translations[h * 3 + 2]);

                var handLandmarkIndex = h * HandTracker.LandmarksCount * 3;
                for (int l = 0; l < HandTracker.LandmarksCount; ++l)
                {
                    var landMarkPosition = new Vector3(
                        landmarks[handLandmarkIndex + (l * 3) + 0],
                        landmarks[handLandmarkIndex + (l * 3) + 1],
                        landmarks[handLandmarkIndex + (l * 3) + 2]);

                    if (!_handLandmarks.ContainsKey(l))
                    {
                        _handLandmarks[l] = Instantiate(fingertipPrefab);
                        _handLandmarks[l].transform.SetParent(arCamera.transform);
                    }

                    _handLandmarks[l].SetActive(true);

                    // Update the landmarks position 

                    Vector3 globalPosition = handPosition + landMarkPosition;
                    _handLandmarks[l].GetComponent<Renderer>().transform.localPosition = globalPosition;

                    if (_handVelocities.ContainsKey(l))
                    {
                        //increment hand velocity
                        var tuple = (_handVelocities[l].Item1 + _handVelocities[l].Item2,
                                    (_handVelocities[l].Item2 + globalPosition - _handVelocities[l].Item1) / Time.deltaTime); 
                        _handVelocities[l] = tuple;
                    }
                    else
                    {
                        _handVelocities[l] = (Vector3.zero, (globalPosition - Vector3.zero) / Time.deltaTime);
                    }
                }
            }
            else if (_handLandmarks.Count > 0)
            {
                foreach (var landmark in _handLandmarks)
                {
                    landmark.Value.SetActive(false);
                    _handVelocities[landmark.Key] = (Vector3.zero, Vector3.zero);
                }
            }
        };



        _handTracker.Start();
        //_handTracker.ShowHandMesh();
        #endif
    }


    //----------------------------------
    private void EventInit()
    {
        _conjureKit.OnJoined += OnJoined;
        _conjureKit.OnLeft += OnLeft;
        _conjureKit.OnStateChanged += OnStateChange;

        _gameEventController.OnFieldMove += pose =>
        {
            if (myField != null)
            {
                myField.transform.position = pose.position;
                myField.transform.rotation = pose.rotation;
            }else{
                myField = Instantiate(fieldPrefab, pose.position, pose.rotation);
            }
        };
    }

    #region ConjureKit Callbacks

    private void OnJoined(Session session)
    {//fired when a user is joined and calibrated. Returns session data (id, ...)
        _myId = session.ParticipantId;
        _session = session;

        _ballSystem = new BallSystem(_session);
        _session.RegisterSystem(_ballSystem, () =>
        {
            _ballSystem.GetComponentsTypeId();
        });

        uiManager.SetSessionId(session.Id, session.ParticipantId.ToString());

        SetBallListener(_ballSystem);

    }

    private void OnLeft(Session lastSession)
    {// fired when user has left the session
        RemoveBallListener();
        _ballSystem = null;

        uiManager.SetSessionId("", "");
    }

    private void OnStateChange(State state)
    {
        _currentState = state;
        var sessionReady = _currentState is State.Calibrated or State.JoinedSession;
        _arCameraFrameProvider.enabled = sessionReady;
        _grund.SetActive(sessionReady);

        uiManager.UpdateState(_currentState.ToString());
        raycastEnabled = _currentState == State.Calibrated;

        uiManager.SetInteractables(sessionReady);

        //auto reconnection (not working / slow connecting)
        /*if (state == State.Disconnected)
        {
            _conjureKit.Connect();
        }*/

    }
    #endregion


    private bool isDragging = false;
    private void FixedUpdate()
    {
        #if !UNITY_EDITOR
            _handTracker.Update();
        #endif

        if (isFieldSelected) UpdateRayCastField();
        else UpdateRayCastBall();

    }

    private void UpdateRayCastBall()
    {

        if (Pointer.current == null || isPressed == false)
        {
            if (mySphere != null && isDragging)
            {
                isDragging = false;
                mySphere.GetComponent<Rigidbody>().isKinematic = false;
            }

            if (mySphere != null)
            {
                var pose = mySphere.transform.GetLocalPose();

                _ballSystem.UpdateBall(mySphereEntityId, pose);
            }
            return;
        }

        var pointPos = Pointer.current.position.ReadValue();

        bool isOverUI = pointPos.IsPointOverUIObject();
        if (isOverUI && !isDragging) return;

        if (aRRaycastManager.Raycast(pointPos, hits, TrackableType.PlaneWithinPolygon) && raycastEnabled)
        {
            var hit = hits[0];

            //checks if the plane is facing up
            if (aRPlaneManager.GetPlane(hit.trackableId).alignment != PlaneAlignment.HorizontalUp) return;

            // if the sphere is not spawned, spawn it
            if (mySphere == null && !isMyBallSpawned)
            {
                Vector3 posRotation = arCamera.transform.forward;
                posRotation.y = 0;

                Vector3 posPosition = hit.pose.position;
                posPosition.y = posPosition.y + 0.3f;


                //CONJURE KIT
                Pose pose = new Pose(posPosition, Quaternion.LookRotation(posRotation));


                _session.AddEntity(pose, entity =>
                {
                    _ballSystem.AddBall(entity);
                    _ballSystem.AddForceBall(entity.Id, Vector3.zero, Vector3.zero);
                    mySphereEntityId = entity.Id;

                }, Debug.LogError);

                isMyBallSpawned = true;

            }
            // if the sphere is spawned, move it to the touched position
            else if (mySphere != null)
            {
                Vector3 posPosition = hit.pose.position;
                //posPosition.y = posPosition.y + spherePrefab.transform.localScale.y / 2f;
                posPosition.y = posPosition.y + 0.3f;


                Vector3 posRotation = arCamera.transform.forward;
                posRotation.y = 0;

                if (!isDragging)
                {
                    isDragging = true;
                    mySphere.GetComponent<Rigidbody>().isKinematic = true;
                }

                mySphere.GetComponent<Rigidbody>().MovePosition(posPosition);
                var pose = mySphere.transform.GetLocalPose();

                _ballSystem.UpdateBall(mySphereEntityId, pose);
            }
        }
    }



    private void UpdateRayCastField()
    {
        if (Pointer.current == null || isPressed == false)
        {
            if (myField != null && isDragging)
            {
                isDragging = false;
            }
            return;
        }
        

        var pointPos = Pointer.current.position.ReadValue();

        bool isOverUI = pointPos.IsPointOverUIObject();
        if (isOverUI && !isDragging) return;

        if (aRRaycastManager.Raycast(pointPos, hits, TrackableType.PlaneWithinPolygon) && raycastEnabled)
        {
            var hit = hits[0];

            //checks if the plane is facing up
            if (aRPlaneManager.GetPlane(hit.trackableId).alignment != PlaneAlignment.HorizontalUp) return;

            // if the field is not spawned, spawn it
            if (myField == null && !isMyFieldSpawned)
            {
                Vector3 posRotation = arCamera.transform.forward;
                posRotation.y = 0;

                Vector3 posPosition = hit.pose.position;
                //posPosition.y = posPosition.y + 0.3f;

                //spawn the field
                myField = Instantiate(fieldPrefab, posPosition, Quaternion.LookRotation(posRotation));

                isMyFieldSpawned = true;

                _gameEventController.SendFieldPos(myField.transform.GetLocalPose());

            }
            // if the field is spawned, move it to the touched position
            else if (myField != null)
            {
                Vector3 posPosition = hit.pose.position;
                //posPosition.y = posPosition.y + spherePrefab.transform.localScale.y / 2f;
                //posPosition.y = posPosition.y + 0.3f;

                Vector3 posRotation = arCamera.transform.forward;
                posRotation.y = 0;

                myField.transform.position = posPosition;
                myField.transform.rotation = Quaternion.LookRotation(posRotation);

                _gameEventController.SendFieldPos(myField.transform.GetLocalPose());
            }
        }
    }

    public void ballColliderListener(Rigidbody hittedRB, Collider other, String c_type) //hittedRB is the ball
    {
        if (other.gameObject.CompareTag("hand"))
        {
            if (c_type == "trigger" && !_spheres.ContainsValue(hittedRB))
            {
                return;
            }

            var ballEntityId = _spheres.FirstOrDefault(x => x.Value == hittedRB).Key;
            var handLanmarkId = _handLandmarks.FirstOrDefault(x => x.Value == other.gameObject).Key;
            var handLandVelocity = _handVelocities[handLanmarkId].Item2;

            Vector3 forceDirection = hittedRB.transform.position - other.transform.position;
            forceDirection.Normalize();


            Rigidbody otherRB = other.attachedRigidbody;

            Vector3 impulseForce = (otherRB.transform.position - hittedRB.transform.position).normalized * (otherRB.mass * handLandVelocity.magnitude) / hittedRB.mass;

            Vector3 distanceFromCollisionPoint = hittedRB.transform.position - other.ClosestPoint(hittedRB.transform.position);

            // Get the radius from the SphereCollider
            SphereCollider handCollider = other.gameObject.GetComponent<SphereCollider>();
            float radius = handCollider != null ? handCollider.radius : 0.5f;

            // Calculate distance factor with a minimum bound
            float distanceFactor = Mathf.Clamp(1f - distanceFromCollisionPoint.magnitude / radius, 0.2f, 1f);
            Vector3 attenuatedImpulseForce = Vector3.Max(impulseForce * Mathf.Clamp01(distanceFactor), 0.05f * Vector3.one);

            // Scale force between 0.05f and 0.5fp
            float forceScale = Mathf.InverseLerp(0.2f, 1f, distanceFactor);
            attenuatedImpulseForce = Vector3.Lerp(0.02f * forceDirection, 0.2f * forceDirection, forceScale);


            if (c_type == "trigger") //others' spheres (to broadcast)
                _ballSystem.UpdateForceBall(ballEntityId, attenuatedImpulseForce, other.ClosestPoint(hittedRB.transform.position));
            else if (c_type == "collision"){ //mysphere
                mySphere.GetComponent<Rigidbody>().AddForceAtPosition(attenuatedImpulseForce*2f, other.ClosestPoint(hittedRB.transform.position), ForceMode.Impulse);
            }
        }
    }

    public void AddForceListener(uint entityId, Vector3 force, Vector3 position)
    {
        if (entityId != mySphereEntityId) return;
        mySphere.GetComponent<Rigidbody>().AddForceAtPosition(force, position, ForceMode.Impulse);
    }


    public void SpawnOrUpdateBall(Entity entity, Pose entityPose)
    {
        if (entity.ParticipantId == _myId) //if owner of the entity
        {
            if (mySphere != null) return;
            else mySphere = Instantiate(spherePrefab, entityPose.position, entityPose.rotation);
            mySphere.tag = "ball1";
            mySphere.GetComponent<ColliderEvent>().setGameManager(this);
        }
        else //other participants
        {
            if (_spheres.ContainsKey(entity.Id)) //if entity already in the list:
            {
                _spheres[entity.Id].MovePosition(entityPose.position);
                _spheres[entity.Id].MoveRotation(entityPose.rotation);
            }
            else //if entity not in the list: add it
            {
                _spheres[entity.Id] = Instantiate(spherePrefab, entityPose.position, entityPose.rotation).GetComponent<Rigidbody>();
                _spheres[entity.Id].gameObject.tag = "ball2";
                _spheres[entity.Id].isKinematic = true;
                _spheres[entity.Id].GetComponent<Collider>().isTrigger = true;
                _spheres[entity.Id].GetComponent<ColliderEvent>().setGameManager(this);
            }
        }
    }

    public void SetBallListener(BallSystem ballSystem)
    {
        _ballSystem = ballSystem;
        _ballSystem.InvokeSpawnBall += SpawnOrUpdateBall;
        _ballSystem.InvokeDestroyBall += DestroyBallListener;
        _ballSystem.InvokeAddForce += AddForceListener;
    }

    public void RemoveBallListener()
    {
        _ballSystem.InvokeSpawnBall -= SpawnOrUpdateBall;
        _ballSystem.InvokeDestroyBall -= DestroyBallListener;
        _ballSystem.InvokeAddForce -= AddForceListener;
    }

    private void DestroyBallListener(uint entityId)
    {

        if (_spheres.ContainsKey(entityId))
        {
            Destroy(_spheres[entityId].gameObject);
            _spheres.Remove(entityId);
        }
    }

    private void OnEnable()
    {
        touchControls.TouchControl.Enable();
    }

    private void OnDisable()
    {
        touchControls.TouchControl.Disable();
    }


    //--------------------Toggles--------------------------
    public void ToggleLightHouse()
    {
        qrCodeBool = !qrCodeBool;
        _manna.SetLighthouseVisible(qrCodeBool);
    }

    public void ToggleSelectPrefab()
    {
        isFieldSelected = !isFieldSelected;
    }

    public void ToggleHandLandmarks()
    {
        landmarksVisualizeBool = !landmarksVisualizeBool;

        if (landmarksVisualizeBool)
        {
            _handTracker.ShowHandMesh();
        }
        else
        {
            _handTracker.HideHandMesh();
        }
    }

    public void ToggleOcclusion()
    {
        occlusionBool = !occlusionBool;

        //arOcclusionManager.requestedHumanDepthMode = occlusionBool ? HumanSegmentationDepthMode.Fastest : HumanSegmentationDepthMode.Disabled;
        //arOcclusionManager.requestedHumanStencilMode = occlusionBool ? HumanSegmentationStencilMode.Fastest : HumanSegmentationStencilMode.Disabled;
        arOcclusionManager.requestedEnvironmentDepthMode = occlusionBool ? EnvironmentDepthMode.Fastest : EnvironmentDepthMode.Disabled;
    }

    public void ToggleDebugUI() //plane mesh
    {
        debugUIBool = !debugUIBool;

        SetPlaneVisible(debugUIBool);
    }
    public void SetPlaneVisible(bool visible)
    {
        foreach (var plane in aRPlaneManager.trackables)
        {

            plane.GetComponent<ARPlaneMeshVisualizer>().enabled = visible;
            //meshvisualizer script
            if (plane.GetComponent<ARFeatheredPlaneMeshVisualizer>() != null){
                plane.GetComponent<ARFeatheredPlaneMeshVisualizer>().enabled = visible;
            }
        }

        aRPlaneManager.planePrefab.GetComponent<ARPlaneMeshVisualizer>().enabled = visible;
        //meshvisualizer script
        if(aRPlaneManager.planePrefab.GetComponent<ARFeatheredPlaneMeshVisualizer>() != null){
            aRPlaneManager.planePrefab.GetComponent<ARFeatheredPlaneMeshVisualizer>().enabled = visible;
        }

    }

}
