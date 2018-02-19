﻿using LunaClient.Base;
using LunaClient.Base.Interface;
using LunaClient.Systems.SettingsSys;
using LunaCommon.Enums;
using LunaCommon.Locks;
using LunaCommon.Message.Data.Lock;
using LunaCommon.Message.Interface;
using LunaCommon.Message.Types;
using System.Collections.Concurrent;
using System.Collections.Generic;
using UniLinq;

namespace LunaClient.Systems.Lock
{
    public class LockMessageHandler : SubSystem<LockSystem>, IMessageHandler
    {
        public ConcurrentQueue<IServerMessageBase> IncomingMessages { get; set; } = new ConcurrentQueue<IServerMessageBase>();

        private static readonly List<LockDefinition> LocksToRemove = new List<LockDefinition>();

        public void HandleMessage(IServerMessageBase msg)
        {
            if (!(msg.Data is LockBaseMsgData msgData)) return;

            switch (msgData.LockMessageType)
            {
                case LockMessageType.ListReply:
                    {
                        var data = (LockListReplyMsgData)msgData;

                        for (var i = 0; i < data.LocksCount; i++)
                        {
                            LockSystem.LockStore.AddOrUpdateLock(data.Locks[i]);
                        }

                        RemoveCorruptLocks(data);

                        if (MainSystem.NetworkState < ClientState.LocksSynced)
                            MainSystem.NetworkState = ClientState.LocksSynced;
                    }
                    break;
                case LockMessageType.Acquire:
                    {
                        var data = (LockAcquireMsgData)msgData;
                        LockSystem.LockStore.AddOrUpdateLock(data.Lock);

                        System.FireAcquireEvent(data.Lock);
                    }
                    break;
                case LockMessageType.Release:
                    {
                        var data = (LockReleaseMsgData)msgData;
                        LockSystem.LockStore.RemoveLock(data.Lock);

                        System.FireReleaseEvent(data.Lock);
                    }
                    break;
            }
        }

        //Here we run trough OUR lock list and if we find a lock that is not ours and it does not exist on the server, we remove it.
        //We don't remove OUR locks as perhaps we just acquired one and we are waiting for the server to receive it.
        private static void RemoveCorruptLocks(LockListReplyMsgData data)
        {
            LocksToRemove.Clear();
            foreach (var existingLock in LockSystem.LockQuery.GetAllLocks())
            {
                if (existingLock.PlayerName != SettingsSystem.CurrentSettings.PlayerName && !data.Locks.Contains(existingLock))
                    LocksToRemove.Add(existingLock);
            }
            foreach (var lockToRemove in LocksToRemove)
            {
                LockSystem.LockStore.RemoveLock(lockToRemove);
            }
        }
    }
}