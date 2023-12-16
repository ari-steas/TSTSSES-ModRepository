using Sandbox.Common.ObjectBuilders;
using Sandbox.Game.Entities;
using Sandbox.Game.EntityComponents;
using Sandbox.ModAPI;
using SpaceEngineers.Game.ModAPI;
using System;
using System.Collections.Generic;
using VRage;
using VRage.Game;
using VRage.Game.Components;
using VRage.Game.ModAPI;
using VRage.ModAPI;
using VRage.ObjectBuilders;

namespace Cython.CSS.IndependentSurvivalModule.Old
{
    [MyEntityComponentDescriptor(typeof(MyObjectBuilder_Reactor), true, "CythonSuitRTGReactor")]
    public class SuitRTGReactor : MyGameLogicComponent
    {
        MyObjectBuilder_EntityBase ObjectBuilder;

        VRage.Game.ModAPI.IMyInventory Inventory;

        public override void Init(MyObjectBuilder_EntityBase objectBuilder)
        {
            ObjectBuilder = objectBuilder;
            this.NeedsUpdate = MyEntityUpdateEnum.EACH_FRAME | MyEntityUpdateEnum.BEFORE_NEXT_FRAME;
        }

        public override MyObjectBuilder_EntityBase GetObjectBuilder(bool copy = false)
        {
            if (copy)
            {
                return (MyObjectBuilder_EntityBase)ObjectBuilder.Clone();
            }
            else
            {
                return ObjectBuilder;
            }
        }

        public override void UpdateOnceBeforeFrame()
        {
            InitializeGrid();

            Inventory = (VRage.Game.ModAPI.IMyInventory)(Entity as Sandbox.ModAPI.IMyTerminalBlock).GetInventory(0) as VRage.Game.ModAPI.IMyInventory;


            // Setting up the MedBay here, so I don't have to make an extra GameLogicComponent
            Sandbox.ModAPI.IMyGridTerminalSystem tsystem = MyAPIGateway.TerminalActionsHelper.GetTerminalSystemForGrid((Entity as Sandbox.ModAPI.IMyFunctionalBlock).CubeGrid);
            List<Sandbox.ModAPI.Ingame.IMyTerminalBlock> cubes = new List<Sandbox.ModAPI.Ingame.IMyTerminalBlock>();
            tsystem.GetBlocksOfType<IMyMedicalRoom>(cubes);
            var sink = (cubes[0] as Sandbox.ModAPI.IMyFunctionalBlock).Components.Get<MyResourceSinkComponent>();
            sink.MaxRequiredInput = 0.000008f;
            sink.Update();
        }

        void InitializeGrid()
        {
            //MyAPIGateway.Utilities.ShowNotification("Grids: " + gridsThreaded.Count, 10000, MyFontEnum.Red);

            IMyCubeGrid grid = ((Sandbox.ModAPI.IMyFunctionalBlock)Entity).CubeGrid;

            grid.Physics.Enabled = false;
            grid.Visible = false;
            grid.CastShadows = false;

            grid.Name = "SuitBackpack";

            IMyPlayer player = null;
            List<IMyPlayer> players = new List<IMyPlayer>();

            MyAPIGateway.Players.GetPlayers(players);

            //MyAPIGateway.Utilities.ShowNotification("Owners: " + MyAPIGateway.Session.Player.IdentityId, 10000, MyFontEnum.Red);
            //MyAPIGateway.Utilities.ShowNotification("Owners: " + ((Sandbox.ModAPI.IMyFunctionalBlock)Entity).OwnerId, 10000, MyFontEnum.Red);


            foreach (var allPlayer in players)
            {
                if (allPlayer.IdentityId == ((Sandbox.ModAPI.IMyFunctionalBlock)Entity).OwnerId)
                    player = allPlayer;
            }

            if (MyAPIGateway.Multiplayer.IsServer)
            {
                grid.ChangeGridOwnership(0, MyOwnershipShareModeEnum.All);
            }

            if (MyAPIGateway.Multiplayer.IsServer)
            {

                CSSIndependentSurvivalModuleCore.Backpacks.Add(player, grid);

                Sandbox.ModAPI.IMyGridTerminalSystem tsystem = MyAPIGateway.TerminalActionsHelper.GetTerminalSystemForGrid(grid);
                List<Sandbox.ModAPI.Ingame.IMyTerminalBlock> cubes = new List<Sandbox.ModAPI.Ingame.IMyTerminalBlock>();

                tsystem.GetBlocksOfType<Sandbox.ModAPI.IMyRefinery>(cubes);
                var refinery = cubes[0] as Sandbox.ModAPI.IMyFunctionalBlock;

                cubes.Clear();
                tsystem.GetBlocksOfType<IMyMedicalRoom>(cubes);
                var medicalRoom = cubes[0] as Sandbox.ModAPI.IMyFunctionalBlock;

                cubes.Clear();

                byte[] answer = new byte[20];
                byte[] answerID = BitConverter.GetBytes(1);
                byte[] answerRefineryId = BitConverter.GetBytes(refinery.EntityId);
                byte[] answerMedicalRoomId = BitConverter.GetBytes(medicalRoom.EntityId);

                for (int i = 0; i < 4; i++)
                {
                    answer[i] = answerID[i];
                }

                for (int i = 0; i < 8; i++)
                {
                    answer[i + 4] = answerRefineryId[i];
                }

                for (int i = 0; i < 8; i++)
                {
                    answer[i + 12] = answerMedicalRoomId[i];
                }

                MyAPIGateway.Multiplayer.SendMessageTo(5890, answer, player.SteamUserId, true);
                if (!MyAPIGateway.Utilities.IsDedicated)
                {
                    //MyAPIGateway.Utilities.ShowNotification("New grid initialized:" + MyAPIGateway.Session.Player.IdentityId, 10000, MyFontEnum.Red);
                }
            }



        }

        public override void UpdateBeforeSimulation()
        {
            Inventory.AddItems(((MyFixedPoint)1) - Inventory.GetItemAmount(CSSIndependentSurvivalModuleCore.RTGPelletId), CSSIndependentSurvivalModuleCore.RTGPelletBuilder);
        }
    }
}

