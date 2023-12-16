using Sandbox.Game.Entities;
using Sandbox.Game.Entities.Character;
using Sandbox.Game.Entities.Cube;
using Sandbox.Game.Gui;
using Sandbox.Game.Weapons;
using Sandbox.Game.World;
using Sandbox.ModAPI;
using SpaceEngineers.Game.Entities.Blocks;
using SpaceEngineers.Game.ModAPI;
using System;
using System.Collections.Generic;
using VRage.Game;
using VRage.Game.Components;
using VRage.Game.Entity.UseObject;
using VRage.Game.ModAPI;
using VRage.ModAPI;
using VRage.ObjectBuilders;
using VRageMath;
using VRageRender.Messages;

namespace Cython.CSS.IndependentSurvivalModule.Old
{
    [MySessionComponentDescriptor(MyUpdateOrder.BeforeSimulation | MyUpdateOrder.AfterSimulation | MyUpdateOrder.Simulation)]
    public class CSSIndependentSurvivalModuleCore : MySessionComponentBase
    {
        public static MyObjectBuilder_PhysicalObject RTGPelletBuilder = new MyObjectBuilder_Ingot() { SubtypeName = "SuitRTGPellet" };
        public static SerializableDefinitionId RTGPelletId = new SerializableDefinitionId(typeof(MyObjectBuilder_Ingot), "SuitRTGPellet");

        // Serverside
        public static Dictionary<IMyPlayer, IMyCubeGrid> Backpacks = new Dictionary<IMyPlayer, IMyCubeGrid>();

        volatile Dictionary<IMyPlayer, List<IMyCubeGrid>> GridsPerPlayerCallback = new Dictionary<IMyPlayer, List<IMyCubeGrid>>();

        //Clientside
        bool IdsReceived = false;
        IMyFunctionalBlock SuitRefinery = null;
        IMyFunctionalBlock SuitMedicalRoom = null;
        long SuitRefineryId = 0;
        long SuitMedicalRoomId = 0;

        bool Initialized = false;
        bool HandlersInitialized = false;
        bool InterfaceOpen = false;

        bool inventoryOpen = false;

        float originalMaxDistance = MyConstants.DEFAULT_INTERACTIVE_DISTANCE;

        MyObjectBuilder_SessionComponent ObjectBuilder = null;

        public override void Init(MyObjectBuilder_SessionComponent sessionComponent)
        {
            ObjectBuilder = sessionComponent;
        }

        public override MyObjectBuilder_SessionComponent GetObjectBuilder()
        {
            return ObjectBuilder;
        }

        protected override void UnloadData()
        {
            if (HandlersInitialized)
            {
                MyAPIGateway.Multiplayer.UnregisterMessageHandler(5890, HandleMessage);
            }
        }

        /*protected override void LoadData ()
		{
			if(!Initialized) 
			{
				SetDistance();
				//MyConstants.DEFAULT_INTERACTIVE_DISTANCE = 100000000000f;
			}
		}*/

        public override void SaveData()
        {

        }

        public override void UpdateBeforeSimulation()
        {
            if (!Initialized)
            {
                Initialize();
            }

            if (!MyAPIGateway.Utilities.IsDedicated)
            {
                UpdateBackpack();
                UpdateInterface();
            }

            if (MyAPIGateway.Multiplayer.IsServer)
            {
                UpdateBackpackPositions();
                UpdateActiveBackpacks();

            }

            if (inventoryOpen)
            {
                if (MyAPIGateway.Gui.GetCurrentScreen == MyTerminalPageEnum.None)
                {
                    UnSetDistance();
                    inventoryOpen = false;
                }
            }
        }

        public override void UpdateAfterSimulation()
        {
            if (MyAPIGateway.Multiplayer.IsServer)
            {
                //UpdateHandtool();
            }
        }

        void SetDistanceServer()
        {
            if (MyAPIGateway.Multiplayer.IsServer)
            {
                MyConstants.DEFAULT_INTERACTIVE_DISTANCE = 100000000000f;
            }

        }

        void SetDistance()
        {
            MyConstants.DEFAULT_INTERACTIVE_DISTANCE = 100000000000f;
        }

        void UnSetDistance()
        {
            MyConstants.DEFAULT_INTERACTIVE_DISTANCE = originalMaxDistance;
        }

        void Initialize()
        {

            if (!HandlersInitialized)
            {
                MyAPIGateway.Multiplayer.RegisterMessageHandler(5890, HandleMessage);

                MyAPIGateway.Session.DamageSystem.RegisterBeforeDamageHandler(0, DamageHandler);

                HandlersInitialized = true;
            }

            if (MyAPIGateway.Session.OnlineMode != MyOnlineModeEnum.OFFLINE)
            {
                SetDistanceServer();//Trying new things
            }


            if (MyAPIGateway.Session.Player != null)
            {
                var player = MyAPIGateway.Session.Player;

                byte[] message = new byte[20];
                byte[] messageID = BitConverter.GetBytes(0);
                byte[] playerID = BitConverter.GetBytes(player.IdentityId);
                byte[] multiplayerId = BitConverter.GetBytes(MyAPIGateway.Multiplayer.MyId);

                for (int i = 0; i < 4; i++)
                {
                    message[i] = messageID[i];
                }

                for (int i = 0; i < 8; i++)
                {
                    message[i + 4] = playerID[i];
                }

                for (int i = 0; i < 8; i++)
                {
                    message[i + 12] = multiplayerId[i];
                }

                MyAPIGateway.Multiplayer.SendMessageToServer(5890, message, true);

                Initialized = true;
            }
        }

        void DamageHandler(object target, ref MyDamageInformation info)
        {
            IMySlimBlock slimBlock = target as IMySlimBlock;

            if (slimBlock != null)
            {
                HandleDamageOnBlock(slimBlock, ref info);
            }

        }

        void HandleDamageOnBlock(IMySlimBlock slimBlock, ref MyDamageInformation info)
        {
            if (
                slimBlock.BlockDefinition.Id.SubtypeName.Equals("CythonSuitAssembler")
                || slimBlock.BlockDefinition.Id.SubtypeName.Equals("CythonSuitRefinery")
                || slimBlock.BlockDefinition.Id.SubtypeName.Equals("CythonSuitFuelConverter")
                || slimBlock.BlockDefinition.Id.SubtypeName.Equals("CythonSuitMedicalRoom")
                || slimBlock.BlockDefinition.Id.SubtypeName.Equals("CythonSuitFuelCell")
                || slimBlock.BlockDefinition.Id.SubtypeName.Equals("CythonSuitRTGReactor")
                || slimBlock.BlockDefinition.Id.SubtypeName.Equals("CythonSuitOxygenGenerator")
            )
            {
                info.Amount = 0;
                info.IsDeformation = false;
            }
        }

        void HandleMessage(byte[] message)
        {
            int id = BitConverter.ToInt32(message, 0);

            if (id == 0)
            {

                long playerID = BitConverter.ToInt64(message, 4);
                ulong sender = BitConverter.ToUInt64(message, 12);

                IMyPlayer player = null;
                List<IMyPlayer> players = new List<IMyPlayer>();

                MyAPIGateway.Players.GetPlayers(players);

                var gridList = new List<IMyCubeGrid>();

                foreach (var allPlayer in players)
                {
                    if (allPlayer.IdentityId == playerID)
                        player = allPlayer;
                }

                /*MyAPIGateway.PrefabManager.SpawnPrefab(
					null, 
					"SuitBackpack", 
					player.GetPosition(), 
					new Vector3(1f,0f, 0f), 
					new Vector3(0f, 1f, 0f), 
					new Vector3(0f, 0f, 0f), 
					new Vector3(0f, 0f, 0f), 
					null, 
					SpawningOptions.DisableSave, 
					player.IdentityId, 
					true
				);*/
                SpawningOptions options = SpawningOptions.SetAuthorship | SpawningOptions.DisableSave;
                long currPlayerID = player.IdentityId;
                MyAPIGateway.PrefabManager.SpawnPrefab(
                    gridList,
                    "SuitBackpack",
                    player.GetPosition(),
                    new Vector3(1f, 0f, 0f),
                    new Vector3(0f, 1f, 0f),
                    new Vector3(0f, 0f, 0f),
                    new Vector3(0f, 0f, 0f),
                    null,
                    SpawningOptions.DisableSave,
                    currPlayerID,
                    true);

            }
            else if (id == 1)
            {

                long RefineryId = BitConverter.ToInt64(message, 4);
                long MedicalRoomId = BitConverter.ToInt64(message, 12);

                SuitRefineryId = RefineryId;
                SuitMedicalRoomId = MedicalRoomId;

                IdsReceived = true;

                //MyAPIGateway.Utilities.ShowNotification("IDs received", 100000, MyFontEnum.Green);
            }

            else if (id == 2)
            {

                long playerId = BitConverter.ToInt64(message, 4);
                long cockpitId = BitConverter.ToInt64(message, 12);

                IMyPlayer player = null;
                List<IMyPlayer> players = new List<IMyPlayer>();

                MyAPIGateway.Players.GetPlayers(players);

                foreach (var allPlayer in players)
                {
                    if (allPlayer.IdentityId == playerId)
                        player = allPlayer;
                }
            }
        }

        void UpdateBackpack()
        {
            if (IdsReceived)
            {
                if (SuitRefinery == null)
                {
                    IMyEntity refinery;

                    if (MyAPIGateway.Entities.TryGetEntityById(SuitRefineryId, out refinery))
                    {
                        if (refinery != null)
                        {
                            SuitRefinery = (IMyFunctionalBlock)refinery;
                        }
                    }
                }

                if (SuitMedicalRoom == null)
                {
                    IMyEntity medicalRoom;

                    if (MyAPIGateway.Entities.TryGetEntityById(SuitMedicalRoomId, out medicalRoom))
                    {
                        if (medicalRoom != null)
                        {
                            SuitMedicalRoom = (IMyFunctionalBlock)medicalRoom;
                        }
                    }
                }

                if (SuitRefinery != null)
                {
                    Vector3D backpackpos = MyAPIGateway.Session.Player.GetPosition();

                    if (MyAPIGateway.Session.Player != null)
                    {
                        if (MyAPIGateway.Session.Player.Controller != null)
                        {
                            if (MyAPIGateway.Session.Player.Controller.ControlledEntity != null)
                            {
                                backpackpos = MyAPIGateway.Session.Player.Controller.ControlledEntity.Entity.PositionComp.GetPosition() + MyAPIGateway.Session.Player.Controller.ControlledEntity.Entity.PositionComp.WorldMatrix.Up * 15f;
                            }
                        }
                    }

                    SuitRefinery.CubeGrid.SetPosition(backpackpos);

                }
            }
        }

        /*
		void UpdateHandtool() {

			if(MyAPIGateway.Multiplayer.IsServer)
			{
				//IMyPlayer player = null;
				List<IMyPlayer> players = new List<IMyPlayer>();

				MyAPIGateway.Players.GetPlayers(players);

				foreach(var player in players)
				{
					if(player.Controller.ControlledEntity != null)
					{
						IMyCharacter character = (player.Controller.ControlledEntity.Entity as IMyCharacter);
						if(character != null)
						{
							IMyHandheldGunObject<MyToolBase> tool = character.EquippedTool as IMyHandheldGunObject<MyToolBase>;
							if(tool != null)
							{
								if(character.SuitEnergyLevel < 0.8f) 
								{
									if(tool.IsShooting)
									{
										tool.EndShoot(MyShootActionEnum.PrimaryAction);
									}
								}
							}
						}
					}
				}
			}
		}
        */

        void UpdateActiveBackpacks()
        {

            List<IMyPlayer> activePlayers = new List<IMyPlayer>();

            MyAPIGateway.Multiplayer.Players.GetPlayers(activePlayers);

            List<IMyPlayer> removedPlayers = new List<IMyPlayer>();

            foreach (var kv in Backpacks)
            {
                if (!activePlayers.Contains(kv.Key))
                {
                    removedPlayers.Add(kv.Key);
                }
            }

            foreach (var removedPlayer in removedPlayers)
            {
                //Backpacks[removedPlayer].Close();
            }

        }

        void UpdateBackpackPositions()
        {
            foreach (var kv in Backpacks)
            {
                Vector3D backpackpos = kv.Key.GetPosition();

                if (kv.Key.Controller != null)
                {
                    if (kv.Key.Controller.ControlledEntity != null)
                    {
                        backpackpos = kv.Key.Controller.ControlledEntity.Entity.PositionComp.GetPosition() + kv.Key.Controller.ControlledEntity.Entity.PositionComp.WorldMatrix.Up * 15f;

                    }
                }

                kv.Value.SetPosition(backpackpos);
            }
        }

        void UpdateInterface()
        {
            if (MyAPIGateway.Gui.GetCurrentScreen == MyTerminalPageEnum.None)
            {
                if (MyAPIGateway.Input.IsKeyPress(VRage.Input.MyKeys.R) && !MyAPIGateway.Input.IsKeyPress(VRage.Input.MyKeys.LeftShift) && MyAPIGateway.Input.IsKeyPress(VRage.Input.MyKeys.LeftControl))
                {
                    if (SuitRefinery != null)
                    {
                        if (MyAPIGateway.Session.Player != null)
                        {
                            if (MyAPIGateway.Session.Player.Controller != null)
                            {
                                if (MyAPIGateway.Session.Player.Controller.ControlledEntity != null)
                                {
                                    if (MyAPIGateway.Session.Player.Controller.ControlledEntity.Entity != null)
                                    {
                                        if (MyAPIGateway.Session.Player.Controller.ControlledEntity.Entity is IMyCharacter)
                                        {
                                            try
                                            {
                                                List<IMyUseObject> useObjects = new List<IMyUseObject>();

                                                (SuitRefinery as MyCubeBlock).UseObjectsComponent.GetInteractiveObjects<IMyUseObject>(useObjects);

                                                if (useObjects.Count > 0)
                                                {
                                                    useObjects[0].Use(UseActionEnum.OpenInventory, MyAPIGateway.Session.Player.Controller.ControlledEntity.Entity);

                                                    SetDistance();

                                                    inventoryOpen = true;
                                                }
                                                else
                                                {
                                                    //MyAPIGateway.Utilities.ShowNotification("NO USEOBJECTS", 5000);
                                                }
                                            }
                                            catch (Exception e)
                                            {
                                                // MyAPIGateway.Utilities.ShowNotification("NULL IN INNER METHOD", 5000);

                                                if (SuitMedicalRoom.Closed)
                                                {
                                                    //MyAPIGateway.Utilities.ShowNotification("Entity CLOSED!", 5000);
                                                }
                                            }
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
                else if (MyAPIGateway.Input.IsKeyPress(VRage.Input.MyKeys.R) && MyAPIGateway.Input.IsKeyPress(VRage.Input.MyKeys.LeftShift) && !MyAPIGateway.Input.IsKeyPress(VRage.Input.MyKeys.LeftControl))
                {
                    if (SuitMedicalRoom != null)
                    {
                        if (MyAPIGateway.Session.Player != null)
                        {
                            if (MyAPIGateway.Session.Player.Controller != null)
                            {
                                if (MyAPIGateway.Session.Player.Controller.ControlledEntity != null)
                                {
                                    if (MyAPIGateway.Session.Player.Controller.ControlledEntity.Entity != null)
                                    {
                                        if (MyAPIGateway.Session.Player.Controller.ControlledEntity.Entity is IMyCharacter)
                                        {

                                            try
                                            {
                                                List<IMyUseObject> useObjects = new List<IMyUseObject>();

                                                (SuitMedicalRoom as MyCubeBlock).UseObjectsComponent.GetInteractiveObjects<IMyUseObject>(useObjects);

                                                if (useObjects.Count > 0)
                                                {
                                                    useObjects[0].Use(UseActionEnum.Manipulate, MyAPIGateway.Session.Player.Controller.ControlledEntity.Entity);
                                                }
                                                else
                                                {
                                                    //MyAPIGateway.Utilities.ShowNotification("NO USEOBJECTS", 5000);
                                                }
                                            }
                                            catch (Exception e)
                                            {
                                                //MyAPIGateway.Utilities.ShowNotification("NULL IN INNER METHOD", 5000);

                                                if (SuitMedicalRoom.Closed)
                                                {
                                                    //MyAPIGateway.Utilities.ShowNotification("Entity CLOSED!", 5000);
                                                }
                                            }

                                        }
                                    }
                                }
                            }
                        }
                    }
                }
            }
        }
    }
}

