namespace DvRemoteStats
{
    public sealed class FuelDieselHolder : ValueHolder<DieselLocoSimulation>
    {
        public override SimComponent GetComponent(DieselLocoSimulation simulation)
        {
            return simulation.fuel;
        }

        public override char Typename => 'F';
    }

    public sealed class OilDieselHolder : ValueHolder<DieselLocoSimulation>
    {
        public override SimComponent GetComponent(DieselLocoSimulation simulation)
        {
            return simulation.oil;
        }

        public override char Typename => 'O';
    }

    public sealed class SandDieselHolder : ValueHolder<DieselLocoSimulation>
    {
        public override SimComponent GetComponent(DieselLocoSimulation simulation)
        {
            return simulation.sand;
        }

        public override char Typename => 'S';
    }

    public sealed class TempDieselHolder : ValueHolder<DieselLocoSimulation>
    {
        public override SimComponent GetComponent(DieselLocoSimulation simulation)
        {
            return simulation.engineTemp;
        }

        public override char Typename => 'T';
    }
}
