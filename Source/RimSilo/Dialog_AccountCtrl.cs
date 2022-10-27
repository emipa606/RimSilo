using System;
using System.Collections.Generic;
using System.Linq;
using RimBank.Trade.Ext;
using RimWorld;
using UnityEngine;
using Verse;

namespace RimBank.Ext.Deposit;

public class Dialog_AccountCtrl : Window, ICurrencyConsumer
{
    private const float margain = 5f;

    private const float margainLarge = 15f;

    private const float vSpacing = 4f;

    private const float btnHeight = 36f;

    private static readonly bool noAbortDuringFirstPeriod;

    private static readonly GUIStyle cachedStyle;

    private static readonly GUIContent tmpContent;

    private static readonly float bannerTitleHeight;

    private static readonly float bannerTitleWidthLargeStyle;

    private static readonly float factionNameHeight;

    private static readonly float loginBarHeight;

    private static readonly Vector2 LEDSize;

    private static readonly Vector2 ImgBtnSize;

    private readonly Pawn pawn;

    internal bool _Consumed;

    private Flags state = Flags.None;

    private Window windowToKeepAlive;

    static Dialog_AccountCtrl()
    {
        noAbortDuringFirstPeriod = false;
        tmpContent = new GUIContent();
        LEDSize = new Vector2(24f, 24f);
        ImgBtnSize = new Vector2(36f, 36f);
        cachedStyle = new GUIStyle(Text.fontStyles[2])
        {
            fontSize = 30,
            wordWrap = false
        };
        var font = Text.Font;
        Text.Font = GameFont.Small;
        loginBarHeight = Text.CalcHeight("MainUILoggedIn".Translate(), 1024f);
        Text.Font = GameFont.Medium;
        factionNameHeight = Text.CalcHeight("CancelButton".Translate(), 1024f);
        var vector = LargeStyleCalcSize("MainUIBannerTitle".Translate());
        bannerTitleWidthLargeStyle = vector.x;
        bannerTitleHeight = vector.y;
        Text.Font = font;
    }

    public Dialog_AccountCtrl(Pawn pawn = null)
    {
        this.pawn = pawn;
        closeOnCancel = true;
        forcePause = true;
        absorbInputAroundWindow = true;
        soundAppear = SoundDefOf.CommsWindow_Open;
        soundClose = SoundDefOf.CommsWindow_Close;
        soundAmbient = SoundDefOf.RadioComms_Ambience;
    }

    public override Vector2 InitialSize => new Vector2(750f, 500f);

    private GUIStyle LargeStyle
    {
        get
        {
            cachedStyle.alignment = Text.Anchor;
            return cachedStyle;
        }
    }

    bool ICurrencyConsumer.Consumed
    {
        set
        {
            if (!value)
            {
                return;
            }

            switch (state)
            {
                case Flags.PayBill:
                    Static.UnFreeze();
                    Messages.Message("MsgUnlocked".Translate(), MessageTypeDefOf.PositiveEvent);
                    break;
                case Flags.Vault:
                    Static.RentVault();
                    Messages.Message("MsgVaultRented".Translate(), MessageTypeDefOf.PositiveEvent);
                    break;
                case Flags.Warehouse:
                    Static.RentWarehouse();
                    Messages.Message("MsgWarehouseRented".Translate(), MessageTypeDefOf.PositiveEvent);
                    break;
                case Flags.BuyPods:
                    Static.dropPodCount++;
                    Messages.Message("MsgPodPurchased".Translate(), MessageTypeDefOf.PositiveEvent);
                    break;
                case Flags.BuyPodsBulk:
                    Static.dropPodCount += 5;
                    Messages.Message("MsgPodPurchased".Translate(), MessageTypeDefOf.PositiveEvent);
                    break;
            }
        }
    }

    private static Vector2 LargeStyleCalcSize(string str)
    {
        tmpContent.text = str;
        return cachedStyle.CalcSize(tmpContent);
    }

    public void Notify_PawnEnteredStaticChamber(Pawn pawn)
    {
        if (this.pawn == pawn)
        {
            Close(false);
        }
    }

    public override void DoWindowContents(Rect inRect)
    {
        GUI.BeginGroup(inRect);
        GUI.color = Color.gray;
        string text = "MainUILoggedIn".Translate();
        var num = Math.Max(bannerTitleHeight, factionNameHeight + 12f + loginBarHeight);
        var vector = Text.CalcSize(text);
        var rect2 = new Rect(inRect.xMax - vector.x, num - vector.y, vector.x, vector.y);
        Widgets.Label(rect2, text);
        GUI.color = Color.white;
        Text.Font = GameFont.Medium;
        text = Faction.OfPlayer.Name;
        vector = Text.CalcSize(text);
        var num2 = Math.Max(rect2.width, vector.x);
        rect2 = new Rect(rect2.xMax - vector.x, rect2.y - 4f - vector.y, vector.x, vector.y);
        Widgets.Label(rect2, text);
        text = "MainUIBannerTitle".Translate();
        rect2 = new Rect(0f, 0f, inRect.width - num2 - 5f, num);
        Text.Anchor = TextAnchor.MiddleCenter;
        if (bannerTitleWidthLargeStyle <= rect2.width)
        {
            GUI.Label(rect2, text, LargeStyle);
        }
        else
        {
            Widgets.Label(rect2, text);
        }

        Text.Font = GameFont.Small;
        Text.Anchor = TextAnchor.UpperLeft;
        var rect3 = new Rect(0f, num + 8f, inRect.width / 2f, inRect.height / 2f);
        text = "VaultLabel".Translate();
        vector = LargeStyleCalcSize(text);
        rect2 = new Rect(rect3.x + 15f + LEDSize.x + 5f, rect3.y, vector.x, vector.y);
        GUI.Label(rect2, text, LargeStyle);
        var rect4 = new Rect(rect3.x + 15f, rect3.y + ((vector.y - LEDSize.y) / 2f), LEDSize.x, LEDSize.y);
        DrawLED(rect4, Flags.Vault);
        var rect5 = new Rect(rect2.x, rect2.yMax, 1024f, 0f);
        DrawCapacityLine(ref rect5, Flags.Vault);
        var rect6 = new Rect(rect2.x, rect5.yMax + 4f, rect3.xMax - rect2.x - 45f, 36f);
        DrawAccessButton(rect6, Flags.Vault);
        var rect7 = new Rect(rect6.x, rect6.yMax + 8f, 0f, 0f);
        DrawManagementBar(ref rect7, Flags.Vault);
        var rect8 = new Rect(rect3.x, rect7.yMax + 16f, rect3.width, 0f);
        rect8.height = inRect.yMax - rect8.y;
        Text.Font = GameFont.Medium;
        text = "StaticChamberLabel".Translate();
        vector = Text.CalcSize(text);
        rect2 = new Rect(rect2.position, vector)
        {
            y = rect8.y
        };
        Widgets.Label(rect2, text);
        Text.Font = GameFont.Small;
        rect4.y = rect2.y;
        if (Widgets.ButtonImage(rect4, StaticConstructor.TexInfo))
        {
            Find.WindowStack.Add(new Dialog_MessageBox(
                "MainClause_StaticChamber".Translate(Utility.StaticChamberFeePerPawn), null, null, null,
                null, "MainClauseTitle".Translate()));
            Event.current.Use();
        }

        TooltipHandler.TipRegion(rect4, "TipServiceInfoIcon".Translate());
        rect6.y = rect2.yMax + 4f;
        if (Widgets.ButtonText(rect6, "AccessButton".Translate(), true, false))
        {
            ExtUtil.PrepareVirtualTrade(pawn, new Trader_StaticChamber());
            Event.current.Use();
        }

        Text.Font = GameFont.Medium;
        text = "GlobalDropPodLabel".Translate();
        vector = Text.CalcSize(text);
        rect2 = new Rect(rect2.position, vector)
        {
            y = rect6.yMax + 12f
        };
        Widgets.Label(rect2, text);
        Text.Font = GameFont.Small;
        rect4.y = rect2.y;
        if (Widgets.ButtonImage(rect4, StaticConstructor.TexInfo))
        {
            Find.WindowStack.Add(new Dialog_MessageBox(
                "MainClause_GlobalDropPod".Translate(Utility.DropPodCapacityPerPod, Utility.DropPodCost),
                null, null, null, null, "MainClauseTitle".Translate()));
            Event.current.Use();
        }

        TooltipHandler.TipRegion(rect4, "TipServiceInfoIcon".Translate());
        rect4.y -= 6f;
        rect4.width = ImgBtnSize.x;
        rect4.height = ImgBtnSize.y;
        rect4.x = rect6.xMax - rect4.width;
        if (Widgets.ButtonImage(rect4, StaticConstructor.TexIncrease))
        {
            var count = Event.current.button == 0 ? 1 : 5;
            Window dlg = null;
            dlg = new Dialog_MessageBox("DlgBuyPod".Translate(count, Utility.DropPodCost, count * Utility.DropPodCost),
                "PayButton".Translate(), delegate
                {
                    state = count == 1 ? Flags.BuyPods : Flags.BuyPodsBulk;
                    Utility.MakeFloatMenuSelectPaymentSource(this, Utility.DropPodCost * count, new string[]
                    {
                        "TraderTitleBuyPod".Translate(),
                        "TraderTitleTipBuyPod".Translate(),
                        "WarehouseSilverTip".Translate(),
                        "BankNoteTip".Translate()
                    }, delegate { dlg?.Close(false); });
                    windowToKeepAlive = dlg;
                }, "CancelButton".Translate(), null, "DlgTitleBuyPod".Translate(), true);
            Find.WindowStack.Add(dlg);
            Event.current.Use();
        }

        TooltipHandler.TipRegion(rect4, "TipBuyPodIcon".Translate());
        rect6.y = rect2.yMax + 4f;
        if (Widgets.ButtonText(rect6, "AccessButton".Translate(), true, false))
        {
            ExtUtil.PrepareVirtualTrade(pawn, new Trader_GlobalDropPod());
            Event.current.Use();
        }

        GUI.color = Color.gray;
        text = "TextPodBar".Translate(Static.dropPodCount, Utility.DropPodCapacityTotal);
        vector = Text.CalcSize(text);
        rect2 = new Rect(rect6.position, vector)
        {
            y = rect6.yMax + 4f
        };
        Widgets.Label(rect2, text);
        GUI.color = Color.white;
        var rect9 = new Rect(rect3.xMax, rect3.y, rect3.width, rect3.height);
        text = "WarehouseLabel".Translate();
        vector = LargeStyleCalcSize(text);
        rect2 = new Rect(rect9.x + 15f + LEDSize.x + 5f, rect9.y, vector.x, vector.y);
        GUI.Label(rect2, text, LargeStyle);
        rect4 = new Rect(rect9.x + 15f, rect9.y + ((vector.y - LEDSize.y) / 2f), LEDSize.x, LEDSize.y);
        DrawLED(rect4, Flags.Warehouse);
        rect5 = new Rect(rect2.x, rect2.yMax, 1024f, 0f);
        DrawCapacityLine(ref rect5, Flags.Warehouse);
        rect6 = new Rect(rect2.x, rect5.yMax + 4f, rect9.xMax - rect2.x - 45f, 36f);
        DrawAccessButton(rect6, Flags.Warehouse);
        rect7 = new Rect(rect6.x, rect6.yMax + 8f, 0f, 0f);
        DrawManagementBar(ref rect7, Flags.Warehouse);
        var rect10 = new Rect(rect9.x, rect7.yMax + 16f, rect9.width, rect8.height);
        Text.Font = GameFont.Medium;
        text = "LabelMiscBar".Translate();
        vector = Text.CalcSize(text);
        rect2 = new Rect(rect2.x, rect10.y, vector.x, vector.y);
        if (Mouse.IsOver(rect2))
        {
            GUI.color = Color.cyan;
        }

        Widgets.Label(rect2, text);
        Widgets.DrawLineHorizontal(rect2.x, rect2.yMax - 1f, rect2.width);
        if (Widgets.ButtonInvisible(rect2))
        {
            var list = new List<FloatMenuOption>();

            void Action()
            {
                Find.WindowStack.Add(new Dialog_MessageBox("DlgFAQ".Translate(), null, null, null, null,
                    "DlgTitleFAQ".Translate()));
            }

            bool Func(Rect rect)
            {
                rect.y += 3f;
                rect.height = 24f;
                GUI.color = Color.white;
                if (!Widgets.ButtonImage(rect, StaticConstructor.TexInfo))
                {
                    return false;
                }

                Action();
                return true;
            }

            var item = new FloatMenuOption("DlgTitleFAQ".Translate(), Action, extraPartWidth: 24f,
                extraPartOnGUI: Func);
            list.Add(item);

            void Action2()
            {
                Find.WindowStack.Add(new Dialog_MaintainanceFeeVisual());
            }

            var unused = delegate(Rect rect)
            {
                rect.x += 6f;
                rect.y += 3f;
                rect.height = 24f;
                GUI.color = Color.white;
                if (!Widgets.ButtonImage(rect, StaticConstructor.TexCalc))
                {
                    return false;
                }

                Action2();
                return true;
            };
            item = new FloatMenuOption("CaptionManFeeCalc".Translate(), Action2, extraPartWidth: 24f,
                extraPartOnGUI: Func);
            list.Add(item);

            void Action3()
            {
                if (Static.IsVaultRented)
                {
                    ShowBill(Flags.CheckBill);
                }
                else
                {
                    Messages.Message("MsgShouldRentServiceFirst".Translate(), MessageTypeDefOf.RejectInput);
                }
            }

            var unused1 = delegate(Rect rect)
            {
                rect.x += 3f;
                rect.y += 3f;
                rect.height = 24f;
                GUI.color = Color.white;
                if (!Widgets.ButtonImage(rect, StaticConstructor.TexBill))
                {
                    return false;
                }

                Action3();
                return true;
            };
            item = new FloatMenuOption("CaptionBillPreview".Translate(), Action3, extraPartWidth: 24f,
                extraPartOnGUI: Func);
            list.Add(item);

            void Action4()
            {
                ShowBill(Flags.PayBill);
            }

            var unused2 = delegate(Rect rect)
            {
                rect.x += 3f;
                rect.y += 3f;
                rect.height = 24f;
                GUI.color = !Static.IsWarehouseRestricted && !Static.IsVaultRestricted ? Color.gray : Color.white;
                if (!Static.IsWarehouseRestricted && !Static.IsVaultRestricted)
                {
                    GUI.DrawTexture(rect, StaticConstructor.TexBill);
                    return false;
                }

                if (!Widgets.ButtonImage(rect, StaticConstructor.TexBill))
                {
                    return false;
                }

                Action4();
                return true;
            };
            item = new FloatMenuOption(
                !Static.IsWarehouseRestricted && !Static.IsVaultRestricted
                    ? "CaptionPayFineDisabled".Translate()
                    : "CaptionPayFineEnabled".Translate(), Action4,
                extraPartWidth: 24f, extraPartOnGUI: Func)
            {
                Disabled = !Static.IsWarehouseRestricted && !Static.IsVaultRestricted
            };
            list.Add(item);

            void Action5A()
            {
                if (!Utility.RemoveAllTickComponentsFromGame())
                {
                    Log.ErrorOnce("Failed to find any components to remove.", 1002473);
                }
                else
                {
                    Static.DestroyAnyContents();
                    StaticConstructor.RemoveAllModComponentsFromRimBankCore();
                    Close(false);
                    Messages.Message("MsgAccountDeleted".Translate(), MessageTypeDefOf.NeutralEvent);
                }
            }

            void Action5()
            {
                Find.WindowStack.Add(Dialog_MessageBox.CreateConfirmation("DlgDeleteAccount".Translate(), Action5A,
                    true, "DlgTitleDeleteAccount".Translate()));
            }

            var unused3 = delegate(Rect rect)
            {
                rect.y += 3f;
                rect.height = 24f;
                GUI.color = Color.red;
                if (!Widgets.ButtonImage(rect, StaticConstructor.TexLockAccess))
                {
                    return false;
                }

                Action5();
                return true;
            };
            item = new FloatMenuOption("CaptionDeleteAccount".Translate(), Action5, extraPartWidth: 24f,
                extraPartOnGUI: Func);
            list.Add(item);
            Find.WindowStack.Add(new FloatMenu(list));
        }

        Text.Font = GameFont.Small;
        GUI.color = Color.gray;
        text = "TextMiscBar".Translate();
        vector = Text.CalcSize(text);
        rect5 = new Rect(rect5.x, rect2.yMax + 4f, vector.x, vector.y);
        Widgets.Label(rect5, text);
        GUI.color = Color.white;
        rect6 = new Rect(inRect.xMax - 160f - 30f, inRect.yMax - 40f - 15f, 160f, 40f);
        if (Widgets.ButtonText(rect6, "CloseButton".Translate(), true, false))
        {
            Close();
            Event.current.Use();
        }

        GUI.EndGroup();
        if (windowToKeepAlive == null || Find.WindowStack.WindowOfType<Dialog_MessageBox>() != null)
        {
            return;
        }

        Find.WindowStack.Add(windowToKeepAlive);
        windowToKeepAlive = null;
    }

    public override void Close(bool doCloseSound = true)
    {
        base.Close(doCloseSound);
        GC.Collect(1);
    }

    private void DrawLED(Rect rect, Flags usage)
    {
        switch (usage)
        {
            case Flags.Vault:
                if (Static.IsVaultRented)
                {
                    GUI.color = Static.IsVaultRestricted ? Color.red : Color.green;
                }
                else
                {
                    GUI.color = Color.gray;
                }

                break;
            case Flags.Warehouse:
                if (Static.IsWarehouseRented)
                {
                    if (Static.IsWarehousePutRestricted)
                    {
                        GUI.color = Static.IsWarehouseGetRestricted ? Color.red : Color.yellow;
                    }
                    else
                    {
                        GUI.color = Color.green;
                    }
                }
                else
                {
                    GUI.color = Color.gray;
                }

                break;
        }

        GUI.DrawTexture(rect, StaticConstructor.TexLEDBase);
        Widgets.DrawHighlightIfMouseover(rect);
        var text = GUI.color == Color.green ? "LEDTipOk" :
            GUI.color == Color.gray ? "LEDTipNotRented" :
            GUI.color == Color.red ? "LEDTipDanger" : "LEDTipCaution";
        TooltipHandler.TipRegion(rect, text.Translate());
        GUI.color = Color.white;
    }

    private void DrawCapacityLine(ref Rect rect, Flags usage)
    {
        GUI.color = Color.gray;
        string text = null;
        if (usage == Flags.Vault && Static.IsVaultRented)
        {
            text = "TextCapacityUsage".Translate(Utility.CalculateVaultUsage(), Utility.VaultCapacity);
        }
        else if (usage == Flags.Warehouse && Static.IsWarehouseRented)
        {
            text = "TextCapacityUsage".Translate(Utility.CalculateWarehouseUsage().ToString("F1"),
                Utility.WarehouseCapacity);
        }

        if (text == null)
        {
            text = "TextCapacityNotRented".Translate();
            rect.height = Text.CalcHeight(text, 1024f);
        }
        else
        {
            var vector = Text.CalcSize(text);
            rect.width = vector.x;
            rect.height = vector.y;
            Widgets.DrawHighlightIfMouseover(rect);
            TooltipHandler.TipRegion(rect, ResolveCapacityTip(usage));
        }

        Widgets.Label(rect, text);
        GUI.color = Color.white;
    }

    private string ResolveCapacityTip(Flags usage)
    {
        string text;
        if (usage == Flags.Vault)
        {
            text = "TipCapacityDetail".Translate(Utility.CalculateVaultUsage(), Utility.VaultCapacity,
                CapacityExpansion.VaultBaseCapacity);
            if (Static.extensionsVault != 0)
            {
                text += "LineCapacityDetail".Translate(CapacityExpansion.VaultCapacityPerUnit, Static.extensionsVault);
            }
        }
        else
        {
            text = "TipCapacityDetail".Translate(Utility.CalculateWarehouseUsage().ToString("F1"),
                Utility.WarehouseCapacity, CapacityExpansion.WarehouseBaseCapacity);
            if (Static.extensionsWarehouse != 0)
            {
                text += "LineCapacityDetail".Translate(CapacityExpansion.WarehouseCapacityPerUnit,
                    Static.extensionsWarehouse);
            }
        }

        return text;
    }

    private void DrawAccessButton(Rect rect, Flags usage)
    {
        if (!Widgets.ButtonText(rect,
                usage == Flags.Vault && Static.IsVaultRented || usage == Flags.Warehouse && Static.IsWarehouseRented
                    ? "AccessButton".Translate()
                    : "RentServiceButton".Translate(), true, false))
        {
            return;
        }

        Event.current.Use();
        if (usage == Flags.Vault && Static.IsVaultRented || usage == Flags.Warehouse && Static.IsWarehouseRented)
        {
            if (usage == Flags.Vault)
            {
                ExtUtil.PrepareVirtualTrade(pawn, new Trader_Vault());
            }
            else
            {
                ExtUtil.PrepareVirtualTrade(pawn, new Trader_Warehouse());
            }
        }
        else
        {
            TryRentService(usage);
        }
    }

    private void TryRentService(Flags usage)
    {
        state = usage;
        if (!Static.IsVaultRented && usage == Flags.Warehouse)
        {
            Messages.Message("MsgShouldRentVaultFirst".Translate(), MessageTypeDefOf.RejectInput);
            return;
        }

        var fee = usage == Flags.Vault ? CapacityExpansion.VaultBaseRent : CapacityExpansion.WarehouseBaseRent;
        fee /= 2;
        var strings = new string[]
        {
            "TraderTitleRetainer".Translate(),
            "TraderTitleTipRetainer".Translate(),
            "WarehouseSilverTip".Translate(),
            "BankNoteTip".Translate()
        };
        string text = usage == Flags.Vault ? "VaultLabel".Translate() : "WarehouseLabel".Translate();
        Window dlg = null;
        dlg = new Dialog_MessageBox("DlgRentService".Translate(text, fee), "PayButton".Translate(), delegate
        {
            if (usage == Flags.Vault)
            {
                Utility.InvokeCurrencyConsumer(this, fee, strings);
            }
            else
            {
                Utility.MakeFloatMenuSelectPaymentSource(this, fee, strings, delegate { dlg?.Close(); });
                windowToKeepAlive = dlg;
            }
        }, "CancelButton".Translate(), null, "DlgTitleRentService".Translate(), true);
        Find.WindowStack.Add(dlg);
    }

    private void DrawManagementBar(ref Rect rect, Flags usage)
    {
        GUI.color = Color.gray;
        string text = "TextManBar".Translate();
        var size = Text.CalcSize(text);
        var rect2 = new Rect(rect.position, size);
        Widgets.Label(rect2, text);
        GUI.color = Color.white;
        var rect3 = new Rect(rect.x + 6f, rect2.yMax + 6f - 4f, 24f, 24f);
        if (Widgets.ButtonImage(rect3, StaticConstructor.TexInfo))
        {
            Find.WindowStack.Add(new Dialog_MessageBox(
                usage == Flags.Vault
                    ? "MainClause_Vault".Translate(CapacityExpansion.VaultBaseRent, CapacityExpansion.VaultBaseRent / 2)
                    : "MainClause_Warehouse".Translate(MarketValueMultiplier.stages[1],
                        CapacityExpansion.WarehouseBaseRent, CapacityExpansion.WarehouseBaseRent / 2,
                        Utility.MassValue), null, null, null, null, "MainClauseTitle".Translate()));
            Event.current.Use();
        }

        TooltipHandler.TipRegion(rect3, "TipServiceInfoIcon".Translate());
        rect.yMax = rect3.yMax + 6f;
        if ((usage != Flags.Vault || !Static.IsVaultRented) && (usage != Flags.Warehouse || !Static.IsWarehouseRented))
        {
            return;
        }

        rect3.x = rect3.xMax + 12f;
        if (Widgets.ButtonImage(rect3, StaticConstructor.TexIncOrDec))
        {
            var array = new[]
            {
                usage == Flags.Vault,
                usage == Flags.Warehouse
            };
            Find.WindowStack.Add(new Dialog_CapacityManager(array[0], array[1]));
            Event.current.Use();
        }

        TooltipHandler.TipRegion(rect3, "TipCapManIcon".Translate());
        rect3.x = rect3.xMax + 12f;
        if (Widgets.ButtonImage(rect3, StaticConstructor.TexRedCrossDiag))
        {
            if (noAbortDuringFirstPeriod && (usage == Flags.Vault && Static.lastPayVaultRentTick < 0 ||
                                             usage == Flags.Warehouse && Static.lastPayWarehouseRentTick < 0))
            {
                Messages.Message("MsgNoAbortDuringFirstPeriod".Translate(), MessageTypeDefOf.RejectInput);
            }
            else
            {
                string text2 = usage == Flags.Vault
                    ? "VaultLabel".Translate().RawText +
                      (Static.IsWarehouseRented ? " , " + "WarehouseLabel".Translate() : "")
                    : "WarehouseLabel".Translate();
                Find.WindowStack.Add(Dialog_MessageBox.CreateConfirmation("DlgAbortService".Translate(text2), delegate
                {
                    Static.DropWarehouse();
                    Static.ResetWarehouse();
                    if (usage != Flags.Vault)
                    {
                        return;
                    }

                    Static.DropVault();
                    Static.ResetVault();
                }, true, "DlgTitleAbortService".Translate()));
            }

            Event.current.Use();
        }

        TooltipHandler.TipRegion(rect3, "TipAbortIcon".Translate());
    }

    private void ShowBill(Flags usage)
    {
        state = usage;
        var val = -1f;
        var list = new List<string>();
        var list2 = new List<int>();
        string text;
        switch (usage)
        {
            case Flags.CheckBill:
                text = "LineVaultRent".Translate();
                val = Math.Max(val, Text.CalcSize(text).x);
                list.Add(text);
                list2.Add(Utility.VaultRent);
                if (Static.IsWarehouseRented)
                {
                    text = "LineWarehouseRent".Translate();
                    val = Math.Max(val, Text.CalcSize(text).x);
                    list.Add(text);
                    list2.Add(Utility.WarehouseRent);
                    text = "LineWarehouseMaintainanceFee".Translate();
                    val = Math.Max(val, Text.CalcSize(text).x);
                    list.Add(text);
                    list2.Add(Utility.WarehouseMaintainanceFee);
                }

                break;
            case Flags.PayBill:
                if (Static.IsVaultRestricted)
                {
                    text = "LineVaultRent".Translate();
                    val = Math.Max(val, Text.CalcSize(text).x);
                    list.Add(text);
                    list2.Add(Utility.VaultRent);
                }

                if (Static.IsWarehouseGetRestricted)
                {
                    text = "LineWarehouseRent".Translate();
                    val = Math.Max(val, Text.CalcSize(text).x);
                    list.Add(text);
                    list2.Add(Utility.WarehouseRent);
                }

                if (Static.IsWarehousePutRestricted)
                {
                    text = "LineWarehouseMaintainanceFee".Translate();
                    val = Math.Max(val, Text.CalcSize(text).x);
                    list.Add(text);
                    list2.Add(Utility.WarehouseMaintainanceFee);
                }

                text = "LineFeeOfPenalty".Translate();
                val = Math.Max(val, Text.CalcSize(text).x);
                list.Add(text);
                list2.Add(Utility.FineForDelay);
                break;
        }

        text = "LineBillTotal".Translate();
        val = Math.Max(val, Text.CalcSize(text).x);
        list.Add(text);
        val += 75f;
        text = "";
        var fee = 0;
        for (var num = list2.Count - 1; num > -1; num--)
        {
            text = $"{list[num].PadLeft(val)} : {list2[num]}\n{text}";
            fee += list2[num];
        }

        text = $"{text}\n{list.Last().PadLeft(val)} : {fee}";
        if (usage == Flags.CheckBill)
        {
            Find.WindowStack.Add(new Dialog_MessageBox(text, null, null, null, null,
                "LineBillDetailTitle".Translate()));
        }

        if (usage != 0)
        {
            return;
        }

        text = $"{text}\n\n" + "LinePayFine".Translate();
        Window dlg = null;
        dlg = new Dialog_MessageBox(text, "PayButton".Translate(), delegate
        {
            Utility.MakeFloatMenuSelectPaymentSource(this, fee, new string[]
            {
                "TraderPayBillTitle".Translate(),
                "TraderPayBillTitleTip".Translate(),
                "WarehouseSilverTip".Translate(),
                "BankNoteTip".Translate()
            }, delegate { dlg?.Close(); }, null, null, true);
            windowToKeepAlive = dlg;
        }, "CloseButton".Translate(), null, "LineBillDetailTitle".Translate());
        Find.WindowStack.Add(dlg);
    }

    private enum Flags
    {
        PayBill,
        CheckBill,
        None,
        Vault,
        Warehouse,
        BuyPods,
        BuyPodsBulk
    }
}