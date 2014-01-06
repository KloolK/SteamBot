using SteamKit2;
using SteamTrade;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;

namespace SteamBot
{
    class GroupManager
    {
        // Stores the members of each group
        private Dictionary<int, HashSet<SteamID>> groupMembers;

        // Inventory cache
        private Dictionary<SteamID, Tuple<Inventory, bool>> inventories;

        // Contains the list of items matching to the latest request
        private Dictionary<int, Dictionary<SteamID, List<Inventory.Item>>> groupRequests;
        // Contains the status of each request: requested and received number of items
        private Dictionary<int, Tuple<int, int>> requestStates;

        public Schema schema { get; private set; }

        private string apiKey;

        private static readonly int groupSize = 3;

        public GroupManager(string apiKey)
        {
            this.apiKey = apiKey;

            this.groupMembers = new Dictionary<int, HashSet<SteamID>>();
            this.inventories = new Dictionary<SteamID, Tuple<Inventory, bool>>();
            this.groupRequests = new Dictionary<int, Dictionary<SteamID, List<Inventory.Item>>>();

            this.schema = Schema.FetchSchema(this.apiKey);
        }

        #region inventories

        public Inventory GetInventory(SteamID id)
        {
            if (!this.inventories.ContainsKey(id))
                this.inventories.Add(id, Tuple.Create(FetchInventory(id), true));

            if (!GetInventoryClean(id))
                FetchInventory(id);

            return this.inventories[id].Item1;
        }

        private Inventory FetchInventory(SteamID id)
        {
            return Inventory.FetchInventory(id, this.apiKey);
        }

        public bool GetInventoryClean(SteamID id)
        {
            if (!this.inventories.ContainsKey(id)) return false;

            return this.inventories[id].Item2;
        }

        #endregion

        #region groups
        public void AddMember(SteamID memberID, int groupID)
        {
            // Create group if it doesn't exists already
            if (!GroupExists(groupID))
                this.groupMembers.Add(groupID, new HashSet<SteamID>());

            // Add member to the group
            this.groupMembers[groupID].Add(memberID);
        }

        public IEnumerable<SteamID> GetGroupMembers(int groupID)
        {
            if (!GroupExists(groupID)) return null;

            return this.groupMembers[groupID];
        }

        public bool CheckMembership(SteamID id, int groupID)
        {
            if (!GroupExists(groupID)) return false;

            return this.groupMembers[groupID].Contains(id);
        }

        public bool GroupExists(int groupID)
        {
            return this.groupMembers.ContainsKey(groupID);
        }

        public bool GroupComplete(int groupID)
        {
            if (!GroupExists(groupID)) return false;

            return this.groupMembers[groupID].Count == GroupManager.groupSize;
        }

        #endregion

        #region requests

        public Dictionary<SteamID, List<Inventory.Item>> GetGroupRequest(int groupID)
        {
            if (!GroupExists(groupID)) return null;

            return this.groupRequests[groupID];
        }

        public bool GetRequestCompleted(int groupID)
        {
            if (!GroupExists(groupID)) return false;

            return this.requestStates[groupID].Item2 >= this.requestStates[groupID].Item1;
        }

        public void UpdateRequestState(int groupID, int received)
        {
            if (!GroupExists(groupID)) return;

            var old = this.requestStates[groupID];
            this.requestStates[groupID] = Tuple.Create(old.Item1, received);
        }

        public void SetGroupRequest(int groupID, Dictionary<SteamID, List<Inventory.Item>> request, int requested)
        {
            if (!GroupExists(groupID)) return;

            this.groupRequests[groupID] = request;

            if (this.requestStates == null)
            {
                this.requestStates = new Dictionary<int, Tuple<int, int>>();
                this.requestStates.Add(groupID, Tuple.Create(requested, 0));
            }
            else
                this.requestStates[groupID] = Tuple.Create(requested, 0);
        }

        #endregion
    }
}
