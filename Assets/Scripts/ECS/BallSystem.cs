using System;
using System.Collections;
using System.Collections.Generic;
using Auki.ConjureKit;
using Auki.ConjureKit.ECS;
using ARHandball.Models;
using UnityEngine;

public class BallSystem : SystemBase
{

    private byte[] _emptyData = new byte[1];

    private const string ballComponent = "BALL.COMPONENT";
    private const string forceComponent = "FORCE.COMPONENT";

    private uint _ballComponentTypeId;
    private uint _forceComponentTypeId;

    public event Action<Entity, Pose> InvokeSpawnBall;
    public event Action<uint> InvokeDestroyBall;

    public event Action<uint, Vector3, Vector3> InvokeAddForce;

    public BallSystem(Session session) : base(session)
    {

    }

    public override string[] GetComponentTypeNames()
    {
        return new[] { ballComponent, forceComponent };
    }

    public override void Update(IReadOnlyList<(EntityComponent component, bool localChange)> updated)
    {
        foreach (var c in updated)
        {

            if (c.component.ComponentTypeId == _ballComponentTypeId)
            {
                var entity = _session.GetEntity(c.component.EntityId);
                var sPose = c.component.Data.FromJsonByteArray<SPose>();
                var pose = sPose.ToUnityPose();
                InvokeSpawnBall?.Invoke(entity, pose);
            }

            if (c.component.ComponentTypeId == _forceComponentTypeId)
            {
                if (c.component.Data.Length == 1) return; //empty array

                var entity = _session.GetEntity(c.component.EntityId);
                var sVectors = c.component.Data.FromJsonByteArray<SVector3[]>();
                var force = sVectors[0].ToVector3();
                var position = sVectors[1].ToVector3();

                if (force == Vector3.zero && position == Vector3.zero) return;

                InvokeAddForce?.Invoke(entity.Id, force, position);
            }

        }
    }

    public override void Delete(IReadOnlyList<(EntityComponent component, bool localChange)> deleted)
    {
        foreach (var c in deleted)
        {
            if (c.component.ComponentTypeId == _ballComponentTypeId)
            {
                var entityId = c.component.EntityId;
                InvokeDestroyBall?.Invoke(entityId);
            }
        }

    }

    public void GetComponentsTypeId()
    {
        _session.GetComponentTypeId(ballComponent, u => _ballComponentTypeId = u, error =>
        {
            Debug.LogError(error.TagString());
        });
        _session.GetComponentTypeId(forceComponent, u => _forceComponentTypeId = u, error =>
        {
            Debug.LogError(error.TagString());
        });

    }

    public void AddBall(Entity entity)
    {
        var pose = _session.GetEntityPose(entity);
        var sPose = new SPose(pose);
        var poseJson = sPose.ToJsonByteArray();
        _session.AddComponent(_ballComponentTypeId, entity.Id, poseJson, null,
            error => Debug.LogError(error.TagString()));
    }

    public void UpdateBall(uint entityId, Pose pose)
    {
        var sPose = new SPose(pose);
        var poseJson = sPose.ToJsonByteArray();
        _session.UpdateComponent(_ballComponentTypeId, entityId, poseJson);
    }

    public void DeleteBall(uint entityId)
    {
        _session.DeleteComponent(_ballComponentTypeId, entityId, null, 
            error => Debug.LogError(error.TagString()));
    }



    public void AddForceBall(uint entityId, Vector3 force, Vector3 position)
    {
        _session.AddComponent(_forceComponentTypeId, entityId, _emptyData, null);
    }

    public void UpdateForceBall(uint entityId, Vector3 force, Vector3 position)
    {

        var entity = _session.GetEntity(entityId);
        if (entity == null)
        {
            Debug.LogError("Entity not found");
            return;
        }

        var sForce = new SVector3(force);
        var sPosition = new SVector3(position);
        SVector3[] sVectors = { sForce, sPosition};
        var forceJson = sVectors.ToJsonByteArray();

        _session.UpdateComponent(_forceComponentTypeId, entityId, forceJson);
    }


}