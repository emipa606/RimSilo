using Verse;

namespace RimBank.Ext.Deposit;

internal static class CapacityExpansion
{
    public static int VaultBaseCapacity => 20000;

    public static int VaultBaseRent => 100;

    public static int VaultCapacityPerUnit => 10000;

    public static int VaultRentPerUnit => 50;

    public static int VaultSubscriptionPerUnit => 850;

    public static int VaultMaxCapacity => 230000;

    public static int WarehouseBaseCapacity => 3000;

    public static int WarehouseBaseRent => 100;

    public static int WarehouseCapacityPerUnit => 1000;

    public static int WarehouseRentPerUnit => 25;

    public static int WarehouseSubscriptonPerUnit => 800;

    public static int WarehouseMaxCapacity => 15000;

    public static int WarehouseExpansionSlotsCount =>
        (WarehouseMaxCapacity - WarehouseBaseCapacity) / WarehouseCapacityPerUnit;

    public static int VaultExpansionSlotsCount => (VaultMaxCapacity - VaultBaseCapacity) / VaultCapacityPerUnit;

    public static int VaultRent => VaultBaseRent + (Static.extensionsVault * VaultRentPerUnit);

    public static int WarehouseRent => WarehouseBaseRent + (Static.extensionsWarehouse * WarehouseRentPerUnit);

    public static int VaultCapacity => VaultBaseCapacity + (Static.extensionsVault * VaultCapacityPerUnit);

    public static int WarehouseCapacity =>
        WarehouseBaseCapacity + (Static.extensionsWarehouse * WarehouseCapacityPerUnit);

    public static void EnsureExpansionInBound()
    {
        if (Static.extensionsVault < 0 ||
            VaultBaseCapacity + (Static.extensionsVault * VaultCapacityPerUnit) > VaultMaxCapacity)
        {
            Log.Error("Vault capacity expansion is out of bound.Setting to default...");
            Static.extensionsVault = 0;
        }

        if (Static.extensionsWarehouse >= 0 &&
            WarehouseBaseCapacity + (Static.extensionsWarehouse * WarehouseCapacityPerUnit) <= WarehouseMaxCapacity)
        {
            return;
        }

        Log.Error("Warehouse capacity expansion is out of bound.Setting to default...");
        Static.extensionsWarehouse = 0;
    }
}