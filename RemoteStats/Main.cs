using System;
using System.Collections.Generic;
using System.Reflection;
using UnityModManagerNet;
using Harmony12;
using UnityEngine;

namespace DvRemoteStats
{
    public class Main
    {        
        static bool Load(UnityModManager.ModEntry modEntry)
        {
            var harmony = HarmonyInstance.Create(modEntry.Info.Id);

            harmony.PatchAll(Assembly.GetExecutingAssembly());

            return true;
        }
    }

    public interface IValueDisplay<in T> where T : LocoSimulation
    {
        char Typename { get; }

        string GetDisplayString(T simulation);
    }

    public abstract class ValueDisplay<T> : IValueDisplay<T> where T : LocoSimulation
    {
        public string GetDisplayString(T simulation)
        {
            return new string(new[] { Typename, ':', GetValue(simulation) });
        }

        protected abstract char GetValue(T simulation);

        public abstract char Typename { get; }
    }

    public sealed class NullValueDisplay : ValueDisplay<LocoSimulation>
    {
        public static readonly NullValueDisplay Instance = new NullValueDisplay();

        private NullValueDisplay() {}

        public override char Typename => 'X';

        protected override char GetValue(LocoSimulation simulation)
        {
            return 'X';
        }
    }

    public abstract class ValueHolder<T> : ValueDisplay<T> where T : LocoSimulation
    {
        public abstract SimComponent GetComponent(T simulation);

        protected override char GetValue(T simulation)
        {
            SimComponent component = GetComponent(simulation);
            if (component.value < component.min)
            {
                return 'u';
            }
            else if (component.value > component.max)
            {
                return 'o';
            }
            else
            {
                float v = component.value - component.min;
                v /= component.max;
                v *= 10;
                int dec = Mathf.RoundToInt(v);
                if (dec > 9)
                {
                    dec = 9;
                }
                else if (dec < 0)
                {
                    dec = 0;
                }
                return dec.ToString()[0];
            }
        }
    }

    public class StatsReader
    {
        public static readonly ValueDisplay<ShunterLocoSimulation>[] ShunterHolders = new ValueDisplay<ShunterLocoSimulation>[]
        {
            new FuelShunterHolder(),
            new OilShunterHolder(),
            new SandShunterHolder(),
            new TempShunterHolder()
        };
        public static readonly ValueDisplay<DieselLocoSimulation>[] DieselHolders = new ValueDisplay<DieselLocoSimulation>[]
        {
            new FuelDieselHolder(),
            new OilDieselHolder(),
            new SandDieselHolder(),
            new TempDieselHolder()
        };

        public static string GetDisplayString(LocoControllerShunter shunter, int index)
        {
            ShunterLocoSimulation sim = shunter.sim();
            return GetDisplayString(sim, ShunterHolders, index);
        }

        public static string GetDisplayString(LocoControllerDiesel shunter, int index)
        {
            DieselLocoSimulation sim = shunter.sim();
            return GetDisplayString(sim, DieselHolders, index);
        }

        private static string GetDisplayString<T>(T simulation, IValueDisplay<T>[] holders, int index) where T : LocoSimulation
        {
            IValueDisplay<T> display;
            if (index < 0 || index >= holders.Length)
            {
                display = NullValueDisplay.Instance;
            }
            else
            {
                display = holders[index];
            }
            return display.GetDisplayString(simulation);
        }
    }

    [HarmonyPatch(typeof(LocomotiveRemoteController), "UpdateCouplerSelection")]
    class LocomotiveRemoteController_UpdateCouplerSelection_Patch
    {

        static bool Prefix(LocomotiveRemoteController __instance, int delta)
        {
            LocomotiveRemoteControllerAdv adv = __instance.createAdv();
            int numHolders;
            switch (adv.pairedLocomotive)
            {
                case LocoControllerShunter _:
                    numHolders = StatsReader.ShunterHolders.Length;
                    break;
                case LocoControllerDiesel _:
                    numHolders = StatsReader.DieselHolders.Length;
                    break;
                default:
                    numHolders = 0;
                    break;
            }
            if (numHolders > 0)
            {
                adv.selectedCoupler += delta;
                while (adv.selectedCoupler < 0)
                {
                    adv.selectedCoupler += numHolders;
                }
                adv.selectedCoupler %= numHolders;
            }
            else
            {
                adv.selectedCoupler = 0;
            }
            adv.UpdateCouplerDisplay();
            return false;
        }
    }

    [HarmonyPatch(typeof(LocomotiveRemoteController), "OnCoupleButtonPressed")]
    class LocomotiveRemoteController_OnCoupleButtonPressed_Patch
    {
        static bool Prefix()
        {
            return false;
        }
    }

    [HarmonyPatch(typeof(LocomotiveRemoteController), "OnUncoupleButtonPressed")]
    class LocomotiveRemoteController_OnUncoupleButtonPressed_Patch
    {
        static bool Prefix()
        {
            return false;
        }
    }

    [HarmonyPatch(typeof(LocomotiveRemoteController), "UpdateCouplerDisplay")]
    class LocomotiveRemoteController_UpdateCouplerDisplay_Patch
    {
        static bool Prefix(LocomotiveRemoteController __instance)
        {
            LocomotiveRemoteControllerAdv adv = __instance.createAdv();
            bool paired;
            string text;
            switch (adv.pairedLocomotive)
            {
                case LocoControllerShunter shunter:
                    paired = true;
                    text = StatsReader.GetDisplayString(shunter, adv.selectedCoupler);
                    break;
                case LocoControllerDiesel diesel:
                    paired = true;
                    text = StatsReader.GetDisplayString(diesel, adv.selectedCoupler);
                    break;
                default:
                    paired = false;
                    text = "N:A";
                    break;
            }
            __instance.couplerSignDisplay.Display(paired ? "+" : "-");
            __instance.couplerDisplay.Display(text);
            return false;
        }
    }

    ref struct LocomotiveRemoteControllerAdv
    {
        public readonly LocomotiveRemoteController controller;

        private readonly static FieldInfo _pairedLocomotive = AccessTools.Field(typeof(LocomotiveRemoteController), "pairedLocomotive");
        private readonly static FieldInfo _selectedCoupler = AccessTools.Field(typeof(LocomotiveRemoteController), "selectedCoupler");
        private readonly static MethodInfo _UpdateCouplerDisplay = AccessTools.Method(typeof(LocomotiveRemoteController), "UpdateCouplerDisplay");

        public LocomotiveRemoteControllerAdv(LocomotiveRemoteController controller)
        {
            this.controller = controller;
        }

        public int selectedCoupler
        {
            get => (int)_selectedCoupler.GetValue(controller);
            set => _selectedCoupler.SetValue(controller, value);
        }

        public ILocomotiveRemoteControl pairedLocomotive => (ILocomotiveRemoteControl)_pairedLocomotive.GetValue(controller);

        public void UpdateCouplerDisplay()
        {
            _UpdateCouplerDisplay.Invoke(controller, new object[0]);
        }
    }

    static class DieselExtensions
    {
        private readonly static FieldInfo _sim = AccessTools.Field(typeof(LocoControllerDiesel), "sim");

        public static DieselLocoSimulation sim(this LocoControllerDiesel controller)
        {
            return (DieselLocoSimulation)_sim.GetValue(controller);
        }
    }

    static class Fields
    {
        private readonly static FieldInfo _sim = AccessTools.Field(typeof(LocoControllerShunter), "sim");

        public static ShunterLocoSimulation sim(this LocoControllerShunter controller)
        {
            return (ShunterLocoSimulation)_sim.GetValue(controller);
        }

        public static LocomotiveRemoteControllerAdv createAdv(this LocomotiveRemoteController controller)
        {
            return new LocomotiveRemoteControllerAdv(controller);
        }
    }
}
