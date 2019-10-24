using System;

namespace DedicatedHostsManager
{
    public interface ICacheProvider
    {
        bool AddData(string key, string data, TimeSpan? expiry = null);
        bool KeyExists(string key);
        void DeleteKey(string key);
    }
}