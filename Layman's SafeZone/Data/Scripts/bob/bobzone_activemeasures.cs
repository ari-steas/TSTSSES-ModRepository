using Sandbox.Common.ObjectBuilders;
using Sandbox.Game.Entities;
using Sandbox.Game.EntityComponents;
using Sandbox.Game.Weapons;
using Sandbox.ModAPI;
using Sandbox.ModAPI.Interfaces.Terminal;
using Sandbox.ModAPI.Weapons;
using System;
using System.Collections.Generic;
using Sandbox.Game.Entities;
using Sandbox.Game;

using Sandbox.ModAPI;
using VRageMath;
using VRage.ModAPI;
using VRage.ObjectBuilders;
using VRage.Game.Components;
using Sandbox.Definitions;
//using Sandbox.ModAPI.Ingame;
using Sandbox.ModAPI.Interfaces;
using ParallelTasks;
using VRage.Game;
using VRage.Game.ModAPI;
using VRage.Game.ModAPI.Interfaces;
using Sandbox.Game.Lights;

using System.Threading;
using System.Text;
using VRage.Utils;
using VRage.Library.Utils;
using Sandbox.Game.SessionComponents;
using Sandbox.Graphics;
using VRage;
using Sandbox.Game.Entities.Cube;
using VRage.Game.Entity;
using BlendTypeEnum = VRageRender.MyBillboard.BlendTypeEnum;

namespace bobzone
{

    [MySessionComponentDescriptor(MyUpdateOrder.BeforeSimulation | MyUpdateOrder.AfterSimulation)]
    public class Session : MySessionComponentBase
    {
        public static bool isHost;
        public static bool isServer;
        public static double tock;
        public static Session Instance;

        public static float radius = 500f;
        public long tick = 0;
        private HashSet<IMyPlayer> players;
        private List<byte> spacket = new List<byte>();
        private List<IMyCubeGrid> dirties = new List<IMyCubeGrid>();
        public static HashSet<IMyCubeBlock> zoneblocks = new HashSet<IMyCubeBlock>();
        public static Dictionary<long, long> zonelookup = new Dictionary<long, long>();

        private Dictionary<long, bool> playerInZone = new Dictionary<long, bool>(); // Track players in zone

        public override void Init(MyObjectBuilder_SessionComponent sessionComponent)
        {
            MyAPIGateway.Session.DamageSystem.RegisterBeforeDamageHandler(9, ProcessDamage);
        }

        public override void BeforeStart()
        {
            isHost = MyAPIGateway.Session.OnlineMode == MyOnlineModeEnum.OFFLINE || MyAPIGateway.Multiplayer.IsServer;
            isServer = MyAPIGateway.Utilities.IsDedicated;
        }

        public override void UpdateBeforeSimulation()
        {
            if (MyAPIGateway.Session == null)
            {
                return;
            }

            tick++;

            List<IMyPlayer> playerlist = new List<IMyPlayer>();
            MyAPIGateway.Players.GetPlayers(playerlist);
            players = new HashSet<IMyPlayer>(playerlist);
            MyAPIGateway.Parallel.ForEach(players, x =>
            {
                var player = x as IMyPlayer;
                if (player == null)
                    return;

                bool isInAnyZone = false;

                foreach (IMyCubeBlock zoneblock in zoneblocks)
                {
                    if (player.Controller?.ControlledEntity?.Entity is IMyCharacter)
                    {
                        IMyCharacter character = player.Controller.ControlledEntity.Entity as IMyCharacter;
                        double distance = (zoneblock.WorldMatrix.Translation - character.WorldMatrix.Translation).LengthSquared();
                        if (distance < radius * radius)
                        {
                            isInAnyZone = true;
                            if (!playerInZone.ContainsKey(player.Identity.IdentityId) || !playerInZone[player.Identity.IdentityId])
                            {
                                playerInZone[player.Identity.IdentityId] = true;
                                var color = GetFactionColor(zoneblock.OwnerId);
                                var colorVector = color.ToVector4();
                                MyVisualScriptLogicProvider.SendChatMessageColored("You have entered the zone.", colorVector, zoneblock.CubeGrid.CustomName);
                            }
                        }
                    }
                }

                if (!isInAnyZone)
                {
                    if (playerInZone.ContainsKey(player.Identity.IdentityId) && playerInZone[player.Identity.IdentityId])
                    {
                        playerInZone[player.Identity.IdentityId] = false;
                        MyAPIGateway.Utilities.ShowNotification("You have left the zone.", 2000, MyFontEnum.Red);
                    }
                }
            });
        }

        protected override void UnloadData()
        {
            // No direct way to unregister damage handlers in current SE API
            // Ensure no references to prevent memory leaks or unintended behavior
            MyAPIGateway.Session.DamageSystem.RegisterBeforeDamageHandler(9, null);
            players?.Clear();
            dirties?.Clear();
            zoneblocks?.Clear();
            zonelookup?.Clear();
            playerInZone?.Clear();
        }

        public void ProcessDamage(object target, ref MyDamageInformation info)
        {
            IMySlimBlock slim = target as IMySlimBlock;
            long idZone = 0;

            if (slim == null || slim.CubeGrid == null)
                return;

            Vector3D targetPosition = Vector3D.Zero;
            slim.ComputeWorldCenter(out targetPosition);

            foreach (IMyCubeBlock zoneblock in zoneblocks)
            {
                double distance = (zoneblock.WorldMatrix.Translation - targetPosition).LengthSquared();
                if (distance < radius * radius)
                {
                    idZone = zoneblock.OwnerId;
                    break;
                }
            }

            if (idZone == 0)
                return;

            if (info.Type == MyDamageType.Grind)
            {
                IMyEntity ent = MyAPIGateway.Entities.GetEntityById(info.AttackerId);
                IMyPlayer player = MyAPIGateway.Players.GetPlayerControllingEntity(ent);

                if (player != null && MyIDModule.GetRelationPlayerBlock(idZone, player.Identity.IdentityId) == MyRelationsBetweenPlayerAndBlock.Enemies)
                {
                    info.Amount = 0;
                }
                else
                {
                    IMyHandheldGunObject<MyToolBase> tool = ent as IMyAngleGrinder;
                    IMyCubeBlock block = ent as IMyCubeBlock;
                    if (block != null)
                    {
                        if (MyIDModule.GetRelationPlayerBlock(idZone, block.OwnerId) == MyRelationsBetweenPlayerAndBlock.Enemies)
                        {
                            info.Amount = 0;
                        }
                    }
                    else if (tool != null)
                    {
                        if (MyIDModule.GetRelationPlayerBlock(idZone, tool.OwnerIdentityId) == MyRelationsBetweenPlayerAndBlock.Enemies)
                        {
                            info.Amount = 0;
                        }
                    }
                }
            }
            else
            {
                info.Amount = 0;
            }
        }

        private Color GetFactionColor(long ownerId)
        {
            var faction = MyAPIGateway.Session.Factions.TryGetPlayerFaction(ownerId);
            if (faction != null)
            {
                var colorMask = faction.CustomColor;
                return ColorMaskToRgb(colorMask).ToColor();
            }
            return Color.White; // Default to white if no faction or color is found
        }

        private Vector3 ColorMaskToRgb(Vector3 colorMask)
        {
            return MyColorPickerConstants.HSVOffsetToHSV(colorMask).HSVtoColor();
        }
    }

    [MyEntityComponentDescriptor(typeof(MyObjectBuilder_UpgradeModule), false, "bobzone")]
    public class bobzoneblock : MyGameLogicComponent
    {
        public IMyUpgradeModule ModBlock { get; private set; }
        public IMyCubeGrid ModGrid;
        public string faction;
        public float radius = 50f; // defer to session value
        private MatrixD matrix;
        private long tock = 0;

        public override void Init(MyObjectBuilder_EntityBase objectBuilder)
        {
            NeedsUpdate = MyEntityUpdateEnum.EACH_FRAME;
            ModBlock = Entity as IMyUpgradeModule;
            ModGrid = ModBlock.CubeGrid;
            radius = Session.radius;
        }

        public override void UpdateAfterSimulation()
        {
            if (ModBlock == null || !ModBlock.IsFunctional || ModBlock.MarkedForClose || ModBlock.Closed || ModBlock.MarkedForClose || !ModBlock.Enabled || !ModGrid.IsStatic)
            {
                lock (Session.zoneblocks)
                {
                    Session.zoneblocks.Remove(ModBlock);
                }

                return;
            }

            lock (Session.zoneblocks)
            {
                Session.zoneblocks.Add(ModBlock);
            }

            tock++;
            Vector3D storage = matrix.Up;
            matrix = ModBlock.WorldMatrix;
            double rad = ((double)tock / 100) % 360 * Math.PI / 180;
            matrix = MatrixD.CreateWorld(matrix.Translation, matrix.Up, matrix.Right * Math.Cos(rad) + matrix.Forward * Math.Sin(rad));

            if (!Session.isServer)
            {
                double renderdistance = (matrix.Translation - MyAPIGateway.Session.Camera.Position).Length();

                if (renderdistance < 20 * radius)
                {
                    var factionColor = GetFactionColor(ModBlock.OwnerId);
                    DrawRing(matrix.Translation, radius, 32, factionColor); // Draw a ring with 32 segments
                }
            }
        }

        public override void Close()
        {
            lock (Session.zoneblocks)
            {
                Session.zoneblocks.Remove(ModBlock);
            }
            base.Close();
        }

        private Color GetFactionColor(long ownerId)
        {
            var faction = MyAPIGateway.Session.Factions.TryGetPlayerFaction(ownerId);
            if (faction != null)
            {
                var colorMask = faction.CustomColor;
                return ColorMaskToRgb(colorMask).ToColor();
            }
            return Color.White; // Default to white if no faction or color is found
        }

        private Vector3 ColorMaskToRgb(Vector3 colorMask)
        {
            return MyColorPickerConstants.HSVOffsetToHSV(colorMask).HSVtoColor();
        }

        private void DrawRing(Vector3D center, double radius, int segments, Color color)
        {
            double angleStep = 2 * Math.PI / segments;
            for (int i = 0; i < segments; i++)
            {
                double angle1 = i * angleStep;
                double angle2 = (i + 1) * angleStep;
                Vector3D start = center + new Vector3D(radius * Math.Cos(angle1), 0, radius * Math.Sin(angle1));
                Vector3D end = center + new Vector3D(radius * Math.Cos(angle2), 0, radius * Math.Sin(angle2));
                Vector4 colorVector = color.ToVector4();
                MySimpleObjectDraw.DrawLine(start, end, MyStringId.GetOrCompute("Square"), ref colorVector, 1f);
            }
        }
    }

    public static class Vector3Extensions
    {
        public static Color ToColor(this Vector3 vector)
        {
            return new Color(vector.X, vector.Y, vector.Z);
        }
    }

}