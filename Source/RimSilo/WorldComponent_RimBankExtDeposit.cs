using RimWorld.Planet;
using Verse;

namespace RimBank.Ext.Deposit;

public class WorldComponent_RimBankExtDeposit : WorldComponent
{
    public WorldComponent_RimBankExtDeposit(World world)
        : base(world)
    {
        Static.InitSetup();
        Log.Message("[RimBankExt.Deposit] Component initialized.");
    }

    public override void ExposeData()
    {
        Static.ExposeData();
    }

    public override void WorldComponentTick()
    {
        if (Static.IsVaultRented && Find.TickManager.TicksAbs >= Static.scheduledPayVaultRentTick)
        {
            Static.CollectRent();
        }

        Static.StaticChamberTick();
    }
}