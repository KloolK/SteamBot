using SteamKit2;
using SteamTrade;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;

namespace SteamBot
{
    class BackpackExpander : UserHandler
    {
        private static GroupManager groupManager;
        private static Object lockingVar = new object();

        private List<Tuple<Schema.Item, Inventory.Item>> myOffers;
        private List<Tuple<Schema.Item, Inventory.Item>> othersOffers;

        // TODO: move to settings.json
        private int groupID = 1;

        private SteamID steamID = null;

        public BackpackExpander(Bot bot, SteamKit2.SteamID id)
            : base(bot, id)
        {
            this.steamID = this.Bot.SteamClient.SteamID;

            lock (BackpackExpander.lockingVar)
            {
                if (BackpackExpander.groupManager == null)
                    BackpackExpander.groupManager = new GroupManager(this.Bot.apiKey);

                // Add the bot to its group
                // TODO: use a different data structure to enable a sorted list
                BackpackExpander.groupManager.AddMember(this.steamID, this.groupID);
            }

            // Wait for own group to be complete, so we really add all group members to friends
            while (!BackpackExpander.groupManager.GroupComplete(this.groupID)) ;

            // Add all group members to friendslist (also accepts pending invites of group members)
            AddGroupFriends();
        }

        public override bool OnFriendAdd()
        {
            if (IsAdmin || IsGroupMember(OtherSID))
                return true;
            return false;
        }

        public override void OnFriendRemove()
        {
            throw new NotImplementedException();
        }

        public override void OnMessage(string message, SteamKit2.EChatEntryType type)
        {
            if (!IsAdmin && !IsGroupMember(OtherSID))
            {
                SendMessage(OtherSID, "verpiss dich!");
                return;
            }
            // TODO: use a command enum
            if (message == "list friends")
            {
                string reply = "\n";
                for (int i = 0; i < this.Bot.SteamFriends.GetFriendCount(); i++)
                {
                    SteamID friendID = this.Bot.SteamFriends.GetFriendByIndex(i);
                    string groupMarker = IsGroupMember(friendID) ? "*" : "";
                    reply += GetFriendName(friendID) + groupMarker + "\n";
                }
                SendMessage(OtherSID, reply);
            }
            else if (message == "list group")
            {
                string reply = "\n";
                foreach (SteamID groupMember in GetGroupMembers())
                {
                    reply += GetFriendName(groupMember) + "\n";
                }
                SendMessage(OtherSID, reply);

            }
            else if (message == "items")
            {
                string reply = GetInventory().Items.Length.ToString();
                SendMessage(OtherSID, reply);
            }
            else if (message.StartsWith("trade "))
            {
                string reply = TradeCommandHandler(message);
                SendMessage(OtherSID, reply);
            }
            else if (message.StartsWith("collect "))
            {
                // remove command name
                string args = message.Substring(message.IndexOf(' ') + 1);

                CollectGroupItems(args);
            }
            else if (message.StartsWith("list "))
            {
                // remove command name
                string args = message.Substring(message.IndexOf(' ') + 1);

                // retrieve list of matching items
                var groupItems = GetGroupItems(args);

                // notify on syntax error                
                if (groupItems == null)
                {
                    SendMessage(OtherSID, "syntax error");
                    return;
                }

                // no error, parse list and send reply
                string reply = "\n";
                int total = 0, maxMatches = 0;
                SteamKit2.SteamID maxAccount = null;

                foreach (var account in groupItems.Keys)
                {
                    int count = groupItems[account].Count;
                    reply += this.Bot.SteamFriends.GetFriendPersonaName(account) + ": " + count + "\n";
                    total += count;
                    if (count > maxMatches)
                    {
                        maxMatches = count;
                        maxAccount = account;
                    }
                }
                reply += "total: " + total;
                SendMessage(OtherSID, reply);
            }
            else if (message.StartsWith("request "))
            {
                // remove command name
                string args = message.Substring(message.IndexOf(' ') + 1);

                // retrieve list of matching items
                var groupItems = GetGroupItems(args);

                // notify on syntax error                
                if (groupItems == null)
                {
                    SendMessage(OtherSID, "syntax error");
                    return;
                }

                // no error, parse list and send reply
                string reply = "\n";
                int total = 0, maxMatches = 0;
                SteamKit2.SteamID maxAccount = null;

                foreach (var account in groupItems.Keys)
                {
                    int count = groupItems[account].Count;
                    reply += this.Bot.SteamFriends.GetFriendPersonaName(account) + ": " + count + "\n";
                    total += count;
                    if (count > maxMatches)
                    {
                        maxMatches = count;
                        maxAccount = account;
                    }
                }
                reply += "total: " + total;
                SendMessage(OtherSID, reply);

                // Determine group member with most matches

                // No matches: abort
                if (maxAccount == null)
                {
                    SendMessage(OtherSID, "no items founds");
                    return;
                }
                // Other bot has most matches: tell him!
                else if (maxAccount != this.steamID)
                {
                    SendMessage(maxAccount, "collect " + args);
                }
                // This bot has most matches: start collecting
                else
                {
                    CollectGroupItems(args);
                }

            }
        }

        public override void OnLoginCompleted()
        {
            return;
        }

        public override bool OnTradeRequest()
        {
            if (!IsAdmin && !IsGroupMember(OtherSID))
            {
                SendMessage(OtherSID, "verpiss dich!");
                SendAdminMessage("Denied trade request from " + GetFriendName(OtherSID));
                return false;
            }
            this.myOffers = new List<Tuple<SteamTrade.Schema.Item, SteamTrade.Inventory.Item>>();
            this.othersOffers = new List<Tuple<SteamTrade.Schema.Item, SteamTrade.Inventory.Item>>();
            SendAdminMessage("Accepted trade request from " + GetFriendName(OtherSID));
            return true;
        }

        public override void OnTradeError(string error)
        {
            SendAdminMessage("trade error");
        }

        public override void OnTradeTimeout()
        {
            SendAdminMessage("trade timeout");
        }

        public override void OnTradeInit()
        {
            SendAdminMessage("trade initialized");

            // 
            //if (BackpackExpander.groupRequests[this.groupID].ContainsKey(this.steamID))
        }

        public override void OnTradeAddItem(Schema.Item schemaItem, Inventory.Item inventoryItem)
        {
            this.othersOffers.Add(Tuple.Create(schemaItem, inventoryItem));
        }

        public override void OnTradeRemoveItem(Schema.Item schemaItem, Inventory.Item inventoryItem)
        {
            this.othersOffers.Remove(this.othersOffers.Find(x => x.Item2.Id == inventoryItem.Id));
        }

        public override void OnTradeMessage(string message)
        {
            //throw new NotImplementedException();
        }

        public override void OnTradeReady(bool ready)
        {
            this.Trade.SetReady(ready);
        }

        public override void OnTradeAccept()
        {
            this.Trade.AcceptTrade();
        }

        public override void OnTradeSuccess()
        {
            throw new NotImplementedException();
        }

        private Dictionary<SteamKit2.SteamID, List<Inventory.Item>> GetGroupItems(string args)
        {
            // determine requested amount
            var splitArgs = args.Split(' ');
            //int requested = 0;
            //bool all = false;

            // TODO: use command line switches: -t type, -definex, -id, -name, -c 123
            /*if (splitArgs.Last() == "all")
                all = true;
            else
                if (int.TryParse(splitArgs.Last(), out requested))
                    all = false;
                else
                    return null;

             */

            // get filter predicate
            Predicate<Inventory.Item> filter = GetFilterPredicate(args);

            // add matching items to list
            Dictionary<SteamKit2.SteamID, List<Inventory.Item>> accountItems = new Dictionary<SteamKit2.SteamID, List<Inventory.Item>>();
            //int matches = 0;

            foreach (var groupMember in GetGroupMembers())
            {
                //if (matches >= requested && !all) break;

                Inventory inventory = GetInventory(groupMember);
                //Inventory inventory = BackpackExpander.inventories[groupMember];
                accountItems[groupMember] = new List<Inventory.Item>();

                foreach (var item in inventory.Items)
                {
                    //if (matches >= requested && !all) break;
                    if (filter.Invoke(item))
                    {
                        accountItems[groupMember].Add(item);
                        //matches++;
                    }
                }
            }
            return accountItems;
        }

        private void CollectGroupItems(string args)
        {
            int requested = 0;
            bool all = true;
            var splitArgs = args.Split(' ');

            // TODO: crashes if amount is missing
            // TODO: make all the default amount
            if (splitArgs.Last() != "all")
            {
                requested = Int32.Parse(splitArgs.Last());
                all = false;
            }

            var request = GroupRequest(GetGroupItems(args), requested);

            // receive requested items from group members
            int received = request[this.steamID].Count;

            foreach (SteamID groupMember in request.Keys)
            {
                if (groupMember == this.steamID) continue;
                if (received >= requested && !all) break;

                string itemIDs = "";
                foreach (var item in request[groupMember])
                {
                    if (received >= requested && !all) break;

                    itemIDs += item.Id + " ";
                    received++;
                }

                SendMessage(groupMember, "give " + itemIDs);
            }

            // Check if we already have enough items
            UpdateRequestState(request[this.steamID].Count);
            if (GetRequestCompleted()) return;

            // Removes ourself so we dont get selected as next trade partner
            request.Remove(this.steamID);

            // Determine the next (first) trade partner send a trade request
            SteamID next = GetNextTradePartner();
            if (next != null)
                this.Bot.OpenTrade(next);
        }

        private Predicate<Inventory.Item> GetFilterPredicate(string args)
        {
            // parse and validate arguments
            string[] filterArgs = args.Split(' ');
            if (filterArgs.Length == 0 || filterArgs.Length > 3) return null;

            // determine filter predicate
            Predicate<Inventory.Item> filter;
            Schema schema = GetSchema();

            switch (filterArgs[0])
            {
                case "weapon":
                case "weapons":
                    filter = (x => schema.GetItem(x.Defindex).CraftClass == "weapon");
                    break;
                case "crate":
                case "crates":
                    filter = (x => schema.GetItem(x.Defindex).ItemClass == "supply_crate");
                    break;
                case "cosmetic":
                case "cosmetics":
                    filter = (x => schema.GetItem(x.Defindex).CraftMaterialType == "hat");
                    break;
                case "metal":
                    filter = (x => schema.GetItem(x.Defindex).CraftMaterialType == "craft_bar");
                    break;
                case "tool":
                    filter = (x => schema.GetItem(x.Defindex).ItemClass == "tool");
                    break;
                case "defindex":
                    ushort defindex;
                    if (!ushort.TryParse(filterArgs[1], out defindex)) return null;
                    filter = (x => x.Defindex == defindex);
                    break;
                case "id":
                    ulong id;
                    if (!ulong.TryParse(filterArgs[1], out id)) return null;
                    filter = (x => x.Id == id || x.OriginalId == id);
                    break;
                case "name":
                    filter = (x => schema.GetItem(x.Defindex).ItemName.ToLower().Contains(filterArgs[1].ToLower()));
                    break;
                default:
                    return null;
            }

            return filter;
        }

        private string TradeCommandHandler(string message)
        {
            // parse and validate arguments
            string[] args = message.Split(' ');
            if (args.Length == 3 && args[1] == "init")
            {
                SteamKit2.SteamID friend = GetFriendSteamID(args[2]);
                if (friend == null)
                    return args[2] + " is not in my friendslist";

                this.Bot.SteamTrade.Trade(friend);
                return "trading " + args[2];
            }
            else return "syntax error";


            int requested = 0;
            bool all = false;

            if (args.Last() == "all")
                all = true;
            else
                if (int.TryParse(args.Last(), out requested))
                    all = false;
                else
                    return null;
        }

        private List<Inventory.Item> GetMatchingItems(SteamID steamID, Predicate<Inventory.Item> filter)
        {
            return null;
        }

        #region GroupManager helpers
        private Inventory GetInventory()
        {
            return GetInventory(this.steamID);
        }
        private Inventory GetInventory(SteamID id)
        {
            return BackpackExpander.groupManager.GetInventory(id);
        }

        private IEnumerable<SteamID> GetGroupMembers()
        {
            return BackpackExpander.groupManager.GetGroupMembers(this.groupID);
        }
        private void AddGroupFriends()
        {
            foreach (SteamID groupMember in GetGroupMembers())
            {
                if (this.Bot.SteamFriends.GetFriendRelationship(groupMember) != SteamKit2.EFriendRelationship.Friend)
                    this.Bot.SteamFriends.AddFriend(groupMember);
            }
        }

        private bool IsGroupMember(SteamID id)
        {
            return BackpackExpander.groupManager.CheckMembership(id, this.groupID);
        }
        private Dictionary<SteamID, List<Inventory.Item>> GroupRequest(Dictionary<SteamID, List<Inventory.Item>> request, int requested)
        {
            BackpackExpander.groupManager.SetGroupRequest(this.groupID, request, requested);
            return BackpackExpander.groupManager.GetGroupRequest(this.groupID);
        }

        private Dictionary<SteamID, List<Inventory.Item>> GetGroupRequest()
        {
            return BackpackExpander.groupManager.GetGroupRequest(this.groupID);
        }

        private bool GetRequestCompleted()
        {
            return BackpackExpander.groupManager.GetRequestCompleted(this.groupID);
        }

        private void UpdateRequestState(int received)
        {
            BackpackExpander.groupManager.UpdateRequestState(this.groupID, received);
        }

        private Schema GetSchema()
        {
            return BackpackExpander.groupManager.schema;
        }

        #endregion

        #region friend helpers

        private string GetFriendName(SteamID id)
        {
            return this.Bot.SteamFriends.GetFriendPersonaName(id);
        }

        private SteamID GetFriendSteamID(string name)
        {
            for (int i = 0; i < this.Bot.SteamFriends.GetFriendCount(); i++)
            {
                SteamID friendID = this.Bot.SteamFriends.GetFriendByIndex(i);
                if (GetFriendName(friendID) == name) return friendID;
            }
            return null;
        }

        private void SendAdminMessage(string message)
        {
            SteamID admin = new SteamID(this.Bot.Admins.First());
            SendMessage(admin, message);
        }

        private void SendMessage(SteamID id, string message)
        {
            this.Bot.SteamFriends.SendChatMessage(id, EChatEntryType.ChatMsg, message);
        }

        #endregion

        #region trade helpers

        private SteamID GetNextTradePartner()
        {
            // Get the current group request
            var request = GetGroupRequest();

            // Find the inventory with the most matches
            SteamID maxID = request.First().Key;
            foreach (var pair in request)
            {
                if (pair.Value.Count > request[maxID].Count)
                    maxID = pair.Key;
            }
            return maxID;
        }

        private void TradeAddItem(Schema.Item schemaItem, Inventory.Item inventoryItem)
        {
            this.myOffers.Add(Tuple.Create(schemaItem, inventoryItem));
        }

        private void TradeRemoveItem(Schema.Item schemaItem, Inventory.Item inventoryItem)
        {
            this.myOffers.Remove(this.myOffers.Find(x => x.Item2.Id == inventoryItem.Id));
        }

        #endregion
    }
}
