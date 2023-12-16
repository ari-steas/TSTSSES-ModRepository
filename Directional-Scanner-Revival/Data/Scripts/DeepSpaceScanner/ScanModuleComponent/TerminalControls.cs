using System;
using System.Collections.Generic;
using System.Text;
using Sandbox.Game.Entities;
using Sandbox.ModAPI;
using Sandbox.ModAPI.Interfaces.Terminal;
using Scanner.Data.Scripts.DeepSpaceScanner;
using VRage;
using VRage.Game.ModAPI;
using VRage.ModAPI;
using VRage.Utils;

namespace DeepSpaceScanner
{
    public partial class ScanLogic
    {
        static bool _controlsCreated;

        static void CreateTerminalControls()
        {
            if (MyAPIGateway.Utilities.IsDedicated || _controlsCreated) return;
            _controlsCreated = true;


            Func<IMyTerminalBlock, bool> enabled = b => !ModComponent.ScanActive;
            Func<IMyTerminalBlock, bool> visible = b => b.BlockDefinition.SubtypeId.StartsWith("DeepSpaceScanner");
            Action<IMyTerminalBlock> startScan = ModComponent.SendRequest;
            
            if (ModConfig.AsteroidScanMaxDistance > 0)
            {
                var ctrl = MyAPIGateway.TerminalControls.CreateControl<IMyTerminalControlOnOffSwitch, IMyCameraBlock>("ScanMode");
                ctrl.Title = MyStringId.GetOrCompute("Scan Mode");
                ctrl.Visible = visible;
                ctrl.OnText = MyStringId.GetOrCompute("Grids");
                ctrl.OffText = MyStringId.GetOrCompute("Ores");
                ctrl.Getter = b =>
                {
                    var l = b.GameLogic.GetAs<ScanLogic>();
                    return l == null || !l.ScanAsteroids;
                };
                ctrl.Setter = (b, v) =>
                {
                    var l = b.GameLogic.GetAs<ScanLogic>();
                    if (l == null) return;
                    l.ScanAsteroids = !v;
                    l.SaveBlockSettings();
                    RefreshCustomInfo(b);
                };
                MyAPIGateway.TerminalControls.AddControl<IMyCameraBlock>(ctrl);
            }

            List<IMyTerminalControl> controls;
            MyAPIGateway.TerminalControls.GetControls<IMyCameraBlock>(out controls);
            foreach (var control in controls)
            {
                if (control.Id != "View") continue;
                var old = control.Visible;
                control.Visible = b => !b.BlockDefinition.SubtypeId.StartsWith("DeepSpaceScanner") && old(b);
            }

            List<IMyTerminalAction> actions;
            MyAPIGateway.TerminalControls.GetActions<IMyCameraBlock>(out actions);
            foreach (var action in actions)
            {
                if (action.Id != "View") continue;
                var old = action.Enabled;
                action.Enabled = b => !b.BlockDefinition.SubtypeId.StartsWith("DeepSpaceScanner") && old(b);
            }

            var popup = MyAPIGateway.TerminalControls.CreateControl<IMyTerminalControlOnOffSwitch, IMyCameraBlock>("ShowPopup");
            popup.Title = MyStringId.GetOrCompute("Results Popup");
            popup.Visible = visible;
            popup.OnText = MyStringId.GetOrCompute("Show");
            popup.OffText = MyStringId.GetOrCompute("Hide");
            popup.Getter = b =>
            {
                var l = b.GameLogic.GetAs<ScanLogic>();
                return l != null && l.ShowPopup;
            };
            popup.Setter = (b, v) =>
            {
                var l = b.GameLogic.GetAs<ScanLogic>();
                if (l == null) return;
                l.ShowPopup = v;
                l.SaveBlockSettings();
            };
            MyAPIGateway.TerminalControls.AddControl<IMyCameraBlock>(popup);

            var b0 = MyAPIGateway.TerminalControls.CreateControl<IMyTerminalControlButton, IMyCameraBlock>("Scan");
            b0.Title = MyStringId.GetOrCompute("Start Scan");
            b0.Visible = visible;
            b0.Action = startScan;
            b0.Enabled = b =>
            {
                var l = b.GameLogic.GetAs<ScanLogic>();
                if (l == null) return false;
                return enabled(b) && l.Sink.IsPowerAvailable(ModConfig.E, l.Consumption);
            };
            MyAPIGateway.TerminalControls.AddControl<IMyCameraBlock>(b0);

            var s0 = MyAPIGateway.TerminalControls.CreateControl<IMyTerminalControlSlider, IMyCameraBlock>("Scan_Str");
            s0.Title = MyStringId.GetOrCompute("Sensor Strength");
            s0.Visible = visible;
            s0.Enabled = enabled;
            s0.SetLimits(0, 100);
            s0.Getter = b => b.GameLogic.GetAs<ScanLogic>().ScannerStrength;
            s0.Setter = (b, v) =>
            {
                var l = b.GameLogic.GetAs<ScanLogic>();
                if (l == null) return;
                l.ScannerStrength = v;
                RefreshCustomInfo(b);
                l.SaveBlockSettings();
            };
            s0.Writer = (b, v) => v.Append($"{b.GameLogic.GetAs<ScanLogic>()?.ScannerStrength:n1}%");
            MyAPIGateway.TerminalControls.AddControl<IMyCameraBlock>(s0);

            var r1 = MyAPIGateway.TerminalControls.CreateControl<IMyTerminalControlListbox, IMyCameraBlock>("Scan_Results");
            r1.Title = MyStringId.GetOrCompute("Scan Results");
            r1.Visible = visible;
            r1.Enabled = b => b.GameLogic.GetAs<ScanLogic>()?.ScanResults.Count > 0;
            r1.VisibleRowsCount = 8;
            r1.Multiselect = false;
            IMyTerminalControlButton b1 = null;
            r1.ListContent = (b, list, arg3) =>
            {
                list.Clear();
                var l = b.GameLogic.GetAs<ScanLogic>();
                if (l == null) return;
                l.SelectedResult = null;
                l.ScanResults
                    .ForEach(x =>
                        list.Add(new MyTerminalControlListBoxItem(MyStringId.GetOrCompute(x.Name ?? "Unknown"), MyStringId.GetOrCompute(x.ToString()), x)));
                b1?.UpdateVisual();
            };
            r1.ItemSelected = (b, list) =>
            {
                var l = b.GameLogic.GetAs<ScanLogic>();
                if (l != null) l.SelectedResult = list.Count > 0 && (list[0].UserData as ScanResult).Location != null ? list[0].UserData as ScanResult : null;
                b1?.UpdateVisual();
            };
            MyAPIGateway.TerminalControls.AddControl<IMyCameraBlock>(r1);

            b1 = MyAPIGateway.TerminalControls.CreateControl<IMyTerminalControlButton, IMyCameraBlock>("ScanGPS");
            b1.Title = MyStringId.GetOrCompute("Create GPS");
            b1.Visible = visible;
            b1.Enabled = b => b.GameLogic.GetAs<ScanLogic>()?.SelectedResult != null;
            b1.Action = b =>
            {
                var r = b.GameLogic.GetAs<ScanLogic>()?.SelectedResult;
                if (r == null) return;
                var gps = MyAPIGateway.Session.GPS.Create($"{r.Name} ({r.Size})", r.ToString(), r.Location, true, true);
                MyAPIGateway.Session.GPS.AddGps(MyAPIGateway.Session.Player.IdentityId, gps);
            };
            MyAPIGateway.TerminalControls.AddControl<IMyCameraBlock>(b1);

            var s1 = MyAPIGateway.TerminalControls.CreateControl<IMyTerminalControlSlider, IMyCameraBlock>("ScanPitch");
            s1.Title = MyStringId.GetOrCompute("Pitch");
            s1.Visible = visible;
            s1.Enabled = enabled;
            s1.SetLimits(-20, 200);
            s1.Getter = b => b.GameLogic.GetAs<ScanLogic>().NextPitch;
            s1.Setter = (b, v) =>
            {
                var l = b.GameLogic.GetAs<ScanLogic>();
                if (l == null) return;
                l.NextPitch = v;
                l.SaveBlockSettings();
            };
            s1.Writer = (b, v) => v.Append($"{b.GameLogic.GetAs<ScanLogic>()?.NextPitch:n0}");
            MyAPIGateway.TerminalControls.AddControl<IMyCameraBlock>(s1);

            var s2 = MyAPIGateway.TerminalControls.CreateControl<IMyTerminalControlSlider, IMyCameraBlock>("ScanYaw");
            s2.Title = MyStringId.GetOrCompute("Yaw");
            s2.Visible = visible;
            s2.Enabled = enabled;
            s2.SetLimits(-180, 180);
            s2.Getter = b => b.GameLogic.GetAs<ScanLogic>().NextYaw;
            s2.Setter = (b, v) =>
            {
                var l = b.GameLogic.GetAs<ScanLogic>();
                if (l == null) return;
                l.NextYaw = v;
                l.SaveBlockSettings();
            };
            s2.Writer = (b, v) => v.Append($"{b.GameLogic.GetAs<ScanLogic>()?.NextYaw:n0}");
            MyAPIGateway.TerminalControls.AddControl<IMyCameraBlock>(s2);

            var p = MyAPIGateway.Utilities.GamePaths.ContentPath;
            var icon = $@"{p}\Textures\GUI\Icons\Actions\SwitchOn.dds";
            var a = MyAPIGateway.TerminalControls.CreateAction<IMyCameraBlock>("ScanOn");
            a.Name = new StringBuilder("Start Scan");
            a.Icon = icon;
            a.ValidForGroups = true;
            a.Action = startScan;
            a.Writer = (b, builder) =>
            {
                var l = b.GameLogic.GetAs<ScanLogic>();
                if (l == null) return;
                var v = Math.Max(0, ModConfig.ScanDuration - MyAPIGateway.Session.ElapsedPlayTime.TotalMilliseconds + l._scanStarted) / 1000;
                builder.Clear().Append(v > 0 ? $"{v:n2}" : "Scan");
            };
            MyAPIGateway.TerminalControls.AddAction<IMyCameraBlock>(a);
            
            icon = $@"{p}\Textures\GUI\Icons\Actions\Decrease.dds";
            a = MyAPIGateway.TerminalControls.CreateAction<IMyCameraBlock>("ScanDecreasePitch");
            a.Name = new StringBuilder("Decrease Pitch");
            a.Icon = icon;
            a.ValidForGroups = true;
            a.Action = b =>
            {
                var l = b.GameLogic.GetAs<ScanLogic>();
                if (l == null) return;
                l.NextPitch = (float) Math.Round(Math.Max(-20, l.NextPitch - 1));
            };
            a.Enabled = enabled;
            a.Writer = (b, builder) =>
            {
                var l = b.GameLogic.GetAs<ScanLogic>();
                if (l == null) return;
                builder.Clear().Append(l.NextPitch.ToString());
            };
            MyAPIGateway.TerminalControls.AddAction<IMyCameraBlock>(a);
            
            icon = $@"{p}\Textures\GUI\Icons\Actions\Increase.dds";
            a = MyAPIGateway.TerminalControls.CreateAction<IMyCameraBlock>("ScanIncreasePitch");
            a.Name = new StringBuilder("Increase Pitch");
            a.Icon = icon;
            a.ValidForGroups = true;
            a.Action = b =>
            {
                var l = b.GameLogic.GetAs<ScanLogic>();
                if (l == null) return;
                l.NextPitch = (float) Math.Round(Math.Min(90, l.NextPitch + 1));
            };
            a.Enabled = enabled;
            a.Writer = (b, builder) =>
            {
                var l = b.GameLogic.GetAs<ScanLogic>();
                if (l == null) return;
                builder.Clear().Append(l.NextPitch.ToString());
            };
            MyAPIGateway.TerminalControls.AddAction<IMyCameraBlock>(a);
            
            icon = $@"{p}\Textures\GUI\Icons\Actions\Decrease.dds";
            a = MyAPIGateway.TerminalControls.CreateAction<IMyCameraBlock>("ScanDecreaseYaw");
            a.Name = new StringBuilder("Decrease Yaw");
            a.Icon = icon;
            a.ValidForGroups = true;
            a.Action = b =>
            {
                var l = b.GameLogic.GetAs<ScanLogic>();
                if (l == null) return;
                l.NextYaw = (float) Math.Round(Math.Max(-90, l.NextYaw - 1));
            };
            a.Enabled = enabled;
            a.Writer = (b, builder) =>
            {
                var l = b.GameLogic.GetAs<ScanLogic>();
                if (l == null) return;
                builder.Clear().Append(l.NextYaw.ToString());
            };
            MyAPIGateway.TerminalControls.AddAction<IMyCameraBlock>(a);
            
            icon = $@"{p}\Textures\GUI\Icons\Actions\Increase.dds";
            a = MyAPIGateway.TerminalControls.CreateAction<IMyCameraBlock>("ScanIncreaseYaw");
            a.Name = new StringBuilder("Increase Yaw");
            a.Icon = icon;
            a.ValidForGroups = true;
            a.Action = b =>
            {
                var l = b.GameLogic.GetAs<ScanLogic>();
                if (l == null) return;
                l.NextYaw = (float) Math.Round(Math.Min(90, l.NextYaw + 1));
            };
            a.Enabled = enabled;
            a.Writer = (b, builder) =>
            {
                var l = b.GameLogic.GetAs<ScanLogic>();
                if (l == null) return;
                builder.Clear().Append(l.NextYaw.ToString());
            };
            MyAPIGateway.TerminalControls.AddAction<IMyCameraBlock>(a);
            
            var lcdSelector = MyAPIGateway.TerminalControls.CreateControl<IMyTerminalControlListbox, IMyCameraBlock>("ScanResultsLCD");
            lcdSelector.Title = MyStringId.GetOrCompute("Output results to");
            lcdSelector.Visible = visible;
            lcdSelector.VisibleRowsCount = 6;
            lcdSelector.ListContent = (b, list, arg3) =>
            {
                try
                {
                    list.Clear();
                    var str = MyStringId.GetOrCompute("");
                    list.Add(new MyTerminalControlListBoxItem(str, str, null));
                    var l = b.GameLogic.GetAs<ScanLogic>();
                    if (l == null) return;
                    foreach (var grid in MyAPIGateway.GridGroups.GetGroup(b.CubeGrid, GridLinkTypeEnum.Mechanical))
                    {
                        foreach (var block in (grid as MyCubeGrid).GetFatBlocks())
                        {
                            if (!(block is IMyTextSurfaceProvider)) continue;
                            var provider = (IMyTextSurfaceProvider) block;
                            for (var i = 0; i < provider.SurfaceCount; i++)
                            {
                                var s = provider.GetSurface(i);
                                str = MyStringId.GetOrCompute($"{(block as IMyTerminalBlock).CustomName} {s.DisplayName}");
                                list.Add(new MyTerminalControlListBoxItem(str, str, MyTuple.Create(block.EntityId, i)));
                            }
                        }                        
                    }
                }
                catch (Exception e)
                {
                    Log.Error(e);
                }
            };
            lcdSelector.ItemSelected = (b, list) =>
            {
                var l = b.GameLogic.GetAs<ScanLogic>();
                if (l == null || list.Count == 0) return;
                l.TextSurface = (MyTuple<long, int>?) list[0].UserData ?? default(MyTuple<long, int>);
                l.SaveBlockSettings();
            };
            MyAPIGateway.TerminalControls.AddControl<IMyCameraBlock>(lcdSelector);
        }
    }
}