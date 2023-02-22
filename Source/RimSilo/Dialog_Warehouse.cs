using System;
using System.Collections.Generic;
using System.Linq;
using RimBank.Trade;
using RimWorld;
using UnityEngine;
using Verse;
using Verse.Sound;

namespace RimBank.Ext.Deposit;

public class Dialog_Warehouse : Window, ICurrencyConsumer
{
    private const float TitleAreaHeight = 45f;

    private const float BaseTopAreaHeight = 55f;

    private const float ColumnWidth = 120f;

    private const float FirstCommodityY = 6f;

    private const float RowInterval = 30f;

    private const float SpaceBetweenTraderNameAndTraderKind = 27f;

    protected readonly Vector2 AcceptButtonSize = new Vector2(160f, 40f);

    private readonly double cachedWarehouseMarketValue = Utility.CalculateWarehouseMarketValue();

    protected readonly Vector2 OtherBottomButtonSize = new Vector2(160f, 40f);

    private readonly double warehouseMassUsage = Utility.CalculateWarehouseUsage();

    private Tradeable cachedCurrencyTradeable;

    private double cachedMarketValue;

    private double cachedMassUsage;

    private List<Tradeable> cachedTradeables;

    private bool marketValueDirty = true;

    private bool massUsageDirty = true;

    private Vector2 scrollPosition = Vector2.zero;

    private TransferableSorterDef sorter1;

    private TransferableSorterDef sorter2;

    public Dialog_Warehouse(Pawn playerNegotiator, ITrader trader)
    {
        TradeSession.SetupWith(trader, playerNegotiator, false);
        closeOnCancel = true;
        forcePause = true;
        absorbInputAroundWindow = true;
        soundAppear = SoundDefOf.CommsWindow_Open;
        soundClose = SoundDefOf.CommsWindow_Close;
        soundAmbient = SoundDefOf.RadioComms_Ambience;
        sorter1 = TransferableSorterDefOf.Category;
        sorter2 = TransferableSorterDefOf.MarketValue;
    }

    public override Vector2 InitialSize => new Vector2(1024f, UI.screenHeight);

    private float TopAreaHeight => 83f;

    private double MassUsage
    {
        get
        {
            if (!massUsageDirty)
            {
                return cachedMassUsage;
            }

            massUsageDirty = false;
            var pair = TradeSession.deal.CalculateMass();
            cachedMassUsage = pair.Second - pair.First + warehouseMassUsage;

            return cachedMassUsage;
        }
    }

    private double MarketValue
    {
        get
        {
            if (!marketValueDirty)
            {
                return cachedMarketValue;
            }

            marketValueDirty = false;
            cachedMarketValue = cachedWarehouseMarketValue + TradeSession.deal.CalculateMarketValue();

            return cachedMarketValue;
        }
    }

    private float MassCapacity => Utility.WarehouseCapacity;

    bool ICurrencyConsumer.Consumed
    {
        set
        {
            if (!value)
            {
                return;
            }

            TradeSession.deal.DoExecute();
            Close();
        }
    }

    public override void PostOpen()
    {
        base.PostOpen();
        Static.TryMessageBoxRestrictedPermissionWarehouse();
        CacheTradeables();
    }

    private void CacheTradeables()
    {
        cachedCurrencyTradeable = (from x in TradeSession.deal.AllTradeables
            where x.IsCurrency
            select x).FirstOrDefault();
        cachedTradeables = (from tr in TradeSession.deal.AllTradeables
                where Utility.ShouldBeTradeable(tr)
                orderby 0 descending
                select tr).ThenBy(tr => tr, sorter1.Comparer).ThenBy(tr => tr, sorter2.Comparer)
            .ThenBy(TransferableUIUtility.DefaultListOrderPriority)
            .ThenBy(tr => tr.ThingDef.label)
            .ThenBy(tr => tr.AnyThing.TryGetQuality(out var qc) ? (int)qc : -1)
            .ThenBy(tr => tr.AnyThing.HitPoints)
            .ToList();
        Trade.Utility.CacheNotes();
    }

    public override void DoWindowContents(Rect inRect)
    {
        TradeSession.deal.UpdateCurrencyCountMassBased();
        TransferableUIUtility.DoTransferableSorters(sorter1, sorter2, delegate(TransferableSorterDef x)
        {
            sorter1 = x;
            CacheTradeables();
        }, delegate(TransferableSorterDef x)
        {
            sorter2 = x;
            CacheTradeables();
        });
        var num = inRect.width - 590f;
        var rect = new Rect(num, 0f, inRect.width - num, TopAreaHeight);
        GUI.BeginGroup(rect);
        Text.Font = GameFont.Medium;
        var rect2 = new Rect(0f, 0f, rect.width / 2f, rect.height);
        Text.Anchor = TextAnchor.UpperLeft;
        Widgets.Label(rect2, Faction.OfPlayer.Name);
        var rect3 = new Rect(rect.width / 2f, 0f, rect.width / 2f, rect.height);
        Text.Anchor = TextAnchor.UpperRight;
        var text = TradeSession.trader.TraderName;
        if (Text.CalcSize(text).x > rect3.width)
        {
            Text.Font = GameFont.Small;
            text = text.Truncate(rect3.width);
        }

        Widgets.Label(rect3, text);
        GUI.color = new Color(1f, 1f, 1f, 0.6f);
        Text.Font = GameFont.Tiny;
        var rect4 = new Rect((rect.width / 2f) - 100f - RowInterval, 0f, 200f, rect.height);
        Text.Anchor = TextAnchor.LowerCenter;
        Widgets.Label(rect4, "ValuePerMassUnit".Translate(Utility.MassValue.ToString("F1")));
        Text.Anchor = TextAnchor.UpperLeft;
        GUI.color = Color.white;
        Text.Font = GameFont.Small;
        var unused = (float)MassUsage;
        var unused1 = MassCapacity;
        var rect5 = rect.AtZero();
        rect5.y = TitleAreaHeight;
        // TODO: TransferableUIUtility.DrawMassInfo(rect5, num2, massCapacity, Translator.Translate("TipWarehouseMass"), -9999f, false);
        var text2 = "TotalValueLabel".Translate(MarketValue.ToString("F2"));
        var vector = Text.CalcSize(text2);
        var rect6 = new Rect(rect5.xMax - vector.x, TitleAreaHeight, vector.x, vector.y);
        Text.Anchor = TextAnchor.UpperRight;
        GUI.color = MarketValueMultiplier.ResolveColor(MarketValue);
        Widgets.Label(rect6, text2);
        GUI.color = Color.gray;
        TooltipHandler.TipRegion(rect6,
            "TipWarehouseMarketValue".Translate((int)MarketValueMultiplier.Resolve(MarketValue)));
        if (Mouse.IsOver(rect6))
        {
            Widgets.DrawHighlight(rect6);
        }

        GUI.EndGroup();
        var rect7 = new Rect(0f, TopAreaHeight / 2f, 590f, TopAreaHeight / 2f);
        GUI.BeginGroup(rect7);
        var rect8 = rect7.AtZero();
        rect8.width = rect8.height = 24f;
        Widgets.ButtonImage(rect8, StaticConstructor.TexArrowGet);
        TooltipHandler.TipRegion(rect8, "TipArrowGet".Translate());
        if (Static.IsWarehouseGetRestricted)
        {
            GUI.DrawTexture(rect8, StaticConstructor.TexLockAccess);
            TooltipHandler.TipRegion(rect8, "TipLockAccess".Translate());
        }

        var rect9 = rect7.AtZero();
        rect9.x += 29f;
        rect9.width = rect9.height = 24f;
        Widgets.ButtonImage(rect9, StaticConstructor.TexArrowPut);
        TooltipHandler.TipRegion(rect9, "TipArrowPut".Translate());
        if (Static.IsWarehousePutRestricted)
        {
            GUI.DrawTexture(rect9, StaticConstructor.TexLockAccess);
            TooltipHandler.TipRegion(rect9, "TipLockAccess".Translate());
        }

        GUI.EndGroup();
        Text.Font = GameFont.Tiny;
        GUI.color = Color.white;
        Utility.DrawColumnSelectionButton(new Rect(70f, TopAreaHeight / 2f, 130f, SpaceBetweenTraderNameAndTraderKind));
        var num8 = 0f;
        if (cachedCurrencyTradeable != null)
        {
            var num9 = inRect.width - 16f;
            Utility.DrawTradeableRow(new Rect(0f, TopAreaHeight, num9, RowInterval), cachedCurrencyTradeable, 1);
            GUI.color = Color.gray;
            Widgets.DrawLineHorizontal(0f, TopAreaHeight + RowInterval - 1f, num9);
            GUI.color = Color.white;
            num8 = RowInterval;
        }

        var mainRect = new Rect(0f, TopAreaHeight + num8, inRect.width,
            inRect.height - TopAreaHeight - 38f - num8 - 20f);
        FillMainRect(mainRect);
        var rect10 = new Rect((inRect.width / 2f) - (AcceptButtonSize.x / 2f), inRect.height - BaseTopAreaHeight,
            AcceptButtonSize.x,
            AcceptButtonSize.y);
        if (Widgets.ButtonText(rect10, "AcceptButton".Translate(), true, false))
        {
            TradeSession.deal.UpdateCurrencyCountMassBased();
            if (Utility.CheckWarehouseViolation())
            {
                Static.MessageRestrictedPermissionWarehouse();
            }
            else if (MassUsage > MassCapacity)
            {
                Messages.Message("MsgExceedMassLimit".Translate(), MessageTypeDefOf.RejectInput);
            }
            else
            {
                if (cachedCurrencyTradeable != null)
                {
                    var expense = Math.Abs(cachedCurrencyTradeable.CountToTransfer);
                    if (expense == 0)
                    {
                        Close();
                    }
                    else
                    {
                        void VaultPaymentHandler()
                        {
                            cachedCurrencyTradeable.ForceTo(0);
                            Utility.ConsumeCurrencyVault(this, expense);
                        }

                        void LocalPaymentHandler()
                        {
                            Trade.Utility.AskPayByBankNotes(cachedCurrencyTradeable, true);
                            Utility.Recache();
                        }

                        Utility.MakeFloatMenuSelectPaymentSource(this, expense, null, null, LocalPaymentHandler,
                            VaultPaymentHandler);
                    }
                }
            }

            Event.current.Use();
        }

        if (Widgets.ButtonText(
                new Rect(rect10.x - 10f - OtherBottomButtonSize.x, rect10.y, OtherBottomButtonSize.x,
                    OtherBottomButtonSize.y), "ResetButton".Translate(), true, false))
        {
            Reset();
            Event.current.Use();
        }

        if (!Widgets.ButtonText(new Rect(rect10.xMax + 10f, rect10.y, OtherBottomButtonSize.x, OtherBottomButtonSize.y),
                "CancelButton".Translate(), true, false))
        {
            return;
        }

        Close();
        Event.current.Use();
    }

    public override void Close(bool doCloseSound = true)
    {
        DragSliderManager.ForceStop();
        base.Close(doCloseSound);
    }

    public override void PostClose()
    {
        base.PostClose();
        Utility.Recache();
    }

    private void FillMainRect(Rect mainRect)
    {
        Text.Font = GameFont.Small;
        var height = FirstCommodityY + (cachedTradeables.Count * RowInterval);
        var viewRect = new Rect(0f, 0f, mainRect.width - 16f, height);
        Widgets.BeginScrollView(mainRect, ref scrollPosition, viewRect);
        var num = FirstCommodityY;
        var num2 = scrollPosition.y - RowInterval;
        var num3 = scrollPosition.y + mainRect.height;
        var num4 = 0;
        foreach (var tradeable in cachedTradeables)
        {
            if (num > num2 && num < num3)
            {
                var rect = new Rect(0f, num, viewRect.width, RowInterval);
                var countToTransfer = tradeable.CountToTransfer;
                Utility.DrawTradeableRow(rect, tradeable, num4);
                if (countToTransfer != tradeable.CountToTransfer)
                {
                    CountToTransferChanged();
                }
            }

            num += RowInterval;
            num4++;
        }

        Widgets.EndScrollView();
    }

    public override bool CausesMessageBackground()
    {
        return true;
    }

    private void Reset()
    {
        SoundDefOf.Tick_Low.PlayOneShotOnCamera();
        TradeSession.deal.Reset();
        CacheTradeables();
        CountToTransferChanged();
    }

    private void CountToTransferChanged()
    {
        massUsageDirty = true;
        marketValueDirty = true;
    }
}