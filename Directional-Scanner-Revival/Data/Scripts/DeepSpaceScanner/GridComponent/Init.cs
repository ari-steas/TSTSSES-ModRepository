using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using Sandbox.Game.Entities;
using Sandbox.Game.EntityComponents;
using Sandbox.ModAPI;
using Sandbox.ModAPI.Interfaces;
using VRage.Game;
using VRage.Game.Components;
using VRage.Game.ModAPI;
using VRage.ModAPI;
using VRage.ObjectBuilders;

namespace DeepSpaceScanner
{
    [MyEntityComponentDescriptor(typeof(MyObjectBuilder_CubeGrid), false, null)]
    public partial class GridLogic : MyGameLogicComponent
    {
        MyCubeGrid _grid;
        uint _signature;
        static bool _propsCreated;

        public uint Signature => _signature;

        readonly ConcurrentDictionary<IMyPowerProducer, bool> _producers = new ConcurrentDictionary<IMyPowerProducer, bool>();
        readonly ConcurrentDictionary<IMyThrust, bool> _thrusters = new ConcurrentDictionary<IMyThrust, bool>();

        float _gridSize = 1; 

        public override void Close()
        {
            base.Close();
            MyAPIGateway.Session.OnSessionReady -= CreateTerminalProperties;
            _producers.Clear();
            _thrusters.Clear();
            _grid = null;
        }
        
        public override void Init(MyObjectBuilder_EntityBase objectBuilder)
        {
            base.Init(objectBuilder);
            _grid = Entity as MyCubeGrid;
            _gridSize = _grid.GridSize / 2.5f;
            MyAPIGateway.Session.OnSessionReady += CreateTerminalProperties;

            _grid.OnBlockAdded += AddBlock;
            _grid.OnBlockRemoved += b =>
            {
                var f = (b as IMySlimBlock).FatBlock;
                if (f is IMyPowerProducer)
                {
                    bool val;
                    _producers.TryRemove(f as IMyPowerProducer, out val);
                    (f as IMyFunctionalBlock).EnabledChanged -= OnProducerEnabledChanged;
                    (f as IMyFunctionalBlock).IsWorkingChanged -= IsProducerWorkingChanged;
                }
                else if (f is IMyThrust)
                {
                    bool val;
                    (f as IMyFunctionalBlock).EnabledChanged -= OnThrustEnabledChanged;
                    (f as IMyFunctionalBlock).IsWorkingChanged -= IsThrustWorkingChanged;
                    _thrusters.TryRemove(f as IMyThrust, out val);
                }
            };

            var blocks = new List<IMySlimBlock>();
            (_grid as IMyCubeGrid).GetBlocks(blocks);
            blocks.ForEach(AddBlock);

            if (MyAPIGateway.Utilities.IsDedicated) return;
            NeedsUpdate |= MyEntityUpdateEnum.EACH_FRAME;
        }

        void AddBlock(IMySlimBlock b)
        {
            var f = b.FatBlock as IMyFunctionalBlock;
            if (f is IMyPowerProducer)
            {
                f.EnabledChanged += OnProducerEnabledChanged;
                f.IsWorkingChanged += IsProducerWorkingChanged;
                OnProducerEnabledChanged(f);                    
            } 
            else if (f is IMyThrust && (f as IMyThrust).BlockDefinition.SubtypeId.EndsWith("HydrogenThrust"))
            {
                f.EnabledChanged += OnThrustEnabledChanged;
                f.IsWorkingChanged += IsThrustWorkingChanged;
                OnThrustEnabledChanged(f);
            }
        }
        
        void OnProducerEnabledChanged(IMyTerminalBlock block)
        {
            var b = block as IMyPowerProducer;
            bool val;
            if (b.Enabled) _producers.TryAdd(b, true);
            else _producers.TryRemove(b, out val);
        }
        
        void OnThrustEnabledChanged(IMyTerminalBlock block)
        {
            var b = block as IMyThrust;
            bool val;
            if (b.Enabled) _thrusters.TryAdd(b, true);
            else _thrusters.TryRemove(b, out val);
        }

        void IsProducerWorkingChanged(IMyCubeBlock myCubeBlock)
        {
            OnProducerEnabledChanged(myCubeBlock as IMyTerminalBlock);
        }
        
        void IsThrustWorkingChanged(IMyCubeBlock myCubeBlock)
        {
            OnThrustEnabledChanged(myCubeBlock as IMyTerminalBlock);
        }

        static void CreateTerminalProperties()
        {
            if (_propsCreated) return;
            _propsCreated = true;

            var prop = MyAPIGateway.TerminalControls.CreateProperty<uint, IMyTerminalBlock>("Signature");
            prop.Getter = b =>
            {
                var l = b.CubeGrid.GameLogic.GetAs<GridLogic>();
                return l?.Signature ?? (uint) 666;
            };
            MyAPIGateway.TerminalControls.AddControl<Sandbox.ModAPI.Ingame.IMyProgrammableBlock>(prop);
        }
    }
}