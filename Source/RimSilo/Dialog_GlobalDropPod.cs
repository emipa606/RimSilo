using System;
using System.Collections.Generic;
using System.Linq;
using RimWorld;
using UnityEngine;
using Verse;
using Verse.Sound;

namespace RimBank.Ext.Deposit;

public class Dialog_GlobalDropPod : Window
{
    private const float TitleAreaHeight = 45f;

    private const float BaseTopAreaHeight = 55f;

    private const float ColumnWidth = 120f;

    private const float FirstCommodityY = 6f;

    private const float RowInterval = 30f;

    private const float SpaceBetweenTraderNameAndTraderKind = 27f;

    private readonly Vector2 acceptButtonSize = new(160f, 40f);

    private readonly Vector2 otherBottomButtonSize = new(160f, 40f);

    private Tradeable cachedCurrencyTradeable;

    private double cachedMassUsage;

    private Tradeable cachedNotesTradeable;

    private List<Tradeable> cachedTradeables;

    private bool massUsageDirty = true;

    private Vector2 scrollPosition = Vector2.zero;

    private TransferableSorterDef sorter1;

    private TransferableSorterDef sorter2;

    private bool traded;

    public Dialog_GlobalDropPod(Pawn playerNegotiator, ITrader trader)
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
    }

    public override Vector2 InitialSize => new(1024f, UI.screenHeight);

    private static float TopAreaHeight => 83f;

    private double MassUsage
    {
        get
        {
            if (!massUsageDirty)
            {
                return cachedMassUsage;
            }

            massUsageDirty = false;
            var num = 0.0;
            if (cachedCurrencyTradeable != null)
            {
                num += cachedCurrencyTradeable.CountToTransfer * cachedCurrencyTradeable.ThingDef.BaseMass;
                if (cachedNotesTradeable != null)
                {
                    num += cachedNotesTradeable.CountToTransfer * cachedNotesTradeable.ThingDef.BaseMass;
                }
            }

            cachedMassUsage = TradeSession.deal.CalculateMass(true).First + num;

            return cachedMassUsage;
        }
    }

    private double ActualMassUsage
    {
        get
        {
            var num = 0.0;
            if (cachedCurrencyTradeable == null)
            {
                return TradeSession.deal.CalculateMass().First + num;
            }

            num += cachedCurrencyTradeable.CountToTransfer * cachedCurrencyTradeable.ThingDef.BaseMass;
            if (cachedNotesTradeable != null)
            {
                num += cachedNotesTradeable.CountToTransfer * cachedNotesTradeable.ThingDef.BaseMass;
            }

            return TradeSession.deal.CalculateMass().First + num;
        }
    }

    private static float MassCapacity => Utility.DropPodCapacityTotal;

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
        cachedNotesTradeable = (from x in TradeSession.deal.AllTradeables
            where Utility.IsBankNote(x)
            orderby x.AnyThing.HitPoints descending
            select x).FirstOrDefault();
        Trade.Utility.CacheNotes();
    }

    public override void DoWindowContents(Rect inRect)
    {
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
        var rect = new Rect(num, 0f, inRect.width - num, TopAreaHeight);
        GUI.BeginGroup(rect);
        Text.Font = GameFont.Medium;
        var rect2 = new Rect(0f, 0f, rect.width / 2f, rect.height);
        Text.Anchor = TextAnchor.UpperLeft;
        Widgets.Label(rect2, Faction.OfPlayer.Name);
        var rect3 = new Rect(rect.width / 2f, 0f, rect.width / 2f, rect.height);
        Text.Anchor = TextAnchor.UpperRight;
        string text = "GlobalDropPodCaption".Translate();
        if (Text.CalcSize(text).x > rect3.width)
        {
            Text.Font = GameFont.Small;
            text = text.Truncate(rect3.width);
        }

        Widgets.Label(rect3, text);
        Text.Anchor = TextAnchor.UpperLeft;
        GUI.color = Color.white;
        Text.Font = GameFont.Small;
        _ = (float)MassUsage;
        _ = MassCapacity;
        var rect4 = rect.AtZero();
        rect4.y = 30f;
        // TODO: TransferableUIUtility.DrawMassInfo(rect4, num2, massCapacity, Translator.Translate("TipPodMass"), -9999f, false);
        rect4.y = 47f;
        GUI.color = Color.gray;
        Widgets.Label(rect4, "TextPodUsage".Translate(Utility.PodCountToSendMassBased((int)Math.Ceiling(MassUsage))));
        GUI.EndGroup();
        Text.Font = GameFont.Tiny;
        GUI.color = Color.white;
        Utility.DrawColumnSelectionButton(new Rect(70f, TopAreaHeight / 2f, 130f, 27f));
        var num3 = 0f;
        var indexOffset = 0;
        if (cachedCurrencyTradeable != null)
        {
            var num4 = inRect.width - 16f;
            var rect5 = new Rect(0f, TopAreaHeight, num4, 30f);
            var countToTransfer = cachedCurrencyTradeable.CountToTransfer;
            Utility.DrawTradeableRow(rect5, cachedCurrencyTradeable, 1, true, true);
            if (countToTransfer != cachedCurrencyTradeable.CountToTransfer)
            {
                countToTransferChanged();
            }

            num3 += 30f;
            if (cachedNotesTradeable != null)
            {
                var rect6 = new Rect(0f, TopAreaHeight + num3, num4, 30f);
                countToTransfer = cachedNotesTradeable.CountToTransfer;
                Utility.DrawTradeableRow(rect6, cachedNotesTradeable, 2, true, true);
                if (countToTransfer != cachedNotesTradeable.CountToTransfer)
                {
                    countToTransferChanged();
                }

                indexOffset = 1;
                num3 += 30f;
            }

            GUI.color = Color.gray;
            Widgets.DrawLineHorizontal(0f, TopAreaHeight + num3 + 2f, num4);
            GUI.color = Color.white;
        }
        else
        {
            Log.ErrorOnce("Trader throws null instead of 0 silver stack.This means that the storage is corrupted.",
                484369);
        }

        var mainRect = new Rect(0f, TopAreaHeight + num3, inRect.width,
            inRect.height - TopAreaHeight - 38f - num3 - 20f);
        fillMainRect(mainRect, indexOffset);
        var rect7 = new Rect((inRect.width / 2f) - (acceptButtonSize.x / 2f), inRect.height - 55f, acceptButtonSize.x,
            acceptButtonSize.y);
        if (Widgets.ButtonText(rect7, "AcceptButton".Translate(), true, false))
        {
            if (MassUsage > MassCapacity)
            {
                Messages.Message("MsgExceedMassLimit".Translate(), MessageTypeDefOf.RejectInput);
            }
            else
            {
                if (ActualMassUsage > 0.0)
                {
                    traded = true;
                }

                Close();
            }

            Event.current.Use();
        }

        if (Widgets.ButtonText(
                new Rect(rect7.x - 10f - otherBottomButtonSize.x, rect7.y, otherBottomButtonSize.x,
                    otherBottomButtonSize.y), "ResetButton".Translate(), true, false))
        {
            reset();
            Event.current.Use();
        }

        if (!Widgets.ButtonText(new Rect(rect7.xMax + 10f, rect7.y, otherBottomButtonSize.x, otherBottomButtonSize.y),
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
        Utility.Recache();
        if (!traded)
        {
            return;
        }

        Find.WindowStack.WindowOfType<Dialog_AccountCtrl>()?.Close(false);
        Trader_GlobalDropPod.TrySendQueuedDrop((int)Math.Ceiling(MassUsage));
    }

    private void fillMainRect(Rect mainRect, int indexOffset)
    {
        Text.Font = GameFont.Small;
        var height = 6f + (cachedTradeables.Count * 30f);
        var viewRect = new Rect(0f, 0f, mainRect.width - 16f, height);
        Widgets.BeginScrollView(mainRect, ref scrollPosition, viewRect);
        var num = 6f;
        var num2 = scrollPosition.y - 30f;
        var num3 = scrollPosition.y + mainRect.height;
        var num4 = indexOffset;
        foreach (var tradeable in cachedTradeables)
        {
            if (num > num2 && num < num3)
            {
                var rect = new Rect(0f, num, viewRect.width, 30f);
                var countToTransfer = tradeable.CountToTransfer;
                Utility.DrawTradeableRow(rect, tradeable, num4);
                if (countToTransfer != tradeable.CountToTransfer)
                {
                    countToTransferChanged();
                }
            }

            num += 30f;
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
        countToTransferChanged();
    }

    private void countToTransferChanged()
    {
        massUsageDirty = true;
    }
}