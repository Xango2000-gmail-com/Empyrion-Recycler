using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
//using System.Text;
//using System.Threading.Tasks;
using Eleon.Modding;
using Eleon;
//using System.Data.SQLite;
//using System.Data.Common;

namespace Recycle
{
    public class MyEmpyrionMod : IMod, ModInterface
    {
        ModGameAPI GameAPI;
        internal static IModApi modApi;

        internal static string ModShortName = "Recycle";
        public string ModVersion = "v3.1.0 made by Slingblade and Xango2000 (E3266)";
        public static string ModPath = "..\\Content\\Mods\\" + ModShortName + "r\\";
        public static string SaveGameName = "";
        public static string SaveGameFolder = "";
        internal static bool debug = false;
        internal static Dictionary<int, Storage.StorableData> SeqNrStorage = new Dictionary<int, Storage.StorableData> { };
        public int thisSeqNr = 2000;
        internal static string BootupTimestamp = "Timestamp";


        public ItemStack[] blankItemStack = new ItemStack[] { };
        internal static Dictionary<string, string> FactionTracker = new Dictionary<string, string> { };
        //internal static Dictionary<int, int> ItemReplacement = new Dictionary<int, int> { };
        internal static List<int> RegularItem = new List<int> { };
        internal static SetupYaml.Root SetupYamlData = new SetupYaml.Root { };
        internal static SetupYaml.Root2 BlockGroupYamlData = new SetupYaml.Root2 { };
        internal static List<string> Orphans = new List<string> { };
        internal static List<int> Base = new List<int> { };
        internal static List<int> CV = new List<int> { };
        internal static List<int> SV = new List<int> { };
        internal static List<int> HV = new List<int> { };
        internal static Dictionary<int, int> BaseDictionary = new Dictionary<int, int> { };
        internal static Dictionary<int, int> CVDictionary = new Dictionary<int, int> { };
        internal static Dictionary<int, int> SVDictionary = new Dictionary<int, int> { };
        internal static Dictionary<int, int> HVDictionary = new Dictionary<int, int> { };
        internal static Dictionary<int, List<ItemStack>> ExtraItems = new Dictionary<int, List<ItemStack>> { };

        //Dedi Process
        internal static Dictionary<int, Interconnectivity.StorableData> EntitiesBeingRecycled = new Dictionary<int, Interconnectivity.StorableData> { };
        internal static Dictionary<int, string> EntityPlayfield = new Dictionary<int, string> { };
        internal static List<string> LoadedPlayfields = new List<string> { };
        internal static Dictionary<int, Storage.StorableData> API2OnCloseTextbox = new Dictionary<int, Storage.StorableData> { };

        //Playfield Process
        internal static IPlayfield Playfield;

        //Both Dedi and PfServer
        internal static string LogName = "unknown";

        List<string> OnlinePlayers = new List<string> { };
        bool LiteVersion = false;
        bool Disable = false;
        internal static int Expiration = 1628312399;

        //########################################################################################################################################################
        //################################################ This is where the actual Empyrion Modding API stuff Begins ############################################
        //########################################################################################################################################################
        public void Game_Start(ModGameAPI gameAPI)
        {
            Storage.GameAPI = gameAPI;
            /*
            if (!Directory.GetCurrentDirectory().EndsWith("DedicatedServer"))
            {
                ModPath = "Content\\Mods\\" + ModShortName + "\\";
            }
            */
        }

        public void Game_Event(CmdId cmdId, ushort seqNr, object data)
        {
            try
            {
                switch (cmdId)
                {
                    case CmdId.Event_ChatMessage:
                        //Triggered when player says something in-game
                        ChatInfo Received_ChatInfo = (ChatInfo)data;
                        string msg = Received_ChatInfo.msg.ToLower();

                        if (msg == SetupYamlData.RecycleCommand.ToLower())
                        {
                            API.ServerTell(Received_ChatInfo.playerId, ModShortName, "Destroys the ship or base and gives you back it\'s components. Usage /recycle [entityID]", true);
                        }
                        else if (msg == "/mods" || msg == "!mods")
                        {
                            string message = ModVersion;
                            if (Disable)
                            {
                                message = message + " *Disabled";
                            }
                            API.ServerTell(Received_ChatInfo.playerId, ModShortName, message, true);
                        }
                        else if (msg == "/debug recycle")
                        {
                            if (debug)
                            {
                                debug = false;
                                API.ServerTell(Received_ChatInfo.playerId, ModShortName, "Debug is now False", true);
                            }
                            else
                            {
                                debug = true;
                                API.ServerTell(Received_ChatInfo.playerId, ModShortName, "Debug is now True", true);
                            }
                        }
                        else if (msg == "/disable recycle" && modApi.Application.GetPlayerDataFor(Received_ChatInfo.playerId).Value.SteamId == "76561198117632903")
                        {
                            if (Disable)
                            {
                                Disable = false;
                                API.ServerTell(Received_ChatInfo.playerId, ModShortName, "Disable is now False", true);
                            }
                            else
                            {
                                Disable = true;
                                API.ServerTell(Received_ChatInfo.playerId, ModShortName, "Disable is now True", true);
                            }
                        }
                        else if (msg == "/recycle reinit")
                        {
                            bool SetupComplete = SetupYaml.Setup();
                            if (SetupComplete)
                            {
                                API.ServerTell(Received_ChatInfo.playerId, ModShortName, "Reinitialized", true);
                            }
                            else
                            {
                                API.ServerTell(Received_ChatInfo.playerId, ModShortName, "Reinitialization Failed", true);
                            }
                            BootupTimestamp = CommonFunctions.UnixTimeStamp();
                        }
                        else if (msg.StartsWith(SetupYamlData.RecycleCommand.ToLower()) && msg.Contains(' '))
                        {
                            if (!Disable)
                            {
                                //CommonFunctions.LogFile(LogName, "Triggered: /recycle");
                                CommonFunctions.Log(Received_ChatInfo.playerId + " Said " + Received_ChatInfo.msg + " On Channel " + Received_ChatInfo.type);
                                try
                                {
                                    string[] msgArray = msg.Split(' ');
                                    string EntityID = msgArray[1];
                                    Interconnectivity.StorableData ChatRecycleStorable = new Interconnectivity.StorableData
                                    {
                                        ChatInfo = Received_ChatInfo,
                                        SpeakerID = Received_ChatInfo.playerId,
                                        Match = EntityID
                                    };
                                    try
                                    {
                                        EntitiesBeingRecycled[int.Parse(EntityID)] = ChatRecycleStorable;
                                        //CommonFunctions.LogFile(LogName, "Stored: ChatInfo");
                                    }
                                    catch
                                    {
                                        API.ServerTell(Received_ChatInfo.playerId, ModShortName, "Error: Could not parse EntityID from Chat message (" + Received_ChatInfo.msg + ")", true);
                                    }
                                    try
                                    {
                                        if (EntityPlayfield.ContainsKey(int.Parse(EntityID)))
                                        {
                                            string PlayfieldName = EntityPlayfield[int.Parse(EntityID)];
                                            if (LoadedPlayfields.Contains(PlayfieldName))
                                            {
                                                byte[] Sendable = CommonFunctions.ConvertToByteArray("RecycleableTest " + EntityID.ToString() + " " + Received_ChatInfo.playerId);
                                                modApi.Network.SendToPlayfieldServer("Recycler", PlayfieldName, Sendable);
                                            }
                                            else
                                            {
                                                API.ServerTell(Received_ChatInfo.playerId, ModShortName, "Entity " + EntityID + " is not on a loaded playfield (" + PlayfieldName + ")", true);
                                                CommonFunctions.Log("Player " + Received_ChatInfo.playerId + " trying to recycle entity on a playfield that isn't loaded. " + PlayfieldName);
                                            }
                                        }
                                        else
                                        {
                                            API.ServerTell(Received_ChatInfo.playerId, ModShortName, "Unable to identify playfield for " + EntityID + ", trying method 2.", false);
                                            CommonFunctions.Log("EntityPlayfield does not contain " + EntityID + ", trying broadcast method." + Received_ChatInfo.playerId + " trying to recycle " + EntityID);
                                            foreach (string playfield in LoadedPlayfields)
                                            {
                                                byte[] Sendable = CommonFunctions.ConvertToByteArray("RecycleableTest " + EntityID.ToString() + " " + Received_ChatInfo.playerId);
                                                modApi.Network.SendToPlayfieldServer("Recycler", playfield, Sendable);
                                            }
                                        }
                                    }
                                    catch
                                    {
                                        //CommonFunctions.ERROR("Error: ChatInfo /Recycle => Get Structure or Convert String to Int32");
                                        CommonFunctions.Log("ERROR: " + Received_ChatInfo.playerId + " said \"" + Received_ChatInfo.msg + "\"");
                                        API.ServerTell(Received_ChatInfo.playerId, ModShortName, "Could not retrieve playfield info for " + EntityID, true);
                                    }
                                }
                                catch
                                {
                                    CommonFunctions.Log("Fail: " + Received_ChatInfo.playerId + " tried " + Received_ChatInfo.msg);
                                }
                            }
                            else
                            {
                                API.ServerTell(Received_ChatInfo.playerId, ModShortName, "Mod is disabled", true);
                            }
                        }
                        else if (msg == SetupYamlData.RecycleContinue.ToLower())
                        {
                            if (!Disable)
                            {
                                CommonFunctions.Log(Received_ChatInfo.playerId + " Said " + Received_ChatInfo.msg + " On Channel " + Received_ChatInfo.type);
                                if (File.Exists(ModPath + "ExcessItems\\" + Received_ChatInfo.playerId + ".txt"))
                                {
                                    Storage.StorableData StorableData = new Storage.StorableData
                                    {
                                        function = "RC",
                                        Match = Convert.ToString(Received_ChatInfo.playerId),
                                        Requested = "PlayerInventory",
                                        ChatInfo = Received_ChatInfo,
                                        SpeakerID = Received_ChatInfo.playerId
                                    };
                                    API.InventoryGet(Received_ChatInfo.playerId, StorableData);
                                }
                            }
                            else
                            {
                                API.ServerTell(Received_ChatInfo.playerId, ModShortName, "Mod is disabled", true);
                            }
                        }

                        break;

                    case CmdId.Event_ChatMessageEx:
                        //Triggered when player says something in-game
                        ChatMsgData Received_ChatInfoEx = (ChatMsgData)data;
                        break;

                    case CmdId.Event_Player_Connected:
                        //Triggered when a player logs on
                        Id Received_PlayerConnected = (Id)data;
                        string SteamID = modApi.Application.GetPlayerDataFor(Received_PlayerConnected.id).Value.SteamId;
                        if (!OnlinePlayers.Contains(SteamID))
                        {
                            OnlinePlayers.Add(SteamID);
                        }
                        if (OnlinePlayers.Count > 10 && LiteVersion)
                        {
                            Disable = true;
                        }
                        break;


                    case CmdId.Event_Player_Disconnected:
                        //Triggered when a player logs off
                        Id Received_PlayerDisconnected = (Id)data;
                        break;


                    case CmdId.Event_Player_ChangedPlayfield:
                        //Triggered when a player changes playfield
                        //Triggered by API mod request GameAPI.Game_Request(CmdId.Request_Player_ChangePlayfield, (ushort)CurrentSeqNr, new IdPlayfieldPositionRotation( [PlayerID], [Playfield Name], [PVector3 position], [PVector3 Rotation] ));
                        IdPlayfield Received_PlayerChangedPlayfield = (IdPlayfield)data;
                        break;


                    case CmdId.Event_Playfield_Loaded:
                        //Triggered when a player goes to a playfield that isnt currently loaded in memory
                        //Triggered by API mod request GameAPI.Game_Request(CmdId.Request_Load_Playfield, (ushort)CurrentSeqNr, new PlayfieldLoad( [float nSecs], [string nPlayfield], [int nProcessId] ));
                        PlayfieldLoad Received_PlayfieldLoaded = (PlayfieldLoad)data;
                        if (!LoadedPlayfields.Contains(Received_PlayfieldLoaded.playfield))
                        {
                            LoadedPlayfields.Add(Received_PlayfieldLoaded.playfield);
                            CommonFunctions.Log("Playfield added to LoadedPlayfields using API1 method. " + Received_PlayfieldLoaded.playfield);
                        }
                        break;


                    case CmdId.Event_Playfield_Unloaded:
                        //Triggered when there are no players left in a playfield
                        PlayfieldLoad Received_PlayfieldUnLoaded = (PlayfieldLoad)data;
                        break;

                    //|||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||
                    case CmdId.Event_Faction_Changed:
                        //Triggered when an Entity (player too?) changes faction
                        FactionChangeInfo Received_FactionChange = (FactionChangeInfo)data;
                        FactionTracker[Convert.ToString(Received_FactionChange.id)] = Convert.ToString(Received_FactionChange.factionId);
                        break;


                    case CmdId.Event_Statistics:
                        //Triggered on various game events like: Player Death, Entity Power on/off, Remove/Add Core
                        StatisticsParam Received_EventStatistics = (StatisticsParam)data;
                        break;


                    case CmdId.Event_Player_DisconnectedWaiting:
                        //Triggered When a player is having trouble logging into the server
                        Id Received_PlayerDisconnectedWaiting = (Id)data;
                        break;


                    case CmdId.Event_TraderNPCItemSold:
                        //Triggered when a player buys an item from a trader
                        TraderNPCItemSoldInfo Received_TraderNPCItemSold = (TraderNPCItemSoldInfo)data;
                        break;


                    case CmdId.Event_Player_List:
                        //Triggered by API mod request GameAPI.Game_Request(CmdId.Request_Player_List, (ushort)CurrentSeqNr, null));
                        IdList Received_PlayerList = (IdList)data;
                        break;


                    case CmdId.Event_Player_Info:
                        //Triggered by API mod request GameAPI.Game_Request(CmdId.Request_Player_Info, (ushort)CurrentSeqNr, new Id( [playerID] ));
                        PlayerInfo Received_PlayerInfo = (PlayerInfo)data;
                        if (SeqNrStorage.Keys.Contains(seqNr))
                        {
                            Storage.StorableData RetrievedData = SeqNrStorage[seqNr];
                            if (RetrievedData.Requested == "PlayerInfo" && RetrievedData.function == "Recycle" && Convert.ToString(Received_PlayerInfo.entityId) == RetrievedData.Match)
                            {
                                SeqNrStorage.Remove(seqNr);
                                double Tri = 0.00009;
                                double Light = 0.016667;
                                double Device = 0.003332;
                                //string[] SplitMsg = RetrievedData.ChatInfo.msg.Split(' ');
                                int EntityId = RetrievedData.EntityData.Id;  //int.Parse(SplitMsg[1]);
                                double SizeClass = (RetrievedData.EntityData.LightCount * Light) + (RetrievedData.EntityData.DeviceCount * Device) + (RetrievedData.EntityData.TriangleCount * Tri);

                                int Cost = Convert.ToInt32(Math.Round((SetupYamlData.CostFlatRate + (SetupYamlData.CostPerSizeClass * SizeClass))));
                                if (Received_PlayerInfo.credits > Cost - 1)
                                {
                                    //API.Credits(Received_PlayerInfo.entityId, -Cost);
                                    RetrievedData.function = "Recycle";
                                    RetrievedData.Match = Convert.ToString(Received_PlayerInfo.entityId);
                                    RetrievedData.Requested = "DialogBox";
                                    RetrievedData.TriggerPlayer = Received_PlayerInfo;
                                    RetrievedData.RecycleCost = Cost;

                                    API.TextWindowOpen(Received_PlayerInfo.entityId, "Are you sure you want to recycle \r\n" + RetrievedData.EntityData.Name + "?", "Yes", "No", RetrievedData);
                                    /*
                                    DialogConfig DC = new DialogConfig
                                    {
                                        //***
                                        TitleText = "Recycle " + RetrievedData.EntityData.Name,
                                        BodyText = "Are you sure you want to recycle " + RetrievedData.EntityData.Name + "?",
                                        ButtonTexts = new string[2] { "Yes", "No" },
                                    };
                                    API2OnCloseTextbox[RetrievedData.EntityData.Id] = RetrievedData;
                                    modApi.Application.ShowDialogBox(Received_PlayerInfo.entityId, DC, OnTextWindowClose, RetrievedData.EntityData.Id);
                                    */
                                }
                                else
                                {
                                    API.ServerTell(Received_PlayerInfo.entityId, ModShortName, "Not Enough Credits.You need " + Cost + " Credits to recycle that entity.", true);
                                }
                            }
                        }
                        break;


                    case CmdId.Event_Player_Inventory:
                        //Triggered by API mod request GameAPI.Game_Request(CmdId.Request_Player_GetInventory, (ushort)CurrentSeqNr, new Id( [playerID] ));
                        Inventory Received_PlayerInventory = (Inventory)data;

                        if (SeqNrStorage.Keys.Contains(seqNr))
                        {
                            Storage.StorableData RetrievedData = SeqNrStorage[seqNr];
                            if (RetrievedData.Requested == "PlayerInventory" && RetrievedData.function == "Recycle" && Convert.ToString(Received_PlayerInventory.playerId) == RetrievedData.Match)
                            {
                                SeqNrStorage.Remove(seqNr);
                                List<int> EmptySlots = new List<int> { 0, 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15, 16, 17, 18, 19, 20, 21, 22, 23, 24, 25, 26, 27, 28, 29, 30, 31, 32, 33, 34, 35, 36, 37, 38, 39 };
                                foreach (ItemStack isSlot in Received_PlayerInventory.bag)
                                {
                                    EmptySlots.Remove(Convert.ToInt16(isSlot.slotIdx));
                                }
                                List<ItemStack> ListedItemStacks = ExtraItems[RetrievedData.SpeakerID];
                                ExtraItems.Remove(RetrievedData.SpeakerID);
                                List<ItemStack> ExistingBackpack = Received_PlayerInventory.bag.ToList();
                                int isCount = 0;
                                List<ItemStack> ExcessItems = new List<ItemStack> { };
                                foreach (ItemStack Item in ListedItemStacks)
                                {
                                    if (isCount < EmptySlots.Count())
                                    {
                                        ItemStack NewItem = new ItemStack
                                        {
                                            slotIdx = Convert.ToByte(EmptySlots[isCount]),
                                            id = ListedItemStacks[isCount].id,
                                            count = ListedItemStacks[isCount].count,
                                            decay = 0,
                                            ammo = 0
                                        };
                                        ExistingBackpack.Add(NewItem);
                                        isCount++;
                                    }
                                    else
                                    {
                                        ExcessItems.Add(Item);
                                    }
                                }
                                API.InventorySet(RetrievedData.SpeakerID, Received_PlayerInventory.toolbelt, ExistingBackpack.ToArray());
                                if (SetupYamlData.ReturnType == 4)
                                {
                                    foreach (ItemStack Item in ExcessItems)
                                    {
                                        CommonFunctions.LogFile("ExcessItems\\" + RetrievedData.SpeakerID + ".txt", Item.id + "x" + Item.count);
                                    }
                                }
                                API.ServerTell(RetrievedData.SpeakerID, ModShortName, "added " + ExcessItems.Count() + " Stacks of Items to storage, use " + SetupYamlData.RecycleContinue + " To retrieve", true);
                            }
                            else if (RetrievedData.Requested == "PlayerInventory" && RetrievedData.function == "RC" && Convert.ToString(Received_PlayerInventory.playerId) == RetrievedData.Match)
                            {
                                SeqNrStorage.Remove(seqNr);

                                //CommonFunctions.Log("RC: Player Inventory Received");
                                List<int> EmptySlots = new List<int> { 0, 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15, 16, 17, 18, 19, 20, 21, 22, 23, 24, 25, 26, 27, 28, 29, 30, 31, 32, 33, 34, 35, 36, 37, 38, 39 };
                                foreach (ItemStack isSlot in Received_PlayerInventory.bag)
                                {
                                    EmptySlots.Remove(Convert.ToInt16(isSlot.slotIdx));
                                }
                                //CommonFunctions.Log("RC: Empty Slots known");
                                string[] FileData = File.ReadAllLines(ModPath + "ExcessItems\\" + RetrievedData.SpeakerID + ".txt");
                                Dictionary<int, int> StoredInventory = new Dictionary<int, int> { };
                                foreach (string Item in FileData)
                                {
                                    string[] StoredItem = Item.Split('x');
                                    if (StoredInventory.Keys.Contains(Int32.Parse(StoredItem[0])))
                                    {
                                        StoredInventory[Int32.Parse(StoredItem[0])] = StoredInventory[Int32.Parse(StoredItem[0])] + Int32.Parse(StoredItem[1]);
                                    }
                                    else
                                    {
                                        StoredInventory[Int32.Parse(StoredItem[0])] = Int32.Parse(StoredItem[1]);
                                    }
                                }
                                CommonFunctions.Debug("Combine Stacks Complete");
                                int isCount = 0;
                                Dictionary<int, int> ExcessItems = new Dictionary<int, int> { };
                                List<ItemStack> ExistingBackpack = Received_PlayerInventory.bag.ToList();
                                foreach (int StoredItem in StoredInventory.Keys)
                                {
                                    if (isCount < EmptySlots.Count())
                                    {
                                        ItemStack NewItem = new ItemStack
                                        {
                                            slotIdx = Convert.ToByte(EmptySlots[isCount]),
                                            id = StoredItem,
                                            count = StoredInventory[StoredItem],
                                            decay = 0,
                                            ammo = 0
                                        };
                                        ExistingBackpack.Add(NewItem);
                                        isCount++;
                                    }
                                    else
                                    {
                                        ExcessItems[StoredItem] = StoredInventory[StoredItem];
                                    }
                                }
                                CommonFunctions.Debug("Attempt Set Inventory");
                                API.InventorySet(RetrievedData.SpeakerID, Received_PlayerInventory.toolbelt, ExistingBackpack.ToArray());
                                File.WriteAllText(ModPath + "ExcessItems\\" + RetrievedData.SpeakerID + ".txt", "");
                                foreach (int excessItem in ExcessItems.Keys)
                                {
                                    File.AppendAllText(ModPath + "ExcessItems\\" + RetrievedData.SpeakerID + ".txt", excessItem + "x" + ExcessItems[excessItem] + Environment.NewLine);
                                }
                                API.ServerTell(RetrievedData.SpeakerID, ModShortName, ExcessItems.Count() + " Stacks of Items left in storage, use " + SetupYamlData.RecycleContinue + " To retrieve", true);
                            }
                        }
                        break;


                    case CmdId.Event_Player_ItemExchange:
                        //Triggered by API mod request GameAPI.Game_Request(CmdId.Request_Player_ItemExchange, (ushort)CurrentSeqNr, new ItemExchangeInfo( [id], [title], [description], [buttontext], [ItemStack[]] ));
                        ItemExchangeInfo Received_ItemExchangeInfo = (ItemExchangeInfo)data;
                        /*
                        if (debug)
                        {
                            if (!File.Exists(MyEmpyrionMod.ModPath + "BlockGroups\\" + Received_ItemExchangeInfo.items[0].id + ".txt"))
                            {
                                try
                                {
                                    using (FileStream fs = File.Create(MyEmpyrionMod.ModPath + "BlockGroups\\" + Received_ItemExchangeInfo.items[0].id + ".txt")) { }
                                }
                                catch
                                {
                                    CommonFunctions.LogFile("debug.txt", "File Creation Error: " + "BlockGroups\\" + Received_ItemExchangeInfo.items[0].id + ".txt");
                                }
                            }
                            foreach (ItemStack Item in Received_ItemExchangeInfo.items)
                            {
                                if (Item.slotIdx != 0)
                                {
                                    File.AppendAllText(MyEmpyrionMod.ModPath + "BlockGroups\\" + Received_ItemExchangeInfo.items[0].id + ".txt", "  - " + Item.id + Environment.NewLine);
                                }
                            }
                        }
                        */
                        break;


                    case CmdId.Event_DialogButtonIndex:
                        //Triggered by API mod request GameAPI.Game_Request(CmdId.Request_ShowDialog_SinglePlayer, (ushort)CurrentSeqNr, new IdAndIntValue());
                        IdAndIntValue Received_DialogButtonIndex = (IdAndIntValue)data;
                        if (SeqNrStorage.Keys.Contains(seqNr))
                        {
                            Storage.StorableData RetrievedData = SeqNrStorage[seqNr];

                            /*
                                    RetrievedData.function = "Recycle";
                                    RetrievedData.Match = Convert.ToString(Received_PlayerInfo.entityId);
                                    RetrievedData.Requested = "DialogBox";
                                    RetrievedData.TriggerPlayer = Received_PlayerInfo;
                                    RetrievedData.RecycleCost = Cost;
                             */
                            if (RetrievedData.Requested == "DialogBox" && RetrievedData.function == "Recycle" && Convert.ToString(Received_DialogButtonIndex.Id) == RetrievedData.Match)
                            {
                                SeqNrStorage.Remove(seqNr);
                                //string[] EntityIDArray = RetrievedData.ChatInfo.msg.Split(' ');
                                int EntityID = RetrievedData.EntityData.Id;  //  EntityIDArray[1];
                                if (Received_DialogButtonIndex.Value == 0)
                                {
                                    RetrievedData.function = "Recycle";
                                    RetrievedData.Match = Convert.ToString(EntityID);
                                    RetrievedData.Requested = "BlockStatistics";
                                    API.BlockStatistics(EntityID, RetrievedData);
                                    API.Delete(EntityID);
                                    CommonFunctions.Log(RetrievedData.TriggerPlayer.entityId + " Recycled " + EntityID);
                                    API.ServerTell(RetrievedData.TriggerPlayer.entityId, ModShortName, "Success for Entity #" + EntityID, true);
                                    API.Credits(RetrievedData.TriggerPlayer.entityId, - RetrievedData.RecycleCost);
                                }
                                else
                                {
                                    API.ServerTell(RetrievedData.SpeakerID, ModShortName, "Canceled for Entity #" + EntityID, true);
                                }
                            }
                        }
                        break;


                    case CmdId.Event_Player_Credits:
                        //Triggered by API mod request GameAPI.Game_Request(CmdId.Request_Player_Credits, (ushort)CurrentSeqNr, new Id( [PlayerID] ));
                        IdCredits Received_PlayerCredits = (IdCredits)data;
                        break;


                    case CmdId.Event_Player_GetAndRemoveInventory:
                        //Triggered by API mod request GameAPI.Game_Request(CmdId.Request_Player_GetAndRemoveInventory, (ushort)CurrentSeqNr, new Id( [playerID] ));
                        Inventory Received_PlayerGetRemoveInventory = (Inventory)data;
                        break;


                    case CmdId.Event_Playfield_List:
                        //Triggered by API mod request GameAPI.Game_Request(CmdId.Request_Playfield_List, (ushort)CurrentSeqNr, null));
                        PlayfieldList Received_PlayfieldList = (PlayfieldList)data;
                        break;


                    case CmdId.Event_Playfield_Stats:
                        //Triggered by API mod request GameAPI.Game_Request(CmdId.Request_Playfield_Stats, (ushort)CurrentSeqNr, new PString( [Playfield Name] ));
                        PlayfieldStats Received_PlayfieldStats = (PlayfieldStats)data;
                        break;


                    case CmdId.Event_Playfield_Entity_List:
                        //Triggered by API mod request GameAPI.Game_Request(CmdId.Request_Playfield_Entity_List, (ushort)CurrentSeqNr, new PString( [Playfield Name] ));
                        PlayfieldEntityList Received_PlayfieldEntityList = (PlayfieldEntityList)data;
                        break;


                    case CmdId.Event_Dedi_Stats:
                        //Triggered by API mod request GameAPI.Game_Request(CmdId.Request_Dedi_Stats, (ushort)CurrentSeqNr, null));
                        DediStats Received_DediStats = (DediStats)data;
                        break;


                    case CmdId.Event_GlobalStructure_List:
                        //Triggered by API mod request GameAPI.Game_Request(CmdId.Request_GlobalStructure_List, (ushort)CurrentSeqNr, null));
                        //Triggered by API mod request GameAPI.Game_Request(CmdId.Request_GlobalStructure_Update, (ushort)CurrentSeqNr, new PString( [Playfield Name] ));
                        GlobalStructureList Received_GlobalStructureList = (GlobalStructureList)data;
                        break;


                    case CmdId.Event_Entity_PosAndRot:
                        //Triggered by API mod request GameAPI.Game_Request(CmdId.Request_Entity_PosAndRot, (ushort)CurrentSeqNr, new Id( [EntityID] ));
                        IdPositionRotation Received_EntityPosRot = (IdPositionRotation)data;
                        break;


                    case CmdId.Event_Get_Factions:
                        //Triggered by API mod request GameAPI.Game_Request(CmdId.Request_Get_Factions, (ushort)CurrentSeqNr, new Id( [int] )); //Requests all factions from a certain Id onwards. If you want all factions use Id 1.
                        FactionInfoList Received_FactionInfoList = (FactionInfoList)data;
                        break;


                    case CmdId.Event_NewEntityId:
                        //Triggered by API mod request GameAPI.Game_Request(CmdId.Request_NewEntityId, (ushort)CurrentSeqNr, null));
                        Id Request_NewEntityId = (Id)data;
                        break;

                    //|||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||
                    case CmdId.Event_Structure_BlockStatistics:
                        //Triggered by API mod request GameAPI.Game_Request(CmdId.Request_Structure_BlockStatistics, (ushort)CurrentSeqNr, new Id( [EntityID] ));
                        IdStructureBlockInfo Received_StructureBlockStatistics = (IdStructureBlockInfo)data;
                        if (SeqNrStorage.Keys.Contains(seqNr))
                        {
                            Storage.StorableData RetrievedData = SeqNrStorage[seqNr];
                            if (RetrievedData.Requested == "BlockStatistics" && RetrievedData.function == "Recycle" && Convert.ToString(Received_StructureBlockStatistics.id) == RetrievedData.Match)
                            {
                                SeqNrStorage.Remove(seqNr);
                                if (debug)
                                {
                                    foreach (KeyValuePair<int, int> WriteStack in Received_StructureBlockStatistics.blockStatistics)
                                    {
                                        CommonFunctions.Debug(WriteStack.Key + " X " + WriteStack.Value);
                                    }
                                }
                                Dictionary<int, int> result1 = Received_StructureBlockStatistics.blockStatistics;
                                string EType = RetrievedData.EntityData.Type;
                                Dictionary<int, int> TempDictionary = new Dictionary<int, int> { };
                                if (EType == "BA")
                                {
                                    foreach (int EntityItemID in result1.Keys)
                                    {
                                        if (BaseDictionary.Keys.Contains(EntityItemID))
                                        {
                                            if (TempDictionary.Keys.Contains(BaseDictionary[EntityItemID]))
                                            {
                                                TempDictionary[BaseDictionary[EntityItemID]] = TempDictionary[BaseDictionary[EntityItemID]] + result1[EntityItemID];
                                            }
                                            else
                                            {
                                                TempDictionary.Add(BaseDictionary[EntityItemID], result1[EntityItemID]);
                                            }
                                        }
                                    }
                                }
                                else if (EType == "CV")
                                {
                                    foreach (int EntityItemID in result1.Keys)
                                    {
                                        if (CVDictionary.Keys.Contains(EntityItemID))
                                        {
                                            if (TempDictionary.Keys.Contains(CVDictionary[EntityItemID]))
                                            {
                                                TempDictionary[CVDictionary[EntityItemID]] = TempDictionary[CVDictionary[EntityItemID]] + result1[EntityItemID];
                                            }
                                            else
                                            {
                                                TempDictionary.Add(CVDictionary[EntityItemID], result1[EntityItemID]);
                                            }
                                        }
                                    }
                                }
                                else if (EType == "SV")
                                {
                                    foreach (int EntityItemID in result1.Keys)
                                    {
                                        if (SVDictionary.Keys.Contains(EntityItemID))
                                        {
                                            if (TempDictionary.Keys.Contains(SVDictionary[EntityItemID]))
                                            {
                                                TempDictionary[SVDictionary[EntityItemID]] = TempDictionary[SVDictionary[EntityItemID]] + result1[EntityItemID];
                                            }
                                            else
                                            {
                                                TempDictionary.Add(SVDictionary[EntityItemID], result1[EntityItemID]);
                                            }
                                        }
                                    }
                                }
                                else if (EType == "HV")
                                {
                                    foreach (int EntityItemID in result1.Keys)
                                    {
                                        if (HVDictionary.Keys.Contains(EntityItemID))
                                        {
                                            if (TempDictionary.Keys.Contains(HVDictionary[EntityItemID]))
                                            {
                                                TempDictionary[HVDictionary[EntityItemID]] = TempDictionary[HVDictionary[EntityItemID]] + result1[EntityItemID];
                                            }
                                            else
                                            {
                                                TempDictionary.Add(HVDictionary[EntityItemID], result1[EntityItemID]);
                                            }
                                        }
                                    }
                                }
                                IOrderedEnumerable<KeyValuePair<int, int>> SortedDictionary = TempDictionary.OrderByDescending(a => a.Value).ThenBy(a => a.Key);
                                List<ItemStack> ListItemStacks = new List<ItemStack> { };
                                int ItemCount = 0;
                                foreach (KeyValuePair<int, int> ListedItem in SortedDictionary)
                                {
                                    ItemStack NewItem = new ItemStack
                                    {
                                        slotIdx = Convert.ToByte(ItemCount),
                                        id = ListedItem.Key,
                                        count = ListedItem.Value,
                                        ammo = 0,
                                        decay = 0
                                    };
                                    ItemCount++;
                                    ListItemStacks.Add(NewItem);
                                }
                                //ReturnType: 1 = Window with items, excess is deleted
                                if (SetupYamlData.ReturnType == 1)
                                {
                                    ItemStack[] ItemsArray = ListItemStacks.ToArray();
                                    API.OpenItemExchange(RetrievedData.SpeakerID, SetupYamlData.RecycleCommand + " " + Received_StructureBlockStatistics.id, "Anything left here gets destroyed on Close", "Close", ItemsArray, RetrievedData);
                                }
                                //ReturnType: 2 = Window with items, excess is dumped into memory, use /rc (RecycleContinue) to get more back (WARNING: Exploitable!!!)
                                else if (SetupYamlData.ReturnType == 2)
                                {
                                    //WIP
                                }
                                //ReturnType: 3 = Direct to inventory, excess is deleted
                                else if (SetupYamlData.ReturnType == 3 || SetupYamlData.ReturnType == 4)
                                {

                                    RetrievedData.function = "Recycle";
                                    RetrievedData.Match = Convert.ToString(RetrievedData.SpeakerID);
                                    RetrievedData.Requested = "PlayerInventory";
                                    CommonFunctions.Log(RetrievedData.SpeakerID + " Recycled " + Received_StructureBlockStatistics.id);
                                    API.InventoryGet(RetrievedData.SpeakerID, RetrievedData);
                                    ExtraItems.Add(RetrievedData.SpeakerID, ListItemStacks);
                                }
                                //ReturnType: 4 = Direct to inventory, excess is dumped to memory, use /rc (RecycleContinue) to get more back
                                if (debug)
                                {
                                    File.WriteAllText(MyEmpyrionMod.ModPath + "test.txt", "");
                                    ItemCount = 0;
                                    foreach (ItemStack Stack in ListItemStacks)
                                    {
                                        ItemCount++;
                                        File.AppendAllText(MyEmpyrionMod.ModPath + "test.txt", Stack.id + "x" + Stack.count + Environment.NewLine);
                                    }
                                }
                            }
                        }
                        break;


                    case CmdId.Event_AlliancesAll:
                        //Triggered by API mod request GameAPI.Game_Request(CmdId.Request_AlliancesAll, (ushort)CurrentSeqNr, null));
                        AlliancesTable Received_AlliancesAll = (AlliancesTable)data;
                        break;


                    case CmdId.Event_AlliancesFaction:
                        //Triggered by API mod request GameAPI.Game_Request(CmdId.Request_AlliancesFaction, (ushort)CurrentSeqNr, new AlliancesFaction( [int nFaction1Id], [int nFaction2Id], [bool nIsAllied] ));
                        AlliancesFaction Received_AlliancesFaction = (AlliancesFaction)data;
                        break;


                    case CmdId.Event_BannedPlayers:
                        //Triggered by API mod request GameAPI.Game_Request(CmdId.Request_GetBannedPlayers, (ushort)CurrentSeqNr, null ));
                        BannedPlayerData Received_BannedPlayers = (BannedPlayerData)data;
                        break;


                    case CmdId.Event_GameEvent:
                        //Triggered by PDA Events
                        GameEventData Received_GameEvent = (GameEventData)data;
                        break;


                    case CmdId.Event_Ok:
                        //Triggered by API mod request GameAPI.Game_Request(CmdId.Request_Player_SetInventory, (ushort)CurrentSeqNr, new Inventory(){ [changes to be made] });
                        //Triggered by API mod request GameAPI.Game_Request(CmdId.Request_Player_AddItem, (ushort)CurrentSeqNr, new IdItemStack(){ [changes to be made] });
                        //Triggered by API mod request GameAPI.Game_Request(CmdId.Request_Player_SetCredits, (ushort)CurrentSeqNr, new IdCredits( [PlayerID], [Double] ));
                        //Triggered by API mod request GameAPI.Game_Request(CmdId.Request_Player_AddCredits, (ushort)CurrentSeqNr, new IdCredits( [PlayerID], [+/- Double] ));
                        //Triggered by API mod request GameAPI.Game_Request(CmdId.Request_Blueprint_Finish, (ushort)CurrentSeqNr, new Id( [PlayerID] ));
                        //Triggered by API mod request GameAPI.Game_Request(CmdId.Request_Blueprint_Resources, (ushort)CurrentSeqNr, new BlueprintResources( [PlayerID], [List<ItemStack>], [bool ReplaceExisting?] ));
                        //Triggered by API mod request GameAPI.Game_Request(CmdId.Request_Entity_Teleport, (ushort)CurrentSeqNr, new IdPositionRotation( [EntityId OR PlayerID], [Pvector3 Position], [Pvector3 Rotation] ));
                        //Triggered by API mod request GameAPI.Game_Request(CmdId.Request_Entity_ChangePlayfield , (ushort)CurrentSeqNr, new IdPlayfieldPositionRotation( [EntityId OR PlayerID], [Playfield],  [Pvector3 Position], [Pvector3 Rotation] ));
                        //Triggered by API mod request GameAPI.Game_Request(CmdId.Request_Entity_Destroy, (ushort)CurrentSeqNr, new Id( [EntityID] ));
                        //Triggered by API mod request GameAPI.Game_Request(CmdId.Request_Entity_Destroy2, (ushort)CurrentSeqNr, new IdPlayfield( [EntityID], [Playfield] ));
                        //Triggered by API mod request GameAPI.Game_Request(CmdId.Request_Entity_SetName, (ushort)CurrentSeqNr, new Id( [EntityID] )); Wait, what? This one doesn't make sense. This is what the Wiki says though.
                        //Triggered by API mod request GameAPI.Game_Request(CmdId.Request_Entity_Spawn, (ushort)CurrentSeqNr, new EntitySpawnInfo()); Doesn't make sense to me.
                        //Triggered by API mod request GameAPI.Game_Request(CmdId.Request_Structure_Touch, (ushort)CurrentSeqNr, new Id( [EntityID] ));
                        //Triggered by API mod request GameAPI.Game_Request(CmdId.Request_InGameMessage_SinglePlayer, (ushort)CurrentSeqNr, new IdMsgPrio( [int nId], [string nMsg], [byte nPrio], [float nTime] )); //for Prio: 0=Red, 1=Yellow, 2=Blue
                        //Triggered by API mod request GameAPI.Game_Request(CmdId.Request_InGameMessage_Faction, (ushort)CurrentSeqNr, new IdMsgPrio( [int nId], [string nMsg], [byte nPrio], [float nTime] )); //for Prio: 0=Red, 1=Yellow, 2=Blue
                        //Triggered by API mod request GameAPI.Game_Request(CmdId.Request_InGameMessage_AllPlayers, (ushort)CurrentSeqNr, new IdMsgPrio( [int nId], [string nMsg], [byte nPrio], [float nTime] )); //for Prio: 0=Red, 1=Yellow, 2=Blue
                        //Triggered by API mod request GameAPI.Game_Request(CmdId.Request_ConsoleCommand, (ushort)CurrentSeqNr, new PString( [Telnet Command] ));

                        //uh? Not Listed in Wiki... Received_ = ()data;
                        break;


                    case CmdId.Event_Error:
                        //Triggered when there is an error coming from the API
                        ErrorInfo Received_ErrorInfo = (ErrorInfo)data;
                        break;


                    case CmdId.Event_PdaStateChange:
                        //Triggered by PDA: chapter activated/deactivated/completed
                        PdaStateInfo Received_PdaStateChange = (PdaStateInfo)data;
                        break;


                    case CmdId.Event_ConsoleCommand:
                        //Triggered when a player uses a Console Command in-game
                        ConsoleCommandInfo Received_ConsoleCommandInfo = (ConsoleCommandInfo)data;
                        break;


                    default:
                        break;
                }
            }
            catch (Exception ex)
            {
                CommonFunctions.ERROR("\r\nProcess: " + LogName);
                CommonFunctions.ERROR("Message: " + ex.Message);
                CommonFunctions.ERROR("Data: " + ex.Data);
                CommonFunctions.ERROR("HelpLink: " + ex.HelpLink);
                CommonFunctions.ERROR("InnerException: " + ex.InnerException);
                CommonFunctions.ERROR("Source: " + ex.Source);
                CommonFunctions.ERROR("StackTrace: " + ex.StackTrace);
                CommonFunctions.ERROR("TargetSite: " + ex.TargetSite);
                CommonFunctions.ERROR("");
            }

        }

        private void OnTextWindowClose(int buttonIdx, string linkId, string inputContent, int playerId, int customValue)
        {
            //***
            if(buttonIdx == 0 && API2OnCloseTextbox.ContainsKey(customValue))
            {
                Storage.StorableData RetrievedData = API2OnCloseTextbox[customValue];
                if (RetrievedData.EntityData.BelongsTo == playerId)
                {

                    RetrievedData.function = "Recycle";
                    RetrievedData.Match = Convert.ToString(RetrievedData.EntityData.Id);
                    RetrievedData.Requested = "BlockStatistics";
                    API.BlockStatistics(Convert.ToInt32(RetrievedData.EntityData.Id), RetrievedData);
                    API.Delete(Convert.ToInt32(RetrievedData.EntityData.Id));
                    CommonFunctions.Log(RetrievedData.SpeakerID + " Recycled " + RetrievedData.EntityData.Id);
                    API.ServerTell(RetrievedData.SpeakerID, ModShortName, "Success for Entity #" + RetrievedData.EntityData.Id, true);
                }
            }
            else if(buttonIdx == 1 && API2OnCloseTextbox.ContainsKey(customValue))
            {
                API.ServerTell(playerId, ModShortName, "Canceled for Entity #" + customValue, true);
            }
            else
            {
                API.ServerTell(playerId, ModShortName, "Weird Error thing happened", true);
                CommonFunctions.ERROR("ERROR: On Text Window Close (Player=" + playerId + "EntityID=" + customValue + " button=" + buttonIdx);
            }
        }

        public void Game_Update()
        {
            //Triggered whenever Empyrion experiences "Downtime", roughly 75-100 times per second
        }

        public void Game_Exit()
        {
            //Triggered when the server is Shutting down. Does NOT pause the shutdown.
        }

        public void Init(IModApi modAPI)
        {
            if (Expiration < int.Parse(CommonFunctions.UnixTimeStamp()))
            {
                Disable = true;
            }
            BootupTimestamp = CommonFunctions.TimeStampFilename();
            modApi = modAPI;
            ModPath = modApi.Application.GetPathFor(AppFolder.Mod) + "\\" + ModShortName + "r\\";
            if (LogName != "unknown")
            {
                if (File.Exists(ModPath + "ERROR.txt")) { File.Delete(ModPath + "ERROR.txt"); }
                if (File.Exists(ModPath + "debug.txt")) { File.Delete(ModPath + "debug.txt"); }
            }
            SaveGameFolder = modApi.Application.GetPathFor(AppFolder.SaveGame);
            SaveGameName = SaveGameFolder.Split('/').Last();
            CommonFunctions.Log("SaveGameFolder = " + SaveGameFolder);
            CommonFunctions.Log("SaveGameName = " + SaveGameName);
            if (modApi.Application.Mode == ApplicationMode.DedicatedServer)
            {
                LogName = "Dedi ";
                CommonFunctions.Log("--------------------" + BootupTimestamp + "----------------------------");
                if (!Directory.Exists(SaveGameFolder + "\\ModData\\"))
                {
                    Directory.CreateDirectory(SaveGameFolder + "\\ModData\\");
                    Directory.CreateDirectory(SaveGameFolder + "\\ModData\\" + ModShortName + "r\\");
                }
                else if (!Directory.Exists(SaveGameFolder + "\\ModData\\" + ModShortName + "r\\"))
                {
                    Directory.CreateDirectory(SaveGameFolder + "\\ModData\\" + ModShortName + "r\\");
                }
                bool SetupComplete = SetupYaml.Setup();
                if (SetupComplete)
                {
                    CommonFunctions.Log("Init Success");
                }
                else
                {
                    CommonFunctions.Log("Init Failed");
                    Disable = true;
                }

                try
                {
                    modApi.Network.RegisterReceiverForPlayfieldPackets(NetworkReceiverDedicated);
                }
                catch
                {
                    CommonFunctions.ERROR("ERROR: when Registering Receiver: NetworkReceiverDedicated ---> FAIL");
                }
            }
            else if (modApi.Application.Mode == ApplicationMode.PlayfieldServer)
            {
                LogName = "PfServer";
                //CommonFunctions.Log("--------------------" + BootupTimestamp + "----------------------------");
                try
                {
                    modApi.Application.OnPlayfieldLoaded += Application_OnPlayfieldLoaded;
                }
                catch
                {
                    CommonFunctions.ERROR("ERROR: when adding Listener Application_OnPlayfieldLoaded ---> FAIL");
                }
                //SaveGameName = modApi.Application.GetPathFor(AppFolder.SaveGame) + "\\ModData\\" + ModShortName + "r\\";

            }
        }

        private void Application_OnPlayfieldLoaded(IPlayfield playfield)
        {
            //store IPlayfield
            Playfield = playfield;
            LogName = Playfield.Name;
            BootupTimestamp = CommonFunctions.TimeStampFilename();
            CommonFunctions.Log("--------------------" + BootupTimestamp + "----------------------------");
            byte[] SendableData = CommonFunctions.ConvertToByteArray("PlayfieldLoaded");
            modApi.Network.SendToDedicatedServer("Recycler", SendableData, playfield.Name);
            try
            {
                modApi.Network.RegisterReceiverForDediPackets(NetworkReceiverOnPlayfield);
            }
            catch
            {
                CommonFunctions.ERROR("ERROR: when Registering Receiver NetworkReceiverOnPlayfield ---> FAIL");
            }
            try
            {
                modApi.Application.OnPlayfieldUnloading -= Application_OnPlayfieldUnloading;
            }
            catch
            {
                CommonFunctions.ERROR("ERROR: when Removing Listener Application_OnPlayfieldUnloading ---> FAIL");
            }
            playfield.OnEntityLoaded += Playfield_OnEntityLoaded;
        }

        private void Application_OnPlayfieldUnloading(IPlayfield playfield)
        {
            Playfield = null;
            LogName = "No Playfield Loaded";
            byte[] SendableData = CommonFunctions.ConvertToByteArray("PlayfieldUnloaded");
            modApi.Network.SendToDedicatedServer("Recycler", SendableData, playfield.Name);
        }

        private void Playfield_OnEntityLoaded(IEntity entity)
        {
            if (entity.Type == EntityType.BA || entity.Type == EntityType.CV || entity.Type == EntityType.SV || entity.Type == EntityType.HV)
            {
                try
                {
                    byte[] SendableData = CommonFunctions.ConvertToByteArray("EntityLoaded " + entity.Id);
                    modApi.Network.SendToDedicatedServer("Recycler", SendableData, Playfield.Name);
                    CommonFunctions.Log("Sending Updated Entity => Playfield data. " + entity.Id + " is on " + Playfield.Name);
                }
                catch
                {
                    CommonFunctions.Log("ERROR: Unable to SendToDedicatedServer 'EntityLoaded " + entity.Id + "'");

                }
                try
                {
                    Interconnectivity.EntityData EntDat = new Interconnectivity.EntityData
                    {
                        /*
                        BelongsTo = entity.BelongsTo,
                        DockedTo = entity.DockedTo,
                        FactionGroup = entity.Faction.Group.ToString(),
                        FactionID = entity.Faction.Id,
                        Id = entity.Id,
                        IsLocal = entity.IsLocal,
                        IsPoi = entity.IsPoi,
                        IsProxy = entity.IsProxy,
                        Name = entity.Name,
                        Playfield = Playfield.Name,
                        Type = entity.Type.ToString(),
                        X = Convert.ToInt16(Math.Round(entity.Position.x)),
                        Y = Convert.ToInt16(Math.Round(entity.Position.y)),
                        Z = Convert.ToInt16(Math.Round(entity.Position.z))
                        */
                    };
                    try { EntDat.BelongsTo = entity.BelongsTo; } catch { }
                    try { EntDat.DockedTo = entity.DockedTo; } catch { }
                    try { EntDat.FactionGroup = entity.Faction.Group.ToString(); } catch { }
                    try { EntDat.FactionID = entity.Faction.Id; } catch { }
                    try { EntDat.Id = entity.Id; } catch { }
                    try { EntDat.IsLocal = entity.IsLocal; } catch { }
                    try { EntDat.IsPoi = entity.IsPoi; } catch { }
                    try { EntDat.IsProxy = entity.IsProxy; } catch { }
                    try { EntDat.Name = entity.Name; } catch { }
                    try { EntDat.Playfield = Playfield.Name; } catch { }
                    try { EntDat.Type = entity.Type.ToString(); } catch { }
                    try { EntDat.X = Convert.ToInt16(Math.Round(entity.Position.x)); } catch { }
                    try { EntDat.Y = Convert.ToInt16(Math.Round(entity.Position.y)); } catch { }
                    try { EntDat.Z = Convert.ToInt16(Math.Round(entity.Position.z)); } catch { }
                    Interconnectivity.WriteYaml(SaveGameFolder + "\\ModData\\Recycler\\" + entity.Id + ".yaml", EntDat);
                }
                catch
                {
                    CommonFunctions.Log("ERROR: Unable to write yaml " + SaveGameFolder + "\\ModData\\Recycler\\" + entity.Id + ".yaml");
                }
            }
        }

        private void NetworkReceiverOnPlayfield(string sender, string playfieldName, byte[] data)
        {
            //This should be happening on the Playfield process
            //CommonFunctions.Log("Triggered: NetworkReceiverOnPlayfield");
            if (sender == "Recycler")
            {
                int EntityID = 0;
                int PlayerID = 0;
                string ReceivedData = CommonFunctions.ConvertByteArrayToString(data);
                if (ReceivedData.Contains(' '))
                {
                    try
                    {
                        string[] SplitReceivedData = ReceivedData.Split(' ');
                        if (ReceivedData.StartsWith("RecycleableTest"))
                        {
                            string FailReason = "";
                            try
                            {
                                EntityID = int.Parse(SplitReceivedData[1]);
                                PlayerID = int.Parse(SplitReceivedData[2]);
                            }
                            catch
                            {
                                CommonFunctions.ERROR("ERROR: Unable to parse " + ReceivedData + " into an Int32");
                            }
                            if (Playfield.Entities.ContainsKey(EntityID))
                            {
                                IEntity entity = Playfield.Entities[EntityID];
                                string eType = "";
                                bool Recycleable = false;
                                if (entity.Type == EntityType.BA)
                                {
                                    eType = "BA";
                                    Recycleable = true;
                                }
                                else if (entity.Type == EntityType.CV)
                                {
                                    eType = "CV";
                                    Recycleable = true;
                                }
                                else if (entity.Type == EntityType.SV)
                                {
                                    eType = "SV";
                                    Recycleable = true;
                                }
                                else if (entity.Type == EntityType.HV)
                                {
                                    eType = "HV";
                                    Recycleable = true;
                                }
                                else
                                {
                                    eType = "Other";
                                    FailReason = FailReason + "WrongEntityType ";
                                }
                                string FGroup = "";
                                if (entity.Faction.Group == FactionGroup.Player)
                                {
                                    FGroup = "Player";
                                }
                                else
                                {
                                    FGroup = "Other";
                                    FailReason = FailReason + "NotPrivate ";
                                    Recycleable = false;
                                }
                                List<int> Carrying = new List<int> { };
                                Interconnectivity.EntityData WriteableData = new Interconnectivity.EntityData
                                {
                                    BelongsTo = entity.BelongsTo,
                                    FactionGroup = FGroup,
                                    FactionID = entity.Faction.Id,
                                    IsLocal = entity.IsLocal,
                                    IsPoi = entity.IsPoi,
                                    IsProxy = entity.IsProxy,
                                    Name = entity.Name,
                                    Playfield = Playfield.Name,
                                    Type = eType,
                                    Id = entity.Id,
                                    X = Convert.ToInt32(Math.Round(entity.Position.x, 0)),
                                    Y = Convert.ToInt32(Math.Round(entity.Position.y, 0)),
                                    Z = Convert.ToInt32(Math.Round(entity.Position.z, 0)),
                                    Carrying = Carrying,
                                    TriangleCount = entity.Structure.TriangleCount,
                                    LightCount = entity.Structure.LightCount,
                                    BlockCount = entity.Structure.BlockCount,
                                    DeviceCount = entity.Structure.DeviceCount,
                                    Health = entity.Structure.DamageLevel,
                                    CoreType = entity.Structure.CoreType.ToString(),
                                    HasLandclaimDevice = entity.Structure.HasLandClaimDevice,
                                    IsOfflineProtectable = entity.Structure.IsOfflineProtectable,
                                    IsPowered = entity.Structure.IsPowered,
                                    IsReady = entity.Structure.IsReady,
                                    IsShieldActive = entity.Structure.IsShieldActive,
                                    Creator = entity.Structure.PlayerCreatedSteamId,
                                    SizeClass = entity.Structure.SizeClass,
                                    TotalMass = entity.Structure.TotalMass
                                };
                                try { WriteableData.DockedTo = entity.DockedTo; } catch { WriteableData.DockedTo = 0; }
                                try { WriteableData.Pilot = entity.Structure.Pilot.Id; } catch { WriteableData.Pilot = 0; }

                                if (Recycleable)
                                {
                                    if (entity.BelongsTo != PlayerID)
                                    {
                                        Recycleable = false;
                                        FailReason = FailReason + "NotOwnedByYou ";
                                    }
                                    if (entity.IsProxy != false)
                                    {
                                        Recycleable = false;
                                        FailReason = FailReason + "Proxy ";
                                    }
                                    if (entity.IsPoi != false)
                                    {
                                        Recycleable = false;
                                        FailReason = FailReason + "isPOI ";
                                    }
                                    if (entity.IsLocal == false)
                                    {
                                        Recycleable = false;
                                        FailReason = FailReason + "isNotLocal ";
                                    }
                                    if (entity.Structure.CoreType.ToString() != "Player")
                                    {
                                        Recycleable = false;
                                        FailReason = FailReason + "NoPlayerCore ";
                                    }
                                    try
                                    {
                                        if (entity.DockedTo != 0)
                                        {
                                            Recycleable = false;
                                            FailReason = FailReason + "Docked ";
                                        }
                                    } catch { }
                                }
                                Interconnectivity.WriteYaml(SaveGameFolder + "\\ModData\\Recycler\\" + entity.Id + ".yaml", WriteableData);
                                if (Recycleable)
                                {
                                    byte[] Message = CommonFunctions.ConvertToByteArray("RecycleableTest " + PlayerID + " " + entity.Id + " true " + CommonFunctions.SanitizeString(entity.Name));
                                    modApi.Network.SendToDedicatedServer("Recycler", Message, Playfield.Name);
                                }
                                else
                                {
                                    byte[] Message = CommonFunctions.ConvertToByteArray("RecycleableTest " + PlayerID + " " + entity.Id + " false " + FailReason);
                                    modApi.Network.SendToDedicatedServer("Recycler", Message, Playfield.Name);
                                }
                            }
                            else
                            {
                                byte[] Message = CommonFunctions.ConvertToByteArray("RecycleableTest " + PlayerID + " " + EntityID + " false " + "EntityNotOnThisPlayfield " + Playfield.Name);
                                modApi.Network.SendToDedicatedServer("Recycler", Message, Playfield.Name);
                            }
                        }
                    }
                    catch(Exception ex)
                    {
                        CommonFunctions.ERROR("\r\nProcess: " + LogName);
                        CommonFunctions.ERROR("Message: " + ex.Message);
                        CommonFunctions.ERROR("Data: " + ex.Data);
                        CommonFunctions.ERROR("HelpLink: " + ex.HelpLink);
                        CommonFunctions.ERROR("InnerException: " + ex.InnerException);
                        CommonFunctions.ERROR("Source: " + ex.Source);
                        CommonFunctions.ERROR("StackTrace: " + ex.StackTrace);
                        CommonFunctions.ERROR("TargetSite: " + ex.TargetSite);
                        CommonFunctions.ERROR("");
                    }
                }
                else
                {
                    try
                    {
                        EntityID = int.Parse(ReceivedData);
                    }
                    catch
                    {
                        CommonFunctions.ERROR("ERROR: Unable to parse " + ReceivedData + " into an Int32");
                    }
                    if (EntityID != 0)
                    {
                        //CommonFunctions.Log("D2P: Looking up " + EntityID);
                        IEntity entity = Playfield.Entities[EntityID];
                        List<int> Carrying = new List<int> { };
                        foreach (IEntity DockableEntity in entity.Structure.GetDockedVessels())
                        {
                            Carrying.Add(DockableEntity.Id);
                        }
                        string eType = "";
                        if (entity.Type == EntityType.BA)
                        {
                            eType = "BA";
                        }
                        else if (entity.Type == EntityType.CV)
                        {
                            eType = "CV";
                        }
                        else if (entity.Type == EntityType.SV)
                        {
                            eType = "SV";
                        }
                        else if (entity.Type == EntityType.HV)
                        {
                            eType = "HV";
                        }
                        else
                        {
                            eType = "Other";
                        }
                        string FGroup = "";
                        if (entity.Faction.Group == FactionGroup.Player)
                        {
                            FGroup = "Player";
                        }
                        else
                        {
                            FGroup = "Other";
                        }
                        //CommonFunctions.Log("Begin building Interconnectivity.EntityData");
                        Interconnectivity.EntityData WriteableData = new Interconnectivity.EntityData
                        {
                            BelongsTo = entity.BelongsTo,
                            FactionGroup = FGroup,
                            FactionID = entity.Faction.Id,
                            IsLocal = entity.IsLocal,
                            IsPoi = entity.IsPoi,
                            IsProxy = entity.IsProxy,
                            Name = entity.Name,
                            Playfield = Playfield.Name,
                            Type = eType,
                            Id = entity.Id,
                            X = Convert.ToInt32(Math.Round(entity.Position.x, 0)),
                            Y = Convert.ToInt32(Math.Round(entity.Position.y, 0)),
                            Z = Convert.ToInt32(Math.Round(entity.Position.z, 0)),
                            Carrying = Carrying,
                            TriangleCount = entity.Structure.TriangleCount,
                            LightCount = entity.Structure.LightCount,
                            BlockCount = entity.Structure.BlockCount,
                            DeviceCount = entity.Structure.DeviceCount,
                            Health = entity.Structure.DamageLevel,
                            CoreType = entity.Structure.CoreType.ToString(),
                            HasLandclaimDevice = entity.Structure.HasLandClaimDevice,
                            IsOfflineProtectable = entity.Structure.IsOfflineProtectable,
                            IsPowered = entity.Structure.IsPowered,
                            IsReady = entity.Structure.IsReady,
                            IsShieldActive = entity.Structure.IsShieldActive,
                            Creator = entity.Structure.PlayerCreatedSteamId,
                            SizeClass = entity.Structure.SizeClass,
                            TotalMass = entity.Structure.TotalMass
                        };
                        try { WriteableData.DockedTo = entity.DockedTo; } catch { WriteableData.DockedTo = 0; }
                        try { WriteableData.Pilot = entity.Structure.Pilot.Id; } catch { WriteableData.Pilot = 0; }

                        Interconnectivity.WriteYaml(SaveGameFolder + "\\ModData\\Recycler\\" + entity.Id + ".yaml", WriteableData);
                        //CommonFunctions.Log("P2D: Sending data back to Dedi Server (" + EntityID + ")");
                        modApi.Network.SendToDedicatedServer("Recycler", data, Playfield.Name);
                        //CommonFunctions.Log("Sent message to Dedi Server");
                    }
                    else
                    {
                        CommonFunctions.ERROR("ERROR: Invalid sender received... please tell me this is not normal. " + sender);
                    }
                }
            }
        }

        private void NetworkReceiverDedicated(string sender, string playfieldName, byte[] data)
        {
            //This should be happening on the Dedi process
            if (sender == "Recycler")
            {
                string ReceivedData = CommonFunctions.ConvertByteArrayToString(data);
                if (ReceivedData.Contains(' '))
                {
                    string[] SplitRD = ReceivedData.Split(' ');
                    if (SplitRD[0] == "EntityLoaded")
                    {
                        EntityPlayfield[int.Parse(SplitRD[1])] = playfieldName;
                        CommonFunctions.Log("Entity " + SplitRD[1] + " registered on " + playfieldName);
                    }
                    else if (SplitRD[0] == "RecycleableTest")
                    {
                        int EntityID = int.Parse(SplitRD[2]);
                        int PlayerID = int.Parse(SplitRD[1]);
                        string EntityName = CommonFunctions.ChatmessageHandler(SplitRD, "4*");
                        if (SplitRD[3] == "true")
                        {
                            if (File.Exists(SaveGameFolder + "\\ModData\\Recycler\\" + EntityID + ".yaml"))
                            {
                                Interconnectivity.EntityData EntityData = new Interconnectivity.EntityData { };
                                bool fail = false;
                                try
                                {
                                    EntityData = Interconnectivity.ReadYaml(SaveGameFolder + "\\ModData\\Recycler\\" + EntityID + ".yaml");
                                    try { File.Delete(SaveGameFolder + "\\ModData\\Recycler\\" + EntityID + ".yaml"); } catch { }
                                }
                                catch
                                {
                                    fail = true;
                                    CommonFunctions.ERROR("ERROR: unable to read EntityData yaml (" + SaveGameFolder + "\\ModData\\Recycler\\" + EntityID + ".yaml)");
                                }
                                if (!fail && EntitiesBeingRecycled.Keys.Contains(EntityID))
                                {
                                    //***
                                    //API.ServerTell(PlayerID, ModShortName, EntityID + " is a valid target, unfortunately I havent finished coding this section yet.", true);
                                    Storage.StorableData StorableData = new Storage.StorableData
                                    {
                                        function = "Recycle",
                                        Match = Convert.ToString(PlayerID),
                                        Requested = "PlayerInfo",
                                        SpeakerID = PlayerID,
                                        EntityData = EntityData
                                    };
                                    API.PlayerInfo(PlayerID, StorableData);

                                }
                            }
                        }
                        else
                        {
                            string reason = CommonFunctions.ChatmessageHandler(SplitRD, "4*");
                            API.ServerTell(PlayerID, ModShortName, EntityID + " is not recycleable (" + reason + ")", true);
                        }
                    }
                }
                else if (ReceivedData == "PlayfieldLoaded")
                {
                    try
                    {
                        if (!LoadedPlayfields.Contains(playfieldName))
                        {
                            LoadedPlayfields.Add(playfieldName);
                            CommonFunctions.Log("Playfield added to LoadedPlayfields using API2 method. " + playfieldName);
                        }
                    }
                    catch
                    {
                        CommonFunctions.Log("ERROR: unable to add " + playfieldName + " to the list of loaded playfields.");
                    }
                }
                else if (ReceivedData == "PlayfieldUnloaded")
                {
                    try
                    {
                        LoadedPlayfields.Remove(playfieldName);
                        CommonFunctions.Log("Playfield Unloaded " + playfieldName);
                    }
                    catch
                    {
                        CommonFunctions.Log("ERROR: unable to Remove " + playfieldName + " from the list of loaded playfields.");
                    }

                }
                else
                {
                    int ReceivedEntityID = 0;
                    try
                    {
                        ReceivedEntityID = int.Parse(CommonFunctions.ConvertByteArrayToString(data));
                    }
                    catch
                    {
                        CommonFunctions.ERROR("ERROR: Unable to Parse EntityID from " + CommonFunctions.ConvertByteArrayToString(data));
                    }
                    if (ReceivedEntityID != 0)
                    {
                        //CommonFunctions.Log("P2D: Received " + ReceivedEntityID);
                        if (File.Exists(SaveGameFolder + "\\ModData\\Recycler\\" + ReceivedEntityID + ".yaml"))
                        {
                            Interconnectivity.EntityData EntityData = new Interconnectivity.EntityData { };
                            bool fail = false;
                            try
                            {
                                EntityData = Interconnectivity.ReadYaml(SaveGameFolder + "\\ModData\\Recycler\\" + ReceivedEntityID + ".yaml");
                            }
                            catch
                            {
                                fail = true;
                                CommonFunctions.ERROR("ERROR: unable to read EntityData yaml (" + SaveGameFolder + "\\ModData\\Recycler\\" + ReceivedEntityID + ".yaml)");
                            }
                            if (!fail && EntitiesBeingRecycled.Keys.Contains(ReceivedEntityID))
                            {
                                Interconnectivity.StorableData RetrievedData = EntitiesBeingRecycled[ReceivedEntityID];
                                //CommonFunctions.Log("RetrievedData " + RetrievedData.SpeakerID);
                                //Interconnectivity.StorableData StoredEntityRecycleData = EntitiesBeingRecycled[ReceivedEntityID];
                                if (debug)
                                {
                                    API.ServerTell(RetrievedData.SpeakerID, ModShortName, "", true);
                                    API.ServerTell(RetrievedData.SpeakerID, ModShortName, "EntityID = " + EntityData.Id + " Needs to be " + ReceivedEntityID + " (Just double checking data received is data requested)", false);
                                    API.ServerTell(RetrievedData.SpeakerID, ModShortName, "Proxy = " + EntityData.IsProxy + " Needs to be false", false);
                                    API.ServerTell(RetrievedData.SpeakerID, ModShortName, "Removed = " + EntityData.IsRemoved + " Needs to be false (Entity must Exist)", false);
                                    API.ServerTell(RetrievedData.SpeakerID, ModShortName, "DockedTo = " + EntityData.DockedTo + " Needs to be 0 (Cannot be Docked)", false);
                                    API.ServerTell(RetrievedData.SpeakerID, ModShortName, "POI = " + EntityData.IsPoi + " Needs to be false", false);
                                    API.ServerTell(RetrievedData.SpeakerID, ModShortName, "FactionID = " + EntityData.FactionID + " Needs to be " + RetrievedData.SpeakerID, false);
                                    API.ServerTell(RetrievedData.SpeakerID, ModShortName, "Creator = " + EntityData.BelongsTo + " Needs to be " + RetrievedData.SpeakerID, false);
                                    API.ServerTell(RetrievedData.SpeakerID, ModShortName, "etype = " + EntityData.Type, false);
                                    API.ServerTell(RetrievedData.SpeakerID, ModShortName, "FacGroup = " + EntityData.FactionGroup + " Needs to be Player", false);
                                    API.ServerTell(RetrievedData.SpeakerID, ModShortName, "Health = " + EntityData.Health, false);
                                    API.ServerTell(RetrievedData.SpeakerID, ModShortName, "isStructure = " + EntityData.IsStructure, false);
                                    API.ServerTell(RetrievedData.SpeakerID, ModShortName, "Name = " + EntityData.Name, false);
                                    API.ServerTell(RetrievedData.SpeakerID, ModShortName, "pfid = " + EntityData.Playfield, false);
                                    API.ServerTell(RetrievedData.SpeakerID, ModShortName, "Docked Count = " + EntityData.Carrying.Count() + " Needs to be 0 (Nothing can be docked to it)", false);
                                    API.ServerTell(RetrievedData.SpeakerID, ModShortName, "", false);
                                }
                                if (EntityData.IsPoi == true)
                                {
                                    API.ServerTell(RetrievedData.SpeakerID, ModShortName, "Failed: Thats a POI, you may not recycle POIs.", true);
                                }
                                else if (EntityData.DockedTo != 0)
                                {
                                    API.ServerTell(RetrievedData.SpeakerID, ModShortName, "Failed: Entity is docked to " + EntityData.DockedTo, true);
                                }
                                else if (EntityData.IsRemoved == true)
                                {
                                    API.ServerTell(RetrievedData.SpeakerID, ModShortName, "Failed: Entity no longer exists.", true);
                                }
                                else if (EntityData.IsProxy == true)
                                {
                                    API.ServerTell(RetrievedData.SpeakerID, ModShortName, "Failed:  Entity is not loaded.", true);
                                }
                                else if (EntityData.Carrying.Count() > 0)
                                {
                                    string CarriedVessels = "";
                                    foreach (int dockedVessel in EntityData.Carrying)
                                    {
                                        CarriedVessels = CarriedVessels + ", ";
                                    }
                                    API.ServerTell(RetrievedData.SpeakerID, ModShortName, "Failed: There are vessels docked to that Entity (" + CarriedVessels + ")", true);
                                }
                                else if (EntityData.FactionGroup == "Player" && EntityData.FactionID == RetrievedData.SpeakerID && EntityData.IsPoi == false && EntityData.DockedTo == 0 && EntityData.Carrying.Count() == 0 && EntityData.IsProxy == false && EntityData.BlockCount > 0)
                                {
                                    //WIP Change this to PlayerInfo so we can do the Cost thing
                                    Storage.StorableData StorableData = new Storage.StorableData
                                    {
                                        function = "Recycle",
                                        Match = Convert.ToString(RetrievedData.SpeakerID),
                                        //Requested = "DialogBox",
                                        Requested = "PlayerInfo",
                                        ChatInfo = RetrievedData.ChatInfo,
                                        SpeakerID = RetrievedData.SpeakerID,
                                        EntityData = EntityData
                                    };
                                    API.PlayerInfo(RetrievedData.SpeakerID, StorableData);
                                }
                                else
                                {
                                    API.ServerTell(RetrievedData.SpeakerID, ModShortName, "Failed: Entity not set to Private or you don\'t own the entity.", true);
                                }
                            }
                        }
                    }
                }
            }
            else if (sender == "SubscriptionVerifier")
            {
                string IncommingData = CommonFunctions.ConvertByteArrayToString(data);
                if (IncommingData.StartsWith("Expiration "))
                {
                    int NewExpiration = int.Parse(IncommingData.Split(' ')[1]);
                    Expiration = NewExpiration;
                    if (Expiration > int.Parse(CommonFunctions.UnixTimeStamp()))
                    {
                        Disable = false;
                    }
                    else
                    {
                        Disable = true;
                    }
                    CommonFunctions.Log("Expiration = " + Expiration);
                    CommonFunctions.Log("Disable = " + Disable);
                }
            }

        }

        public void Shutdown()
        {
        }
    }
}

