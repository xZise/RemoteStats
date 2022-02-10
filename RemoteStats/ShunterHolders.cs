using UnityEngine;

namespace DvRemoteStats
{
    public sealed class FuelShunterHolder : ValueHolder<ShunterLocoSimulation>
    {
        public override SimComponent GetComponent(ShunterLocoSimulation simulation)
        {
            return simulation.fuel;
        }

        public override char Typename => 'F';
    }

    public sealed class OilShunterHolder : ValueHolder<ShunterLocoSimulation>
    {
        public override SimComponent GetComponent(ShunterLocoSimulation simulation)
        {
            return simulation.oil;
        }

        public override char Typename => 'O';
    }

    public sealed class SandShunterHolder : ValueHolder<ShunterLocoSimulation>
    {
        public override SimComponent GetComponent(ShunterLocoSimulation simulation)
        {
            return simulation.sand;
        }

        public override char Typename => 'S';
    }

    public sealed class TempShunterHolder : ValueDisplay<ShunterLocoSimulation>
    {
        protected override string GetValue(ShunterLocoSimulation simulation)
        {
            float temp = simulation.engineTemp.value;
            if (temp < 40)
            {
                return "c";
            }
            else if (temp > 99)
            {
                return "H";
            }
            else
            {
                return StatsReader.FormatValue(temp / 10);
            }
        }

        public override char Typename => 'T';
    }
}
