using System.Collections.Generic;
using RimWorld;
using Verse;

namespace RimBank.Ext.Deposit;

internal static class Static
{
    public const int MaintainancePeriodTicks = 900000;

    public const int StaticChamberTicks = 420000;

    public const float DropWarehouseChanceLosingWholeStack = 0.65f;

    private const int ExposableVarsCountForCheck = 16;
    private static string CopyrightStr = "RimBankExt.Deposit A17,user19990313,Baidu Tieba&Ludeon forum";

    public static List<Pawn> contentStaticChamber = [];

    public static List<int> ticksStaticChamber = [];

    public static int scheduledNearestStaticChamberExpireTick = -1999;

    public static List<Thing> contentWarehouse = [];

    public static int contentVaultSilver;

    public static int contentVaultBanknote;

    public static int lastPayVaultRentTick = -1999;

    public static int lastPayWarehouseRentTick = -1999;

    public static int scheduledPayVaultRentTick = -1999;

    public static int scheduledPayWarehouseRentTick = -1999;

    private static bool frozenVault;

    private static bool frozenWarehousePut;

    private static bool frozenWarehouseGet;

    public static int extensionsVault;

    public static int extensionsWarehouse;

    public static int dropPodCount;

    public static bool IsVaultRented => scheduledPayVaultRentTick > 0;

    public static bool IsWarehouseRented => scheduledPayWarehouseRentTick > 0;

    public static bool IsStaticChamberNull => scheduledNearestStaticChamberExpireTick < 0;

    public static bool IsWarehouseRestricted => frozenWarehouseGet || frozenWarehousePut;

    public static bool IsWarehouseGetRestricted => frozenWarehouseGet;

    public static bool IsWarehousePutRestricted => frozenWarehousePut;

    public static bool IsVaultRestricted => frozenVault;

    public static void ExposeData()
    {
        Scribe_Values.Look(ref scheduledPayVaultRentTick, "schedVault", -1999);
        if (IsVaultRented)
        {
            Scribe_Values.Look(ref contentVaultBanknote, "notes");
            Scribe_Values.Look(ref contentVaultSilver, "silver");
            Scribe_Values.Look(ref lastPayVaultRentTick, "lastVaultRentTick", -1999);
            Scribe_Values.Look(ref frozenVault, "frozen");
            Scribe_Values.Look(ref extensionsVault, "extVault");
        }

        Scribe_Values.Look(ref scheduledPayWarehouseRentTick, "schedWarehouse", -1999);
        if (IsWarehouseRented)
        {
            Scribe_Collections.Look(ref contentWarehouse, "contents", LookMode.Deep);
            Scribe_Values.Look(ref lastPayWarehouseRentTick, "lastWarehouseRentTick", -1999);
            Scribe_Values.Look(ref frozenWarehouseGet, "frozenGet");
            Scribe_Values.Look(ref frozenWarehousePut, "frozenPut");
            Scribe_Values.Look(ref extensionsWarehouse, "extWarehouse");
        }

        Scribe_Values.Look(ref scheduledNearestStaticChamberExpireTick, "nearestChamberExpire", -1999);
        if (!IsStaticChamberNull)
        {
            Scribe_Collections.Look(ref contentStaticChamber, "chambers", LookMode.Deep);
            Scribe_Collections.Look(ref ticksStaticChamber, "ticks");
        }

        Scribe_Values.Look(ref dropPodCount, "pods");
        CapacityExpansion.EnsureExpansionInBound();
    }

    public static void InitSetup()
    {
        contentStaticChamber = [];
        ticksStaticChamber = [];
        scheduledNearestStaticChamberExpireTick = -1999;
        contentWarehouse = [];
        contentVaultSilver = 0;
        contentVaultBanknote = 0;
        lastPayVaultRentTick = -1999;
        lastPayWarehouseRentTick = -1999;
        scheduledPayVaultRentTick = -1999;
        scheduledPayWarehouseRentTick = -1999;
        frozenVault = false;
        frozenWarehousePut = false;
        frozenWarehouseGet = false;
        extensionsWarehouse = 0;
        extensionsVault = 0;
        dropPodCount = 0;
    }

    public static void CollectRent()
    {
        if (!IsVaultRented)
        {
            return;
        }

        string text = "LineRentReportHeader".Translate() + "\n";
        var text2 = "";
        var dropped = true;
        if (IsWarehouseRented && IsWarehouseRestricted)
        {
            var deprived = false;
            if (frozenWarehouseGet)
            {
                text2 = "LineWarehouseDeprived".Translate();
                deprived = true;
            }

            if (frozenWarehousePut && contentWarehouse.Any())
            {
                text2 = $"{text2}\n" + "LineWarehouseDropped".Translate();
                DropWarehouse();
                dropped = false;
            }

            if (deprived)
            {
                ResetWarehouse();
            }
        }

        if (frozenVault)
        {
            if (contentVaultSilver != 0 || contentVaultBanknote != 0)
            {
                text2 = "LineVaultDropped".Translate() + "\n" + text2;
                DropVault();
            }

            text2 = "LineVaultDeprived".Translate() + "\n" + text2;
            ResetVault();
            Find.LetterStack.ReceiveLetter("LetterTitleViolation".Translate(), text + text2, LetterDefOf.ThreatSmall);
            return;
        }

        var vaultRent = Utility.VaultRent;
        var warehouseRent = Utility.WarehouseRent;
        var warehouseMaintainanceFee = Utility.WarehouseMaintainanceFee;
        var num = 0;
        var num2 = Utility.CalculateVaultUsage();
        var text3 = "";
        var text4 = "";
        if (num2 >= vaultRent)
        {
            num += vaultRent;
            text3 = "LineRentdVault".Translate(vaultRent) + "\n";
            if (IsWarehouseRented && scheduledPayWarehouseRentTick <= Find.TickManager.TicksAbs)
            {
                if (num2 >= num + warehouseRent)
                {
                    num += warehouseRent;
                    text3 = text3 + "LineRentdWarehouse".Translate(warehouseRent) + "\n";
                    if (dropped && num2 >= num + warehouseMaintainanceFee)
                    {
                        num += warehouseMaintainanceFee;
                        text3 = text3 + "LineRentdMaintainanceFee".Translate(warehouseMaintainanceFee) +
                                "\n";
                    }
                    else
                    {
                        text4 = "LineUnRentdMaintainanceFee".Translate(warehouseMaintainanceFee) + "\n";
                        frozenWarehousePut = true;
                    }
                }
                else
                {
                    text4 = "LineUnRentdWarehouse".Translate(warehouseRent) + "\n";
                    if (warehouseMaintainanceFee > 0)
                    {
                        text4 = text4 + "LineUnRentdMaintainanceFee".Translate(warehouseMaintainanceFee) +
                                "\n";
                    }

                    frozenWarehouseGet = true;
                    frozenWarehousePut = true;
                }
            }
        }
        else
        {
            text4 = "LineUnRentdVault".Translate(vaultRent) + "\n";
            frozenVault = true;
            if (IsWarehouseRented)
            {
                text4 = text4 + "LineUnRentdWarehouse".Translate(warehouseRent) + "\n";
                if (warehouseMaintainanceFee > 0)
                {
                    text4 = text4 + "LineUnRentdMaintainanceFee".Translate(warehouseMaintainanceFee) + "\n";
                }

                frozenWarehouseGet = true;
                frozenWarehousePut = true;
            }
        }

        var pair = Utility.SimulateExchange(contentVaultSilver, contentVaultBanknote, num);
        contentVaultBanknote = pair.First;
        contentVaultSilver = pair.Second;
        ScheduleNext();
        string text5 = "LetterTitleNorm".Translate();
        text += text3;
        var letterDef = LetterDefOf.NeutralEvent;
        if (!text4.NullOrEmpty())
        {
            text5 = "LetterTitleWarn".Translate();
            text = $"{text}\n{text4}";
            letterDef = LetterDefOf.NegativeEvent;
        }

        if (!text2.NullOrEmpty())
        {
            text5 = "LetterTitleViolation".Translate();
            text = $"{text}\n{text2}";
            letterDef = LetterDefOf.ThreatSmall;
        }

        if (!text4.NullOrEmpty())
        {
            text = $"{text}\n\n" + "LinePayFineTail".Translate();
        }

        Find.LetterStack.ReceiveLetter(text5, text, letterDef);
        Log.Message($"[RimBank.Ext] Rent collected at Tick {Find.TickManager.TicksAbs}");
    }

    public static void RentVault()
    {
        lastPayVaultRentTick = -1999;
        scheduledPayVaultRentTick = Find.TickManager.TicksAbs + 900000;
    }

    public static void RentWarehouse()
    {
        lastPayWarehouseRentTick = -1999;
        scheduledPayWarehouseRentTick = scheduledPayVaultRentTick;
    }

    private static void ScheduleNext()
    {
        if (IsVaultRented)
        {
            lastPayVaultRentTick = Find.TickManager.TicksAbs;
            scheduledPayVaultRentTick = lastPayVaultRentTick + 900000;
        }

        if (!IsWarehouseRented)
        {
            return;
        }

        lastPayWarehouseRentTick = Find.TickManager.TicksAbs;
        scheduledPayWarehouseRentTick = lastPayWarehouseRentTick + 900000;
    }

    public static void DropVault()
    {
        contentVaultBanknote = 0;
        contentVaultSilver = 0;
    }

    public static void DropWarehouse()
    {
        if (contentWarehouse.Count == 0)
        {
            return;
        }

        var list = new List<Thing>();
        foreach (var item in contentWarehouse)
        {
            if (Rand.Chance(0.65f))
            {
                continue;
            }

            if (item.stackCount > 1)
            {
                item.stackCount = Rand.Range(1, item.stackCount + 1);
            }

            list.Add(item);
        }

        Utility.TryDeliverThings(list);
        contentWarehouse = [];
        Utility.Recache();
    }

    public static void ResetVault()
    {
        lastPayVaultRentTick = -1999;
        scheduledPayVaultRentTick = -1999;
        frozenVault = false;
    }

    public static void ResetWarehouse()
    {
        lastPayWarehouseRentTick = -1999;
        scheduledPayWarehouseRentTick = -1999;
        frozenWarehouseGet = false;
        frozenWarehousePut = false;
    }

    public static void UnFreeze()
    {
        frozenVault = false;
        frozenWarehouseGet = false;
        frozenWarehousePut = false;
    }

    public static void EnterStaticChamber(Pawn pawn)
    {
        if (contentStaticChamber.Count != ticksStaticChamber.Count)
        {
            Log.ErrorOnce(
                "StaticChamber pawns count doesnt equal to ticks count.That means the storage is affected and bugged.",
                330671);
        }

        if (contentStaticChamber.Count == 0)
        {
            scheduledNearestStaticChamberExpireTick = Find.TickManager.TicksAbs + 420000;
        }

        contentStaticChamber.Add(pawn);
        ticksStaticChamber.Add(Find.TickManager.TicksAbs);
    }

    public static void ExitStaticChamber(Pawn pawn)
    {
        for (var i = 0; i < contentStaticChamber.Count; i++)
        {
            if (contentStaticChamber[i] != pawn)
            {
                continue;
            }

            contentStaticChamber.RemoveAt(i);
            ticksStaticChamber.RemoveAt(i);
            if (i != 0)
            {
                return;
            }

            if (contentStaticChamber.Count == 0)
            {
                scheduledNearestStaticChamberExpireTick = -1999;
            }
            else
            {
                scheduledNearestStaticChamberExpireTick = ticksStaticChamber[0] + 420000;
            }

            return;
        }

        Log.Error("Tried to eject a chamber for pawn " + pawn.NameShortColored + ",but it is not here.");
    }

    public static void StaticChamberTick()
    {
        if (scheduledNearestStaticChamberExpireTick < 0 ||
            scheduledNearestStaticChamberExpireTick > Find.TickManager.TicksAbs)
        {
            return;
        }

        var list = new List<Thing>();
        var pawn = contentStaticChamber[0];
        ExitStaticChamber(pawn);
        list.Add(pawn);
        while (scheduledNearestStaticChamberExpireTick > 0 &&
               scheduledNearestStaticChamberExpireTick < Find.TickManager.TicksAbs + 60)
        {
            pawn = contentStaticChamber[0];
            ExitStaticChamber(pawn);
            list.Add(pawn);
        }

        Utility.TryDeliverThings(list);
        Messages.Message("MsgChamberContentBack".Translate(), MessageTypeDefOf.SilentInput);
    }

    public static void TryMessageBoxRestrictedPermissionWarehouse()
    {
        if (!IsWarehouseRestricted)
        {
            return;
        }

        string text = frozenWarehouseGet ? "TipArrowGet".Translate() : "";
        Find.WindowStack.Add(new Dialog_MessageBox("DlgWarehouseLockAccess".Translate("TipArrowPut".Translate(), text),
            null, null, null, null,
            "DlgTitleLockAccess".Translate()));
    }

    public static void TryMessageBoxRestrictedPermissionVault()
    {
        if (IsVaultRestricted)
        {
            Find.WindowStack.Add(new Dialog_MessageBox("DlgVaultLockAccess".Translate(), null, null, null, null,
                "DlgTitleLockAccess".Translate()));
        }
    }

    public static void MessageRestrictedPermissionWarehouse()
    {
        if (frozenWarehouseGet)
        {
            Messages.Message("MsgGetLocked".Translate(), MessageTypeDefOf.RejectInput);
        }

        if (frozenWarehousePut)
        {
            Messages.Message("MsgPutLocked".Translate(), MessageTypeDefOf.RejectInput);
        }
    }

    public static void MessageRestrictedPermissionVault()
    {
        Messages.Message("MsgVaultLocked".Translate(), MessageTypeDefOf.RejectInput);
    }

    internal static void DestroyAnyContents()
    {
        var list = new List<Thing>();
        foreach (var item in contentStaticChamber)
        {
            list.Add(item);
        }

        Utility.TryDeliverThings(list);
        Utility.TryDeliverThings(contentWarehouse);
        InitSetup();
    }
}