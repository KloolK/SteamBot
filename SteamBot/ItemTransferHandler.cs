using SteamKit2;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using SteamTrade;
using SteamKit2.Internal;
using SteamKit2.GC;
using System.Threading;

namespace SteamBot
{
    class ItemTransferHandler : UserHandler
    {
        private List<Inventory.Item> myOffers;
        private Schema schema;

        public ItemTransferHandler(Bot bot, SteamID sid)
            : base(bot, sid)
        {
            this.myOffers = new List<Inventory.Item>();
        }

        public override bool OnFriendAdd()
        {
            return true;
        }

        public override void OnFriendRemove()
        {
            return;
        }

        public override void OnMessage(string message, SteamKit2.EChatEntryType type)
        {
            if (type != EChatEntryType.ChatMsg) return;

            //if (message.StartsWith("grp_status_begin"))
            //{
            //    GroupStatusMessageHandler(message);
            //    return;
            //}

            //if (!IsAdmin && !Bot.groupMembers.Contains(OtherSID))
            //{
            //    SendChatMessage(OtherSID, "halt dich da lieber raus!");
            //    return;
            //}

            if (message == "help")
            {
                string help = "\navailable commands: craft, open, close\n" +
                    "craft : craft all tradeable weapons and metal into refined, automatically goes ingame\n" +
                    "open/close : open or close tf2, not really needed\n" +
                    "send a trade request to transfer items";
                SendChatMessage(OtherSID, help);
            }
            else if (message == "open")
            {
                OpenGame();                
            }
            else if (message == "close")
            {
                CloseGame();
            }
            //else if (message.StartsWith("group"))
            //{
            //    GroupCommandHandler(message);
            //}
            else if (message == "craft")
            {
                OpenGame();
                Thread.Sleep(200);

                var crafted = CraftAll();
                CloseGame();

                double refCount = ((double)crafted.Item1) / 9;
                refCount = Math.Truncate(refCount * 100) / 100;

                SendChatMessage(OtherSID, String.Format("crafted {0} scrap ({1} refined)", crafted.Item1, refCount));
            }
            else if (message.StartsWith("move "))
            {
                string[] args = message.Split(' ');

                TF2GC.Items.SetItemPosition(this.Bot, 1785564763UL, Convert.ToUInt32(args[1]));
            }
            else if (message.StartsWith("delete "))
            {
                string[] args = message.Split(' ');

                TF2GC.Items.DeleteItem(this.Bot, ulong.Parse(args[1]));
            }
            else if (message.StartsWith("sort "))
            {
                string[] args = message.Split(' ');

                //var msg = new ClientGCMsg<MsgSort>();

                //msg.Body.sort_type = Convert.ToUInt64(args[1]);

                TF2GC.Items.SortItems(this.Bot, Convert.ToByte(args[1]));
            }
            else
            {
                SendChatMessage(OtherSID, "\nunknown command\nwrite \"help\" to view available commands");
            }
        }

        public override void OnLoginCompleted()
        {

        }

        //public override void OnConnectedToFriends()
        //{
        //    //SendToAdmin("na du");
        //}

        public override bool OnTradeRequest()
        {
            if (this.IsAdmin)
            {
                Bot.SteamFriends.SendChatMessage(OtherSID, SteamKit2.EChatEntryType.ChatMsg, "hallo mein herr :)");
                return true;
            }
            else
            {
                Bot.SteamFriends.SendChatMessage(OtherSID, SteamKit2.EChatEntryType.ChatMsg, "mit dir sicher nicht!");
                return false;
            }
        }

        public override void OnTradeError(string error)
        {
            throw new NotImplementedException();
        }

        public override void OnTradeTimeout()
        {
            throw new NotImplementedException();
        }

        public override void OnTradeInit()
        {
            Trade.SendMessage("hi there!\nwrite \"help\" to view available commands");
        }

        public override void OnTradeAddItem(SteamTrade.Schema.Item schemaItem, SteamTrade.Inventory.Item inventoryItem)
        {
            Trade.SendMessage(":)");
        }

        public override void OnTradeRemoveItem(SteamTrade.Schema.Item schemaItem, SteamTrade.Inventory.Item inventoryItem)
        {
            Trade.SendMessage(":(");
        }

        public override void OnTradeMessage(string message)
        {
            if (message == "help")
            {
                string help = "\navailable commands: add, remove\n" +
                    "usage:\n" +
                    "add/remove all : add/remove all tradeable items\n" +
                    "add/remove <type> <amount> : add/remove given amount of items of the specified type\n" +
                    "add/remove <property> <param> : add/remove all items where the specified property matches the param\n" +
                    "type may be: weapon(s), cosmetic(s), metal, tool, crate(s)\n" +
                    "amount may be: any valid number or \"all\"\n" +
                    "property may be: name, defindex, id\n" +
                    "param may be: a valid value for the selected property\n" +
                    "just ready up and click trade when you're done";
                Trade.SendMessage(help);
            }
            else if (message.StartsWith("add ") || message.StartsWith("remove "))
            {
                int moved = AddRemoveCommandHandler(message);
                if (moved >= 0) Trade.SendMessage(String.Format("Moved {0} items", moved));
                else Trade.SendMessage("\nunknown command\nwrite \"help\" to view available commands");
            }
            else
            {
                Trade.SendMessage("\nunknown command\nwrite \"help\" to view available commands");
            }
        }

        public override void OnTradeReady(bool ready)
        {
            Trade.SetReady(ready);
        }

        public override void OnTradeAccept()
        {
            Trade.AcceptTrade();
        }

        public override void OnTradeSuccess()
        {
            throw new NotImplementedException();
        }

        //private void GroupCommandHandler(string message)
        //{
        //    string[] args = message.Split(' ');
        //    //TODO args länge prüfen

        //    if (args[1] == "status")
        //    {
        //        string status = "";
        //        foreach (SteamID member in Bot.groupMembers)
        //        {
        //            //if (member == Bot.SteamClient.SteamID) status += Bot.SteamFriends.GetPersonaName() + " ";
        //            status += GetFriendName(member) + " ";
        //        }
        //        SendToAdmin(status);
        //    }
        //    else if (args[1] == "add")
        //    {
        //        if (Bot.groupMembers.Find(x => x.AccountID == Bot.SteamClient.SteamID.AccountID) == null)
        //            Bot.groupMembers.Add(Bot.SteamClient.SteamID);

        //        for (int i = 2; i < args.Length; i++)
        //        {
        //            SteamID newMember = GetFriendSteamID(args[i]);
        //            if (newMember == null)
        //            {
        //                SendToAdmin("can't find " + args[i]);
        //                continue;
        //            }
        //            if (Bot.groupMembers.Find(x => x.AccountID == newMember.AccountID) != null) continue;
        //            //TODO bestätigung anfordern
        //            Bot.groupMembers.Add(newMember);
        //        }
        //        PropagateGroupStatus();
        //    }
        //    else if (args[1] == "request")
        //    {

        //    }
        //}

        //private void PropagateGroupStatus()
        //{
        //    string message = "grp_status_begin";
        //    foreach (SteamID member in Bot.groupMembers)
        //    {
        //        message += " " + member;
        //    }
        //    //message += " grp_status_end";

        //    foreach (SteamID member in Bot.groupMembers)
        //    {
        //        if (member == Bot.SteamClient.SteamID) continue;
        //        SendChatMessage(member, message);
        //    }
        //}

        //private void GroupStatusMessageHandler(string message)
        //{
        //    string[] args = message.Split(' ');

        //    Bot.groupMembers.Clear();
        //    for (int i = 1; i < args.Length; i++)
        //    {
        //        string memberID = args[i].Trim(new char[] { ',', ' ' });
        //        Bot.groupMembers.Add(new SteamID(memberID));
        //        SendToAdmin("added " + GetFriendName(new SteamID(memberID)));
        //    }
        //}

        private int AddRemoveCommandHandler(string message)
        {
            string[] args = message.Split(' ');
            if (args.Length < 2) return -1;

            ulong number = 0, moved = 0;
            bool isNumber = false, moveAll = false;
            bool direction = args[0].Equals("add"); //true = add, false = remove

            //2 Argumente
            if (args.Length == 2)
            {
                foreach (var item in Trade.MyInventory.Items)
                {
                    if (!item.IsNotTradeable && (direction ? !myOffers.Contains(item) : myOffers.Contains(item)))
                    {
                        if (direction)
                        {
                            Trade.AddItem(item.Id);
                            myOffers.Add(item);
                        }
                        else
                        {
                            Trade.RemoveItem(item.Id);
                            myOffers.Remove(item);
                        }
                        moved++;
                        Thread.Sleep(200);
                    }
                }
                return (int)moved;
            }

            isNumber = ulong.TryParse(args[2], out number);
            moveAll = args[2] == "all";

            Predicate<Inventory.Item> typeFilter;

            //min. 3 Argumente
            switch (args[1])
            {
                case "weapon":
                case "weapons":
                    typeFilter = (x => Trade.CurrentSchema.GetItem(x.Defindex).CraftClass == "weapon");
                    break;
                case "crate":
                case "crates":
                    typeFilter = (x => Trade.CurrentSchema.GetItem(x.Defindex).ItemClass == "supply_crate");
                    break;
                case "cosmetic":
                case "cosmetics":
                    typeFilter = (x => Trade.CurrentSchema.GetItem(x.Defindex).CraftMaterialType == "hat");
                    break;
                case "metal":
                    typeFilter = (x => Trade.CurrentSchema.GetItem(x.Defindex).CraftMaterialType == "craft_bar");
                    break;
                case "tool":
                    typeFilter = (x => Trade.CurrentSchema.GetItem(x.Defindex).ItemClass == "tool");
                    break;
                case "defindex":
                    typeFilter = (x => x.Defindex == number);
                    break;
                case "id":
                    typeFilter = (x => x.Id == number || x.OriginalId == number);
                    break;
                case "name":
                    typeFilter = (x => Trade.CurrentSchema.GetItem(x.Defindex).ItemName.ToLower().Contains(args[2].ToLower()));
                    break;
                default:
                    return -1;
            }

            foreach (var item in Trade.MyInventory.Items)
            {
                if (moved >= number && isNumber && !moveAll) break;
                if (!item.IsNotTradeable && (direction ? !myOffers.Contains(item) : myOffers.Contains(item)) && typeFilter.Invoke(item))
                {
                    if (direction)
                    {
                        Trade.AddItem(item.Id);
                        myOffers.Add(item);
                    }
                    else
                    {
                        Trade.RemoveItem(item.Id);
                        myOffers.Remove(item);
                    }
                    moved++;
                    Thread.Sleep(200);
                }
            }
            return (int)moved;
        }

        private void OpenGame()
        {
            this.Bot.SetGamePlaying(440);
            //var clientMsg = new ClientMsgProtobuf<CMsgClientGamesPlayed>(EMsg.ClientGamesPlayedNoDataBlob);
            //clientMsg.Body.games_played.Add(new CMsgClientGamesPlayed.GamePlayed
            //{
            //    game_id = 440,
            //});
            //Bot.SteamClient.Send(clientMsg);
        }

        private void CloseGame()
        {
            this.Bot.SetGamePlaying(0);
            //var clientMsg = new ClientMsgProtobuf<CMsgClientGamesPlayed>(EMsg.ClientGamesPlayedNoDataBlob);
            //clientMsg.Body.games_played.Add(new CMsgClientGamesPlayed.GamePlayed
            //{
            //    game_id = 0,
            //});
            //Bot.SteamClient.Send(clientMsg);
        }

        private void Craft(short recipe, ulong[] items)
        {
            TF2GC.Crafting.CraftItems(this.Bot, recipe, items);
            //ClientGCMsg<GCMsgCraftItem> Msg = new ClientGCMsg<GCMsgCraftItem>();
            //Msg.Header.SetEMsg(1002);
            //Msg.Body.recipe = recipe;
            //foreach (Inventory.Item item in items)
            //{
            //    Msg.Body.items.Add(item.Id);
            //}
            //var gc = Bot.SteamClient.GetHandler<SteamGameCoordinator>();

            //gc.Send(Msg, 440);
        }

        private bool Scrap()
        {
            GetSchema();

            Inventory inv = Inventory.FetchInventory(Bot.SteamClient.SteamID, "CD3363A2A26BBC24AE26A287D82E8946");

            List<ulong> items = new List<ulong>();
            string[] classes = { "Scout", "Soldier", "Pyro", "Demoman", "Heavy", "Engineer", "Medic", "Sniper", "Spy" };

            for (int i = 0; i < 9; i++)
            {
                foreach (Inventory.Item item in inv.Items)
                {
                    if (!item.IsNotCraftable && !item.IsNotTradeable)
                    {
                        Schema.Item schemaItem = schema.GetItem(item.Defindex);
                        if (schemaItem.CraftClass == "weapon")
                        {
                            bool valid = false;
                            foreach (string cls in schema.GetItem(item.Defindex).UsableByClasses)
                            {
                                if (cls == classes[i])
                                {
                                    valid = true;
                                    break;
                                }
                            }
                            if (!valid) continue;

                            items.Add(item.Id);
                            if (items.Count == 2)
                            {
                                Craft(3, items.ToArray());
                                //Craft(2, items);
                                return true;
                            }
                        }
                    }
                }
                items.Clear();
            }
            return false;
        }

        private bool Reclaimed()
        {
            GetSchema();

            Inventory inv = Inventory.FetchInventory(Bot.SteamClient.SteamID, "CD3363A2A26BBC24AE26A287D82E8946");

            List<ulong> items = new List<ulong>();

            foreach (Inventory.Item item in inv.Items)
            {
                if (!item.IsNotCraftable && !item.IsNotTradeable && schema.GetItem(item.Defindex).ItemName == "Scrap Metal")
                {
                    items.Add(item.Id);
                    if (items.Count == 3)
                    {
                        Craft(4, items.ToArray());
                        return true;
                    }
                }
            }
            return false;
        }

        private bool Refined()
        {
            GetSchema();

            Inventory inv = Inventory.FetchInventory(Bot.SteamClient.SteamID, "CD3363A2A26BBC24AE26A287D82E8946");

            List<ulong> items = new List<ulong>();

            foreach (Inventory.Item item in inv.Items)
            {
                if (!item.IsNotCraftable && !item.IsNotTradeable && schema.GetItem(item.Defindex).ItemName == "Reclaimed Metal")
                {
                    items.Add(item.Id);
                    if (items.Count == 3)
                    {
                        Craft(5, items.ToArray());
                        return true;
                    }
                }
            }
            return false;
        }

        private int ScrapAll()
        {
            int counter = 0;
            while (Scrap())
            {
                counter++;
                Thread.Sleep(1000);
            }
            return counter;
        }

        private int ReclaimedAll()
        {
            int counter = 0;
            while (Reclaimed())
            {
                counter++;
                Thread.Sleep(1000);
            }
            return counter;
        }

        private int RefinedAll()
        {
            int counter = 0;
            while (Refined())
            {
                counter++;
                Thread.Sleep(1000);
            }
            return counter;
        }

        private Tuple<int, int, int> CraftAll()
        {
            int scrap = ScrapAll();
            int reclaimed = ReclaimedAll();
            int refined = RefinedAll();

            return new Tuple<int, int, int>(scrap, reclaimed, refined);
        }

        private void GetSchema()
        {
            if (schema == null)
                schema = Schema.FetchSchema("CD3363A2A26BBC24AE26A287D82E8946");
        }

        private void SendChatMessage(SteamID other, string message)
        {
            Bot.SteamFriends.SendChatMessage(other, EChatEntryType.ChatMsg, message);
        }

        private void SendToAdmin(string message)
        {
            SendChatMessage(Bot.Admins[0], message);
        }

        private string GetFriendName(ulong steamid)
        {
            return GetFriendName(new SteamID(steamid));
        }

        private string GetFriendName(SteamID steamid)
        {
            return Bot.SteamFriends.GetFriendPersonaName(steamid);
        }

        private SteamID GetFriendSteamID(string name)
        {
            for (int i = 0; i < Bot.SteamFriends.GetFriendCount(); i++)
            {
                SteamID friendID = Bot.SteamFriends.GetFriendByIndex(i);
                if (GetFriendName(friendID) == name) return friendID;
            }
            return null;
        }

        public override bool OnGroupAdd()
        {
            throw new NotImplementedException();
        }
    }
}
