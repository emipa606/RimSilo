using System;
using System.Collections.Generic;
using System.Linq;
using RimBank.Trade;
using RimWorld;
using UnityEngine;
using Verse;
using Verse.Sound;

namespace RimBank.Ext.Deposit;

public class Dialog_StaticChamber : Window, ICurrencyConsumer
{
    private const float TitleAreaHeight = 45f;

    private const float BaseTopAreaHeight = 55f;

    private const float ColumnWidth = 120f;

    private const float FirstCommodityY = 6f;

    private const float RowInterval = 30f;

    private const float SpaceBetweenTraderNameAndTraderKind = 27f;

    private readonly Vector2 AcceptButtonSize = new(160f, 40f);

    public readonly Vector2 OtherBottomButtonSize = new(160f, 40f);

    private Tradeable cachedCurrencyTradeable;

    private List<Tradeable> cachedTradeables;

    private Vector2 scrollPosition = Vector2.zero;

    private TransferableSorterDef sorter1;

    private TransferableSorterDef sorter2;

    public Dialog_StaticChamber(Pawn playerNegotiator, ITrader trader)
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
        Utility.ResetColumnType();
        Utility.SetColumnToDateColumn();
    }

    public override Vector2 InitialSize => new(1024f, UI.screenHeight);

    private static float TopAreaHeight => 83f;

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
        cacheTradeables();
    }

    private void cacheTradeables()
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
        TradeSession.deal.UpdateCurrencyCountStaticChamber();
        TransferableUIUtility.DoTransferableSorters(sorter1, sorter2, delegate(TransferableSorterDef x)
        {
            sorter1 = x;
            cacheTradeables();
        }, delegate(TransferableSorterDef x)
        {
            sorter2 = x;
            cacheTradeables();
        });
        var num = inRect.width - 590f;
        var position = new Rect(num, 0f, inRect.width - num, TopAreaHeight);
        GUI.BeginGroup(position);
        Text.Font = GameFont.Medium;
        var rect = new Rect(0f, 0f, position.width / 2f, position.height);
        Text.Anchor = TextAnchor.UpperLeft;
        Widgets.Label(rect, Faction.OfPlayer.Name);
        var rect2 = new Rect(position.width / 2f, 0f, position.width / 2f, position.height);
        Text.Anchor = TextAnchor.UpperRight;
        var text = TradeSession.trader.TraderName;
        if (Text.CalcSize(text).x > rect2.width)
        {
            Text.Font = GameFont.Small;
            text = text.Truncate(rect2.width);
        }

        Widgets.Label(rect2, text);
        GUI.color = new Color(1f, 1f, 1f, 0.6f);
        Text.Font = GameFont.Tiny;
        var rect3 = new Rect((position.width / 2f) - 100f - RowInterval, 0f, 200f, position.height);
        Text.Anchor = TextAnchor.LowerCenter;
        Widgets.Label(rect3, "StaticChamberFee".Translate(Utility.StaticChamberFeePerPawn));
        GUI.EndGroup();
        Text.Font = GameFont.Tiny;
        GUI.color = Color.white;
        Utility.DrawColumnSelectionButton(new Rect(70f, TopAreaHeight / 2f, 130f, SpaceBetweenTraderNameAndTraderKind));
        var num2 = 0f;
        if (cachedCurrencyTradeable != null)
        {
            var num3 = inRect.width - 16f;
            Utility.DrawTradeableRow(new Rect(0f, TopAreaHeight, num3, RowInterval), cachedCurrencyTradeable, 1);
            GUI.color = Color.gray;
            Widgets.DrawLineHorizontal(0f, TopAreaHeight + RowInterval - 1f, num3);
            GUI.color = Color.white;
            num2 = RowInterval;
        }

        var mainRect = new Rect(0f, TopAreaHeight + num2, inRect.width,
            inRect.height - TopAreaHeight - 38f - num2 - 20f);
        fillMainRect(mainRect);
        var rect4 = new Rect((inRect.width / 2f) - (AcceptButtonSize.x / 2f), inRect.height - BaseTopAreaHeight,
            AcceptButtonSize.x,
            AcceptButtonSize.y);
        if (Widgets.ButtonText(rect4, "AcceptButton".Translate(), true, false))
        {
            TradeSession.deal.UpdateCurrencyCountStaticChamber();
            if (cachedCurrencyTradeable != null)
            {
                var expense = Math.Abs(cachedCurrencyTradeable.CountToTransfer);
                if (expense == 0)
                {
                    ((ICurrencyConsumer)this).Consumed = true;
                }
                else
                {
                    void vaultPayAction()
                    {
                        cachedCurrencyTradeable.ForceTo(0);
                        Utility.ConsumeCurrencyVault(this, expense);
                    }

                    void action()
                    {
                        if (Trader_StaticChamber.NoOneInSquad())
                        {
                            Find.WindowStack.Add(Dialog_MessageBox.CreateConfirmation("NoOneInSquadConfirm".Translate(),
                                vaultPayAction, true, "NoOneInSquadTitle".Translate()));
                        }
                        else
                        {
                            vaultPayAction();
                        }
                    }

                    void localPayAction()
                    {
                        Trade.Utility.AskPayByBankNotes(cachedCurrencyTradeable, true);
                        Utility.Recache();
                    }

                    void localPaymentHandler()
                    {
                        if (Trader_StaticChamber.NoOneInSquad())
                        {
                            Find.WindowStack.Add(Dialog_MessageBox.CreateConfirmation("NoOneInSquadConfirm".Translate(),
                                localPayAction, true, "NoOneInSquadTitle".Translate()));
                        }
                        else
                        {
                            localPayAction();
                        }
                    }

                    var vaultPaymentHandler = (Action)action;
                    Utility.MakeFloatMenuSelectPaymentSource(this, expense, null, null, localPaymentHandler,
                        vaultPaymentHandler);
                }
            }

            Event.current.Use();
        }

        if (Widgets.ButtonText(
                new Rect(rect4.x - 10f - OtherBottomButtonSize.x, rect4.y, OtherBottomButtonSize.x,
                    OtherBottomButtonSize.y), "ResetButton".Translate(), true, false))
        {
            reset();
            Event.current.Use();
        }

        if (!Widgets.ButtonText(new Rect(rect4.xMax + 10f, rect4.y, OtherBottomButtonSize.x, OtherBottomButtonSize.y),
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
        Utility.ResetColumnType();
    }

    private void fillMainRect(Rect mainRect)
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
                Utility.DrawTradeableRow(new Rect(0f, num, viewRect.width, RowInterval), tradeable, num4);
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

    private void reset()
    {
        SoundDefOf.Tick_Low.PlayOneShotOnCamera();
        TradeSession.deal.Reset();
        cacheTradeables();
    }
}