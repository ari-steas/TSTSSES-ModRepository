using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using DeepSpaceScanner;
using DeepSpaceScanner.Data.Scripts.DeepSpaceScanner;
using Sandbox.Game;
using Sandbox.Game.Entities;
using Sandbox.ModAPI;
using VRage;
using VRage.Game.Components;
using VRage.Game.Entity;


namespace Scanner.Data.Scripts.DeepSpaceScanner
{
    [MySessionComponentDescriptor(MyUpdateOrder.AfterSimulation)]
    public class ModComponent : MySessionComponentBase
    {
        const ushort Channel = 59701;
        static ConcurrentDictionary<ulong, Guid> _players = new ConcurrentDictionary<ulong, Guid>();
        static ConcurrentDictionary<ulong, ScanTask> _tasks = new ConcurrentDictionary<ulong, ScanTask>();

        public static Guid Guid;

        uint _signature;
        ulong _cnt;
        bool _active;
        static double _scanned;

        static ScanLocalTask _localTask;

        Hud _hud;

        public static bool ScanActive { get; set; }

        public override void BeforeStart()
        {
            base.BeforeStart();
            if (ModConfig.DrawHud) _hud = new Hud();
            MyAPIGateway.Session.OnSessionReady += Ready;
            Guid = Guid.NewGuid();
            if (MyAPIGateway.Session.IsServer) MyVisualScriptLogicProvider.PlayerDisconnected += PlayerDisconnected;
            MyModAPIHelper.MyMultiplayer.Static.RegisterMessageHandler(Channel, HandleMessage);
        }

        protected override void UnloadData()
        {
            base.UnloadData();
            if (MyAPIGateway.Session.IsServer) MyVisualScriptLogicProvider.PlayerDisconnected -= PlayerDisconnected;
            MyModAPIHelper.MyMultiplayer.Static.UnregisterMessageHandler(Channel, HandleMessage);
            _players.Clear();
        }

        static void Ready()
        {
            if (!MyAPIGateway.Utilities.IsDedicated)
                MyModAPIHelper.MyMultiplayer.Static.SendMessageToServer(Channel, MyAPIGateway.Utilities.SerializeToBinary(new Register()), false);
        }

        static void PlayerDisconnected(long playerId)
        {
            var steamId = MyAPIGateway.Multiplayer.Players.TryGetSteamId(playerId);
            Guid res;
            _players.TryRemove(steamId, out res);
        }

        static void HandleMessage(byte[] data)
        {
            ulong? senderId = null;
            long? entityId = null;
            try
            {
                var msg = MyAPIGateway.Utilities.SerializeFromBinary<MessageBase>(data);
                senderId = msg.SenderId;

                if (msg is ScanResponse) HandleScanResponse(msg as ScanResponse);
                else if (msg is Register) HandleRegister(msg as Register);
                else if (msg is ScanRequest)
                {
                    Guid playerGuid;
                    entityId = (msg as ScanRequest).EntityId;
                    if (!_players.TryGetValue(msg.SenderId, out playerGuid)) throw new Exception("Player Guid doesn't exist");
                    if (playerGuid != msg.Guid) throw new Exception("Invalid player Guid");
                    HandleScanRequest(msg as ScanRequest);
                }
                else if (msg is Scanned) HandleScanned();
            }
            catch (Exception e)
            {
                if (senderId == null)
                {
                    Log.Error(e);
                    return;
                }
                
                SendResponse(new ScanResponse
                {
                    Error = e.Message,
                    SenderId = (ulong) senderId,
                    EntityId = entityId
                });
            }
        }

        static void HandleRegister(Register msg)
        {
            _players.TryAdd(msg.SenderId, msg.Guid);
        }

        static void HandleScanRequest(ScanRequest msg)
        {
            var entity = MyEntities.GetEntityById(msg.EntityId);
            if (entity == null) throw new Exception("Entity doesn't exist");
            var b = (MyCubeBlock) entity;

            var player = MyAPIGateway.Multiplayer.Players.GetPlayerControllingEntity(b.GetTopMostParent());
            if (player == null || player.SteamUserId != msg.SenderId) throw new Exception("You must control grid to initiate scan");

            var l = b.GameLogic.GetAs<ScanLogic>();
            if (l == null) return;

            if (_tasks.ContainsKey(msg.SenderId)) throw new Exception("Previous scan is still active");

            var task2 = new ScanTask(msg, l);
            _tasks.TryAdd(msg.SenderId, task2);
            task2.Start();
        }

        static void HandleScanResponse(ScanResponse msg)
        {
            try
            {
                if (msg.Error != null) MyAPIGateway.Utilities.InvokeOnGameThread(() => MyAPIGateway.Utilities.ShowNotification(msg.Error, 2000, "Red"));
                else if (_localTask != null)
                {
                    msg.Results = _localTask.Response.Results;
                    _localTask = null;
                }
                ScanActive = false;
                
                if (msg.EntityId == null) return;
                var entity = MyEntities.GetEntityById((long) msg.EntityId);
                if (entity == null) return;
                var b = (MyCubeBlock) entity;
                var l = b.GameLogic.GetAs<ScanLogic>();
                if (l == null) return;
                l.ModuleScanActive = false;
                
                if (msg.Error != null) return;
                
                if (msg.Results != null)
                {
                    l.ScanResults = msg.Results;
                    ScanLogic.RefreshCustomInfo(b as IMyTerminalBlock);
                }

                var lcd = l.TextSurface;
                if (!l.ShowPopup && lcd.Equals(default(MyTuple<long, int>))) return;
                var s = new StringBuilder();
                var i = 1;
                if (msg.Results == null || msg.Results.Count == 0) s.AppendLine("No signatures found");
                else msg.Results.ForEach(x => s.AppendLine($"{i++}. {x}\n"));
                
                MyAPIGateway.Utilities.InvokeOnGameThread(() =>
                {
                    try
                    {
                        var str = s.ToString();
                        if (l.ShowPopup) MyAPIGateway.Utilities.ShowMissionScreen("Scan Results", "", "", str);
                        MyEntity block;
                        if (lcd.Equals(default(MyTuple<long, int>))) return; 
                        if (!MyEntities.TryGetEntityById(lcd.Item1, out block)) return;
                        (block as IMyTextSurfaceProvider).GetSurface(lcd.Item2).WriteText(str);
                    }
                    catch (Exception e)
                    {
                        Log.Error(e);
                    }
                });
            }
            catch (Exception e)
            {
                Log.Error(e);
            }
        }

        static void HandleScanned()
        {
            try
            {
                if (MyAPIGateway.Utilities.IsDedicated) return;
                _scanned = MyAPIGateway.Session.ElapsedPlayTime.TotalMilliseconds;
            }
            catch (Exception e)
            {
                Log.Error(e);
            }
        }

        public static void SendRequest(IMyTerminalBlock b)
        {
            var l = b.GameLogic.GetAs<ScanLogic>();
            if (l == null) return;
            if (ScanActive || l.ModuleScanActive) return;
            l.ModuleScanActive = true;
            l.ScanResults.Clear();
            var request = new ScanRequest(b.EntityId, l.ScannerStrength, l.Pitch, l.Yaw, l.ScanAsteroids);
            if (l.ScanAsteroids)
            {
                _localTask = new ScanLocalTask(request, l);
                _localTask.Start();
            }
            var serialized = MyAPIGateway.Utilities.SerializeToBinary(request);
            MyModAPIHelper.MyMultiplayer.Static.SendMessageToServer(Channel, serialized, true);
        }

        public static void SendResponse(MessageBase response)
        {
            if (response is ScanResponse)
            {
                var r = (ScanResponse) response;
                if (r.Error != null) r.Results?.Clear();
            }
            var serialized = MyAPIGateway.Utilities.SerializeToBinary(response);
            MyModAPIHelper.MyMultiplayer.Static.SendMessageTo(Channel, serialized, response.SenderId, true);
        }

        ulong _frame;

        public override void UpdateAfterSimulation()
        {
            base.UpdateAfterSimulation();
            if (_frame++ % 10 != 0) return;

            if (MyAPIGateway.Session.IsServer)
            {
                foreach (var kv in _tasks)
                {
                    if (!kv.Value.IsComplete) continue;
                    kv.Value.Finish();
                    _tasks.Remove(kv.Key);
                    if (kv.Value.Response != null) SendResponse(kv.Value.Response);
                }
                
                if (MyAPIGateway.Utilities.IsDedicated) return;
            } 

            if (MyAPIGateway.Session.ElapsedPlayTime.TotalMilliseconds - _scanned > ModConfig.ScanDuration) _scanned = 0;

            var ctrl = MyAPIGateway.Session.ControlledObject;
            _active = ctrl is MyShipController;
            if (!_active) return;
            var l = (ctrl as MyShipController).CubeGrid.GameLogic.GetAs<GridLogic>();
            if (l == null) return;
            _signature = l.Signature;
        }

        public override void Draw()
        {
            base.Draw();
            if (!_active || _frame < 300) return;
            _hud?.Draw(_signature, _scanned > 0);
        }
    }
}