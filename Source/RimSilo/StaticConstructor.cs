using RimBank.Core.Interactive;
using UnityEngine;
using Verse;

namespace RimBank.Ext.Deposit;

[StaticConstructorOnStartup]
internal static class StaticConstructor
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

        Log.Message("[RimBankExt.Deposit] FloatMenu items added.");
    }

    internal static void RemoveAllModComponentsFromRimBankCore()
    {
        FloatMenuManager.Remove("RimBankExtDepositFloatMenuEntryLabel".Translate());
    }
}