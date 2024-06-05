using System;
using System.Collections;
using System.Collections.Generic;
using Auki.ConjureKit;
using Auki.ConjureKit.ECS;
using ARHandball.Models;
using UnityEngine;

public class HandSystem : SystemBase
{
    private const string handComponent = "HAND.COMPONENT";

    private uint _handComponentTypeId;

    public event Action<Entity, Pose> InvokeSpawnHand;
    public event Action<uint> InvokeDestroyHand;

    public HandSystem(Session session) : base(session)
    {

    }

    public override string[] GetComponentTypeNames()
    {
        return new[] { handComponent };
    }

    public override void Update(IReadOnlyList<(EntityComponent component, bool localChange)> updated)
    {
        foreach (var c in updated)
        {
            if (c.component.ComponentTypeId == _handComponentTypeId)
            {
                var entity = _session.GetEntity(c.component.EntityId);
                var sPose = c.component.Data.FromJsonByteArray<SPose>();
                var pose = sPose.ToUnityPose();
                InvokeSpawnHand?.Invoke(entity, pose);
                continue;
            }
        }
    }

    public override void Delete(IReadOnlyList<(EntityComponent component, bool localChange)> deleted)
    {
        foreach (var c in deleted)
        {
            if (c.component.ComponentTypeId == _handComponentTypeId)
            {
                var entityId = c.component.EntityId;
                InvokeDestroyHand?.Invoke(entityId);
            }
        }
    }

    public void GetComponentsTypeId()
    {
        _session.GetComponentTypeId(handComponent, u => _handComponentTypeId = u, error =>
        {
            Debug.LogError(error.TagString());
        });
    }

    public void AddHand(Entity entity)
    {
        var pose = _session.GetEntityPose(entity);
        var sPose = new SPose(pose);
        var poseJson = sPose.ToJsonByteArray();
        _session.AddComponent(_handComponentTypeId, entity.Id, poseJson, null,
            error => Debug.LogError(error.TagString()));
    }

    public void UpdateHand(uint entityId, Pose pose)
    {
        if (_session.GetEntity(entityId) == null) return;

        var sPose = new SPose(pose);
        var poseJson = sPose.ToJsonByteArray();
        _session.UpdateComponent(_handComponentTypeId, entityId, poseJson);
    }

    public void DeleteHand(uint entityId)
    {
        _session.DeleteComponent(_handComponentTypeId, entityId, null, 
            error => Debug.LogError(error.TagString()));
    }

}