using RimBank.Core.Interactive;
using UnityEngine;
using Verse;

namespace RimBank.Ext.Deposit;

[StaticConstructorOnStartup]
internal class StaticConstructor
{
    public static readonly Texture2D TexArrowPut;

    public static readonly Texture2D TexArrowGet;

    public static readonly Texture2D TexLockAccess;

    public static readonly Texture2D TexPin;

    public static readonly Texture2D TexIncrease;

    public static readonly Texture2D TexDecrease;

    public static readonly Texture2D TexLEDBase;

    public static readonly Texture2D TexRedCrossDiag;

    public static readonly Texture2D TexIncOrDec;

    public static readonly Texture2D TexInfo;

    public static readonly Texture2D TexCalc;

    public static readonly Texture2D TexBill;

    public static readonly Texture2D TargeterMouseAttachment;

    public static readonly Texture2D FillableTexEmptySlot;

    public static readonly Texture2D FillableTexOccupiedSlot;

    static StaticConstructor()
    {
        TexArrowPut = ContentFinder<Texture2D>.Get("UI/Put");
        TexArrowGet = ContentFinder<Texture2D>.Get("UI/Get");
        TexLockAccess = ContentFinder<Texture2D>.Get("UI/Lock");
        TexPin = ContentFinder<Texture2D>.Get("UI/Pin");
        TexIncrease = ContentFinder<Texture2D>.Get("UI/Increase");
        TexDecrease = ContentFinder<Texture2D>.Get("UI/Decrease");
        TexLEDBase = ContentFinder<Texture2D>.Get("UI/LEDBase");
        TexRedCrossDiag = ContentFinder<Texture2D>.Get("UI/RedCrossDiag");
        TexIncOrDec = ContentFinder<Texture2D>.Get("UI/IncOrDec");
        TexInfo = ContentFinder<Texture2D>.Get("UI/Info");
        TexCalc = ContentFinder<Texture2D>.Get("UI/Calc");
        TexBill = ContentFinder<Texture2D>.Get("UI/Bill");
        TargeterMouseAttachment = ContentFinder<Texture2D>.Get("UI/Overlays/LaunchableMouseAttachment");
        FillableTexEmptySlot = TexUI.GrayTextBG;
        FillableTexOccupiedSlot = SolidColorMaterials.NewSolidColorTexture(new Color(1f, 1f, 1f, 0.6f));
        FloatMenuManager.Add("RimBankExtDepositFloatMenuEntryLabel".Translate(),
            delegate(Pawn pawn) { Find.WindowStack.Add(new Dialog_AccountCtrl(pawn)); }, true);
#if DEBUG
            FloatMenuManager.Add("Open Vault",
                delegate(Pawn pawn) { ExtUtil.PrepareVirtualTrade(pawn, new Trader_Vault()); });
            FloatMenuManager.Add("Open Warehouse",
                delegate(Pawn pawn) { ExtUtil.PrepareVirtualTrade(pawn, new Trader_Warehouse()); });
            FloatMenuManager.Add("Open StaticChamber",
                delegate(Pawn pawn) { ExtUtil.PrepareVirtualTrade(pawn, new Trader_StaticChamber()); });
            FloatMenuManager.Add("Open GlobalDropPod",
                delegate(Pawn pawn) { ExtUtil.PrepareVirtualTrade(pawn, new Trader_GlobalDropPod()); });
            FloatMenuManager.Add("Rent Vault", delegate { Static.RentVault(); });
            FloatMenuManager.Add("Rent Warehouse", delegate { Static.RentWarehouse(); });
            FloatMenuManager.Add("Clear Fine", delegate { Static.UnFreeze(); });
            FloatMenuManager.Add("InstantCollectRent", delegate
            {
                if (Static.IsVaultRented)
                {
                    Static.scheduledPayVaultRentTick = Find.TickManager.TicksAbs;
                }

                if (Static.IsWarehouseRented)
                {
                    Static.scheduledPayWarehouseRentTick = Find.TickManager.TicksAbs;
                }

                Static.CollectRent();
            });
            FloatMenuManager.Add("DropPod++", delegate { Static.dropPodCount++; });
            FloatMenuManager.Add("DestroyAnyContents", delegate { Static.DestroyAnyContents(); });
            FloatMenuManager.Add("printf(Warehouse)", delegate { Utility._debugOutputContentWarehouse(); });
            FloatMenuManager.Add("printf(StaticChamber)", delegate { Utility._debugOutputContentStaticChamber(); });
            FloatMenuManager.Add("Warehouse(Up)",
                delegate(Pawn pawn) { ExtUtil.PrepareVirtualTrade(pawn, new Trader_Warehouse(true)); });
            FloatMenuManager.Add("Warehouse(Dn)",
                delegate(Pawn pawn) { ExtUtil.PrepareVirtualTrade(pawn, new Trader_Warehouse(false, true)); });
            FloatMenuManager.Add("Vault(Up)",
                delegate(Pawn pawn) { ExtUtil.PrepareVirtualTrade(pawn, new Trader_Vault(true)); });
            FloatMenuManager.Add("Vault(Dn)",
                delegate(Pawn pawn) { ExtUtil.PrepareVirtualTrade(pawn, new Trader_Vault(false, true)); });
            FloatMenuManager.Add("StaticChamber(Up)",
                delegate(Pawn pawn) { ExtUtil.PrepareVirtualTrade(pawn, new Trader_StaticChamber(true)); });
            FloatMenuManager.Add("StaticChamber(Dn)",
                delegate(Pawn pawn) { ExtUtil.PrepareVirtualTrade(pawn, new Trader_StaticChamber(false, true)); });
            FloatMenuManager.AddShiftKeyItem("CoreShiftKeyItemRemoveTest",
                delegate { FloatMenuManager.Remove("CoreShiftKeyItemRemoveTest"); });
#endif

        Log.Message("[RimBankExt.Deposit] FloatMenu items added.");
    }

    internal static void RemoveAllModComponentsFromRimBankCore()
    {
        FloatMenuManager.Remove("RimBankExtDepositFloatMenuEntryLabel".Translate());
    }
}