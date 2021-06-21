using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Eleon;
using Eleon.Modding;
using System.IO;
using YamlDotNet.Serialization;


namespace Recycle
{

    class Interconnectivity
    {
        internal class StorableData
        {
            public int SpeakerID; //Keep
            public string Requested; //Keep
            public string Match; //Keep
            public string function; //Keep
            public ChatInfo ChatInfo; //Keep
            public EntityData EntityData;
            //public PlayerInfo TriggerPlayer;
            //public List<PlayerInfo> TargetPlayer;
            //public IdStructureBlockInfo StructureBlockInfo;
            //public PlayfieldEntityList PlayfieldEntities;
            //public ConsoleCommandInfo ConsoleCommand;
            //public Id PlayerConnected;
            //public Id PlayerDisconnected;
            //public IdList PlayerList;
            //public ItemExchangeInfo ItemExchange;
            //public IdCredits PlayerCredits;
            //public IdPlayfield PlayfieldChange;
            //public PlayfieldList PlayfieldList;
            //public PlayfieldStats PlayfieldStats;
            //public PlayfieldLoad PlayfieldLoad;
            //public PlayfieldLoad PlayfieldUnload;
            //public DediStats DediStats;
            //public GameEventData GameEvent;
            //public GlobalStructureList GlobalStructsList;
            //public IdPositionRotation EntityPosRot;
            //public FactionChangeInfo FactionChange;
            //public FactionInfoList GetFactions;
            //public StatisticsParam EventStatistics;
            //public Id NewEntityId;
            //public string OK;
            //public ErrorInfo ErrorInfo;
            //public Id PlayerDisconnectedWaiting;
            //public IdStructureBlockInfo StructureBlockStatistics;
            //public AlliancesTable AlliancesAll;
            //public AlliancesFaction AlliancesFaction;
            //public BannedPlayerData BannedPlayers;
            //public TraderNPCItemSoldInfo TraderNPCItemSold;
            //public Inventory PlayerGetAndRemoveInventory;
            //public PdaStateChange PdaStateChange;
            //public PlayfieldEntityList PlayfieldEntityList;
            //public IdAndIntValue DialogButtonIndex;
        }

        internal class Recycling
        {
            public int EntityID { get; set; }
            public int PlayerID { get; set; }
            public bool ValidTarget { get; set; }
            public string PlayfieldName { get; set; }
            public int Cost { get; set; }
            public EntityData EntityData { get; set; }
        }

        internal class EntityData
        {
            public int DockedTo {get; set;} //Should be 0
            public int BelongsTo { get; set; }
            public bool IsPoi { get; set; } //Should be 0
            public bool IsProxy { get; set; } //Should be 0
            public bool IsLocal { get; set; } //Should be 0
            public string Name { get; set; }
            public int Id { get; set; }
            public string Type { get; set; } //BA 2, CV 3, SV 4, HV 5
            public int FactionID { get; set; }
            public string FactionGroup { get; set; } //Should be 1
            public string Playfield { get; set; }
            public int X { get; set; }
            public int Y { get; set; }
            public int Z { get; set; }
            public List<int> Carrying { get; set; } //Should be null
            public float Health { get; set; }
            public bool IsRemoved { get; set; }
            public bool IsStructure { get; set; }
            public int TriangleCount { get; set; }
            public int LightCount { get; set; }
            public int BlockCount { get; set; }
            public int DeviceCount { get; set; }
            public float TotalMass { get; set; }
            public string CoreType { get; set; }
            public bool HasLandclaimDevice { get; set; }
            public bool IsOfflineProtectable { get; set; }
            public bool IsPowered { get; set; }
            public bool IsReady { get; set; }
            public bool IsShieldActive { get; set; }
            public int Pilot { get; set; }
            public string Creator { get; set; }
            public double SizeClass { get; set; }
        }

        public static EntityData ReadYaml(String filePath)
        {
            using (var input = File.OpenText(filePath))
            {
                Deserializer deserializer = new DeserializerBuilder()
                    .IgnoreUnmatchedProperties()
                    .Build();
                EntityData Output = deserializer.Deserialize<EntityData>(input);
                return Output;
            }
        }

        public static void WriteYaml(string Path, EntityData ConfigData)
        {
            File.WriteAllText(Path, "---\r\n");
            Serializer serializer = new SerializerBuilder()
                //.EmitDefaults()
                .Build();
            string WriteThis = serializer.Serialize(ConfigData);
            File.AppendAllText(Path, WriteThis);
        }

    }
}
