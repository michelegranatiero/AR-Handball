using System;
using Auki.ConjureKit;
using Auki.ConjureKit.Vikja;
using Auki.Util;
using ARHandball.Models;
using UnityEngine;


public class GameEventController
{
    private IConjureKit _conjureKit;
    private Vikja _vikja;

    private uint _myEntityId;

    private const string NotifyFieldPos = "NOTIFY.FIELD.POS";
    public Action<Pose> OnFieldMove;

    #region Public Methods
    public void Initialize(IConjureKit conjureKit, Vikja vikja)
    {
        _conjureKit = conjureKit;
        _vikja = vikja;
        _vikja.OnEntityAction += OnEntityAction;
        _conjureKit.OnParticipantEntityCreated += SetMyEntityId;
    }

    public void SendFieldPos(Pose pose)
    {
        _vikja.RequestAction(_myEntityId, NotifyFieldPos, new SPose(pose).ToJsonByteArray(), action =>
        {
            OnFieldMove?.Invoke(action.Data.FromJsonByteArray<SPose>().ToUnityPose());
        }, null);
    }
    #endregion

    #region Private Methods
    private void SetMyEntityId(Entity entity)
    {
        _myEntityId = entity.Id;
    }
    private void OnEntityAction(EntityAction obj)
    {
        switch (obj.Name)
        {
            case NotifyFieldPos:
                OnFieldMove?.Invoke(obj.Data.FromJsonByteArray<SPose>().ToUnityPose());
                break;
        }
    }
    #endregion
}