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
            string prefix = new string(new[] { Typename, ':' });
            return prefix + GetValue(simulation);
        }

        protected abstract string GetValue(T simulation);

        public abstract char Typename { get; }
    }

    public sealed class NullValueDisplay : ValueDisplay<LocoSimulation>
    {
        public static readonly NullValueDisplay Instance = new NullValueDisplay();

        private NullValueDisplay() {}

        public override char Typename => 'X';

        protected override string GetValue(LocoSimulation simulation)
        {
            return "X";
        }
    }

    public abstract class ValueHolder<T> : ValueDisplay<T> where T : LocoSimulation
    {
        public abstract SimComponent GetComponent(T simulation);

        protected override string GetValue(T simulation)
        {
            SimComponent component = GetComponent(simulation);
            if (component.value < component.min)
            {
                return "u";
            }
            else if (component.value > component.max)
            {
                return "o";
            }
            else
            {
                float v = component.value - component.min;
                v /= component.max;
                v *= 10;
                return StatsReader.FormatValue(v);
            }
        }
    }

    public interface IReader
    {
        bool Paired { get; }
        int Count { get; }

        string GetDisplayString(int index);
    }

    public sealed class NoneReader : IReader
    {
        public readonly static NoneReader Instance = new NoneReader();

        private NoneReader() { }

        public bool Paired => false;

        public int Count => 0;

        public string GetDisplayString(int index)
        {
            return "N:A";
        }
    }

    public class Reader<T> : IReader where T : LocoSimulation
    {
        public bool Paired => true;
        public int Count => holders.Count;

        private readonly IReadOnlyList<IValueDisplay<T>> holders;
        private readonly T simulation;

        public Reader(IReadOnlyList<IValueDisplay<T>> holders, T simulation)
        {
            this.holders = holders;
            this.simulation = simulation;
        }

        public string GetDisplayString(int index)
        {
            IValueDisplay<T> display;
            if (index < 0 || index >= holders.Count)
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

    public static class Readers
    {
        private static readonly IReadOnlyList<IValueDisplay<ShunterLocoSimulation>> ShunterHolders = Array.AsReadOnly(new ValueDisplay<ShunterLocoSimulation>[]
        {
            new FuelShunterHolder(),
            new OilShunterHolder(),
            new SandShunterHolder(),
            new TempShunterHolder()
        });
        private static readonly IReadOnlyList<IValueDisplay<DieselLocoSimulation>> DieselHolders = Array.AsReadOnly(new ValueDisplay<DieselLocoSimulation>[]
        {
            new FuelDieselHolder(),
            new OilDieselHolder(),
            new SandDieselHolder(),
            new TempDieselHolder()
        });

        public static IReader GetReader(ILocomotiveRemoteControl locomotive)
        {
            switch (locomotive)
            {
                case LocoControllerShunter shunter: return new Reader<ShunterLocoSimulation>(ShunterHolders, shunter.sim());
                case LocoControllerDiesel diesel: return new Reader<DieselLocoSimulation>(DieselHolders, diesel.sim());
                default: return NoneReader.Instance;
            }
        }
    }

    public class StatsReader
    {
        public static string FormatValue(float value)
        {
            int flooredValue = Mathf.FloorToInt(value);
            if (flooredValue < 0)
            {
                flooredValue++;
            }
            string result = flooredValue.ToString();
            if (value - flooredValue >= 0.5)
            {
                result += ".";
            }
            return result;
        }
    }

    [HarmonyPatch(typeof(LocomotiveRemoteController), "UpdateCouplerSelection")]
    class LocomotiveRemoteController_UpdateCouplerSelection_Patch
    {

        static bool Prefix(LocomotiveRemoteController __instance, int delta)
        {
            LocomotiveRemoteControllerAdv adv = new LocomotiveRemoteControllerAdv(__instance);
            IReader reader = Readers.GetReader(adv.pairedLocomotive);
            int numHolders = reader.Count;
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
            LocomotiveRemoteControllerAdv adv = new LocomotiveRemoteControllerAdv(__instance);
            IReader reader = Readers.GetReader(adv.pairedLocomotive);
            __instance.couplerSignDisplay.Display(reader.Paired ? "+" : "-");
            __instance.couplerDisplay.Display(reader.GetDisplayString(adv.selectedCoupler));
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
    }
}
