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
        protected override char GetValue(ShunterLocoSimulation simulation)
        {
            float temp = simulation.engineTemp.value;
            int tensTemp = Mathf.RoundToInt(temp / 10);
            if (tensTemp < 4)
            {
                return 'c';
            }
            else if (tensTemp > 9)
            {
                return 'H';
            }
            else
            {
                return tensTemp.ToString()[0];
            }
        }

        public override char Typename => 'T';
    }
}
