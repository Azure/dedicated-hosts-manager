using System;

namespace DedicatedHostsManager.DedicatedHostStateManager
{
    /// <summary>
    /// Dedicated Host state manager.
    /// </summary>
    public interface IDedicatedHostStateManager
    {
        /// <summary>
        /// Inserts a key in the cache when a Dedicated Host is at full capacity.
        /// </summary>
        /// <param name="key">Key (host ID).</param>
        /// <param name="data">Data.</param>
        /// <param name="expiry">TTL.</param>
        bool MarkHostAtCapacity(string key, string data, TimeSpan? expiry = null);

        /// <summary>
        /// Checks to see if a Dedicated Host is at full capacity.
        /// </summary>
        /// <param name="key">Key (host ID).</param>
        bool IsHostAtCapacity(string key);

        /// <summary>
        /// Inserts a key in the cache when a Dedicated Host is ready for deletion.
        /// </summary>
        /// <param name="key">Key (host ID).</param>
        /// <param name="data">Data.</param>
        /// <param name="expiry">TTL.</param>
        bool MarkHostForDeletion(string key, string data, TimeSpan? expiry = null);

        /// <summary>
        /// Checks to see if a Dedicated Host is pending deletion.
        /// </summary>
        /// <param name="key"></param>
        bool IsHostMarkedForDeletion(string key);

        /// <summary>
        /// Inserts a key in the cache when a VM is being provisioned on a Dedicated Host.
        /// </summary>
        /// <param name="key">Key (host ID).</param>
        /// <param name="data">Data.</param>
        /// <param name="expiry">TTL.</param>
        bool MarkHostUsage(string key, string data, TimeSpan? expiry = null);

        /// <summary>
        /// Checks to see if a VM is being provisioned on a Dedicated Host.
        /// </summary>
        /// <param name="key"></param>
        /// <returns></returns>
        bool IsHostInUsage(string key);

        /// <summary>
        /// Removes the Dedicated Host from the pending deletion list.
        /// </summary>
        /// <param name="key">Key (host ID).</param>
        void UnmarkHostForDeletion(string key);
    }
}