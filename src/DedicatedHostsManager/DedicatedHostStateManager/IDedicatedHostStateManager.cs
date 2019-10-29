using System;

namespace DedicatedHostsManager.DedicatedHostStateManager
{
    public interface IDedicatedHostStateManager
    {
        bool MarkHostAtCapacity(string key, string data, TimeSpan? expiry = null);
        bool IsHostAtCapacity(string key);
        bool MarkHostForDeletion(string key, string data, TimeSpan? expiry = null);
        bool IsHostMarkedForDeletion(string key);
        bool MarkHostUsage(string key, string data, TimeSpan? expiry = null);
        bool IsHostInUsage(string key);
        void UnmarkHostUsage(string key);
    }
}