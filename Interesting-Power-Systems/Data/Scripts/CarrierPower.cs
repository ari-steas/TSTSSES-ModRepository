using System.Collections.Generic;
using Sandbox.Game;
using Sandbox.Game.Entities;
using Sandbox.ModAPI;
using Sandbox.ModAPI.Interfaces;
using SpaceEngineers.Game.ModAPI;
using VRage.Game;
using VRage.Game.Components;
using VRage.Game.ModAPI;
using VRage.ModAPI;
using VRage.Utils;

// Mostly stolen (with permission) from Klime - see https://steamcommunity.com/sharedfiles/filedetails/?id=1844150178
namespace CarrierPower
{
    [MySessionComponentDescriptor(MyUpdateOrder.BeforeSimulation)]
    public class CarrierPower : MySessionComponentBase
    {
        static readonly float powerRadius = 100; // currently defunct, uses beacon range
        static readonly float threshold = 10; // if within +- distance, do thing (also defunct)

        static readonly string transmitterSubtype = "PowerTransmitter";
        static readonly string receiverSubtype = "PowerReceiver";

        private List<IMyTerminalBlock> transmitterList = new List<IMyTerminalBlock>();
        private List<IMyTerminalBlock> receiverList = new List<IMyTerminalBlock>();

        private int receiverIterator = 0;
        private int _timer = 0;

        public override void LoadData()
        {
            base.LoadData();
            MyEntities.OnEntityCreate += OnEntityCreate;
        }

        private void OnEntityCreate(IMyEntity entity)
        {
            if (entity is IMyCubeBlock)
            {
                // check block subtype and add to lists
                var subtype = (entity as IMyCubeBlock).BlockDefinition.SubtypeId;
                if (subtype == receiverSubtype && !receiverList.Contains(entity as IMyTerminalBlock))
                {
                    receiverList.Add(entity as IMyTerminalBlock);
                }
                // transmitters
                if (subtype == transmitterSubtype && !transmitterList.Contains(entity as IMyTerminalBlock))
                {
                    transmitterList.Add(entity as IMyTerminalBlock);
                }
            }
        }

        public override void Init(MyObjectBuilder_SessionComponent sessionComponent)
        {
            base.Init(sessionComponent);
            MyVisualScriptLogicProvider.BlockBuilt += BlockBuilt;
            MyVisualScriptLogicProvider.BlockDestroyed += BlockDestroyed;
        }

        private void BlockBuilt(string typeid, string subtypeid, string gridname, long blockid)
        {
            // defunct block logging, done through entitycreate now
            #region old
            //// Log newly built transmitters
            //if (subtypeid == transmitterSubtype)
            //{
            //    IMyTerminalBlock test = MyAPIGateway.Entities.GetEntityById(blockid) as IMyTerminalBlock;
            //    if (test != null && !transmitterList.Contains(test))
            //    {
            //        transmitterList.Add(test);
            //    }
            //    MyLog.Default.WriteLine("new transmitter placed");
            //}

            //// Log newly built receivers
            //// 
            //if (subtypeid == receiverSubtype)
            //{
            //    IMyTerminalBlock test = MyAPIGateway.Entities.GetEntityById(blockid) as IMyTerminalBlock;
            //    if (test != null && !receiverList.Contains(test))
            //    {
            //        receiverList.Add(test);
            //    }
            //    MyLog.Default.WriteLine("new transmitter placed");
            //}
            #endregion

            // test print
            //foreach (IMyTerminalBlock block in receiverList)
            //{
            //    MyAPIGateway.Utilities.ShowNotification(block.EntityId.ToString());
            //}
        }

        private void BlockDestroyed(string entityName, string gridname, string typeId, string subtypeId)
        {
            // maybe do something here idk
        }

        public override void UpdateBeforeSimulation()
        {
            _timer += 1;

            // every tick check one receiver

            if (receiverIterator >= receiverList.Count)
            {
                receiverIterator = 0;
            }
            else
            {
                var receiver = receiverList[receiverIterator];
                try
                {
                    checkTransmitters(receiver);
                }
                catch (System.NullReferenceException e)
                {
                    MyLog.Default.WriteLine("Transmitter exception: " + e.StackTrace);
                }
                receiverIterator++;
            }

            // clear junk every 100 ticks
            // did there used to be a function for this?

            if (_timer % 100 == 0)
            {

                #region old
                /*
                allPlayer.Clear();
                MyAPIGateway.Multiplayer.Players.GetPlayers(allPlayer);
                for (int i = 0; i < allPlayer.Count; i++)
                {
                    for (int j = 0; j < safedict.Count; j++)
                    {
                        if (MyAPIGateway.Entities.EntityExists(safedict[j].EntityId))
                        {
                            if (safedict[j].IsSafeZoneEnabled())
                            {
                                if (allPlayer[i].Character != null && (allPlayer[i].Character.WorldMatrix.Translation - safedict[j].WorldMatrix.Translation).Length() <= safedict[j].GetValueFloat("SafeZoneSlider"))
                                {
                                    MyVisualScriptLogicProvider.SetPlayersHydrogenLevel(allPlayer[i].IdentityId, 1f);
                                    break;
                                }
                            }
                        }
                    }
                }
                */
                #endregion

                clearDeletedBlocks();

            }

        }

        protected void checkTransmitters(IMyTerminalBlock receiver)
        {
            var receiverFaction = MyAPIGateway.Session.Factions.TryGetPlayerFaction(receiver.OwnerId);

            foreach (IMyTerminalBlock transmitter in transmitterList)
            {
                // transmitter faction
                var transmitterFaction = MyAPIGateway.Session.Factions.TryGetPlayerFaction(transmitter.OwnerId);

                // check beacon, working, faction
                if (transmitter is IMyBeacon
                    && transmitter.IsWorking
                    && receiverFaction == transmitterFaction)
                {
                    // if radius found
                    // power on block and return for this cycle

                    var distance = (receiver.WorldMatrix.Translation - transmitter.WorldMatrix.Translation).LengthSquared();
                    var radius = (transmitter as IMyBeacon).Radius;
                    if (distance < radius * radius)
                    {
                        (receiver as IMyBatteryBlock).Enabled = true;
                        //MyAPIGateway.Utilities.ShowNotification("TRANSMITTER FOUND");
                        return;
                    }
                }
            }

            // fallthrough - out of all transmitter radii
            // force power off
            (receiver as IMyBatteryBlock).Enabled = false;

        }

        private void clearDeletedBlocks()
        {
            receiverList.RemoveAll(isDeleted);
            transmitterList.RemoveAll(isDeleted);
        }

        public static bool isDeleted(IMyTerminalBlock block)
        {
            return (MyAPIGateway.Entities.GetEntityById(block.EntityId) == null);
        }

        // defunct old garbage init checking
        #region old
        //private List<IMyPlayer> allPlayer = new List<IMyPlayer>();
        List<IMySlimBlock> allb = new List<IMySlimBlock>();
        protected void initialSetup()
        {
            // cycle through all entities as cubegrids
            HashSet<IMyEntity> allents = new HashSet<IMyEntity>();
            MyAPIGateway.Entities.GetEntities(allents);
            foreach (var ent in allents)
            {
                IMyCubeGrid cubeg = ent as IMyCubeGrid;
                if (cubeg != null)
                {
                    // allb as blocks in grid, cycle through all blocks 
                    allb.Clear();
                    cubeg.GetBlocks(allb);
                    foreach (var block in allb)
                    {
                        if (block.FatBlock != null)
                        {
                            // log any Transmitters
                            bool isTransmitter = (block.FatBlock.BlockDefinition.SubtypeName == transmitterSubtype);
                            IMyTerminalBlock testTransmitter = block.FatBlock as IMyTerminalBlock;
                            if (isTransmitter && testTransmitter != null && !transmitterList.Contains(testTransmitter))
                            {
                                transmitterList.Add(testTransmitter);
                                MyLog.Default.WriteLine("init - Transmitter added: " + testTransmitter.EntityId.ToString());
                            }

                            // log any Receivers
                            bool isReceiver = (block.FatBlock.BlockDefinition.SubtypeName == receiverSubtype);
                            IMyTerminalBlock testReceiver = block.FatBlock as IMyTerminalBlock;
                            if (isReceiver && testReceiver != null && !receiverList.Contains(testReceiver))
                            {
                                receiverList.Add(testReceiver);
                                MyLog.Default.WriteLine("init - Reciever added: " + testReceiver.EntityId.ToString());
                            }
                        }

                    }
                }
            }
        }
        #endregion

        protected override void UnloadData()
        {
            MyVisualScriptLogicProvider.BlockBuilt -= BlockBuilt;
            MyVisualScriptLogicProvider.BlockDestroyed -= BlockDestroyed;
            MyEntities.OnEntityCreate -= OnEntityCreate;

            allb = null;
            //allPlayer = null;
            transmitterList = null;
            receiverList = null;
        }
    }
}