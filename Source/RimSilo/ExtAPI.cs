using RimBank.Trade.Ext;
using Verse;

namespace RimBank.Ext.Deposit;

public static class ExtAPI
{
    public static bool isVaultRented => Static.IsVaultRented;

    public static bool isVaultRestricted => Static.IsVaultRestricted;

    public static bool isWarehouseRented => Static.IsWarehouseRented;

    public static bool isWarehouseGetRestricted => Static.IsWarehouseGetRestricted;

    public static bool isWarehousePutRestricted => Static.IsWarehousePutRestricted;

    public static bool isStaticChamberEmpty => Static.IsStaticChamberNull;

    public static bool hasAnyDropPods => Static.dropPodCount > 0;

    public static void PrepareVault(Pawn pawn, bool upOnly = false, bool downOnly = false)
    {
        ExtUtil.PrepareVirtualTrade(pawn, new Trader_Vault(upOnly, downOnly));
    }

    public static void PrepareWarehouse(Pawn pawn, bool upOnly = false, bool downOnly = false)
    {
        ExtUtil.PrepareVirtualTrade(pawn, new Trader_Warehouse(upOnly, downOnly));
    }

    public static void PrepareStaticChamber(Pawn pawn, bool upOnly = false, bool downOnly = false)
    {
        ExtUtil.PrepareVirtualTrade(pawn, new Trader_StaticChamber(upOnly, downOnly));
    }

    public static void PrepareGlobalDropPod(Pawn pawn)
    {
        ExtUtil.PrepareVirtualTrade(pawn, new Trader_GlobalDropPod());
    }
}