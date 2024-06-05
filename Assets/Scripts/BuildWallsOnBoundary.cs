using DilmerGames.Core.Singletons;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.ARSubsystems;

public class BuildWallsOnBoundary : Singleton<BuildWallsOnBoundary>
{

    private GameObject wallPrefab;
    private Dictionary<ARPlane, List<GameObject>> planeCubesMap;

    public void setWallPrefab(GameObject wallPrefab)
    {
        this.wallPrefab = wallPrefab;
    }

    public void setPlaneCubesMap(Dictionary<ARPlane, List<GameObject>> planeCubesMap)
    {
        this.planeCubesMap = planeCubesMap;
    }
    public void OnPlanesChanged(ARPlanesChangedEventArgs changes)
    {
        foreach (ARPlane plane in changes.added)
        {
            if (plane.alignment != PlaneAlignment.HorizontalUp) continue;
            SpawnCubeOnPlaneEdge(plane);
            //Debug.Log($"Plane added {plane.trackableId}");
        }

        foreach (ARPlane plane in changes.updated)
        {
            if (plane.alignment != PlaneAlignment.HorizontalUp) continue;
            UpdatePlaneCubes(plane);
            //Debug.Log($"Plane updated {plane.trackableId}");
        }

        foreach (ARPlane plane in changes.removed)
        {
            if (plane.alignment != PlaneAlignment.HorizontalUp) continue;
            RemovePlaneCubes(plane);
            //Debug.Log($"Plane removed {plane.trackableId}");
        }
    }


    void SpawnCubeOnPlaneEdge(ARPlane plane)
    {
        float wall_y_offset = wallPrefab.transform.localScale.y / 2;

        List<Vector3> boundaryPoints = GetPlaneBoundaryPoints(plane, wall_y_offset);
        List<GameObject> planeCubes = new List<GameObject>();

        // Spawn cubes only on the boundary points
        /*Vector3 planeCenter = plane.center; // Plane center in world space
        Vector3 centVec = new Vector3(planeCenter.x, planeCenter.y + wall_y_offset, planeCenter.z);

        foreach (Vector3 point in boundaryPoints)
        {
            GameObject cube = Instantiate(wallPrefab, point, Quaternion.identity);
            planeCubes.Add(cube);
        }
        GameObject centCube = Instantiate(wallPrefab, centVec, Quaternion.identity);
        planeCubes.Add(centCube);*/

        // Spawn border walls
        for (int i = 0; i < boundaryPoints.Count; i++)
        {
            Vector3 startPoint = boundaryPoints[i];
            Vector3 endPoint = boundaryPoints[(i + 1) % boundaryPoints.Count];
            Vector3 direction = endPoint - startPoint;
            Vector3 position = (startPoint + endPoint) / 2; // Midpoint of the wall
            //Vector3 position = startPoint + (direction / 2.0f); // Alternative

            Quaternion rotation = Quaternion.LookRotation(direction);
            GameObject stretchCube = Instantiate(wallPrefab, position, rotation);

            stretchCube.transform.LookAt(startPoint);
            Vector3 localScale = stretchCube.transform.localScale;
            localScale.z = direction.magnitude;
            stretchCube.transform.localScale = localScale;

            planeCubes.Add(stretchCube);
        }

        planeCubesMap.Add(plane, planeCubes);
    }

    void UpdatePlaneCubes(ARPlane plane)
    {
        // Remove old cubes
        RemovePlaneCubes(plane);

        // Update and instantiate new cubes on the updated plane boundary
        SpawnCubeOnPlaneEdge(plane);
    }

    void RemovePlaneCubes(ARPlane plane)
    {

        if (!planeCubesMap.ContainsKey(plane)) return;

        foreach (GameObject cube in planeCubesMap[plane])
        {
            Destroy(cube);
        }

        planeCubesMap.Remove(plane);
    }


    List<Vector3> GetPlaneBoundaryPoints(ARPlane plane, float wall_y_offset)
    {
        List<Vector3> boundaryPoints = new List<Vector3>();
        var boundary = plane.boundary;

        Matrix4x4 localToWorldMatrix = plane.transform.localToWorldMatrix;

        foreach (var point in boundary)
        {
            Vector3 localPoint = new Vector3(point.x, wall_y_offset, point.y);
            Vector3 worldPoint = localToWorldMatrix.MultiplyPoint(localPoint);

            boundaryPoints.Add(worldPoint);
        }

        return boundaryPoints;
    }

    public Dictionary<ARPlane, List<GameObject>> GetARPlanesCubesMaps()
    {
        return planeCubesMap;
    }

}
