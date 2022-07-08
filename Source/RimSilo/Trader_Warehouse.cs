using System.Collections.Generic;
using RimBank.Trade.Ext;
using RimWorld;
using Verse;

namespace RimBank.Ext.Deposit;

public class Trader_Warehouse : VirtualTrader
{
    public static readonly string[] TipStrings =
    {
        "TraderWarehouseTitle".Translate(),
        "TraderWarehouseTitleTip".Translate(),
        "WarehouseSilverTip".Translate(),
        "BankNoteTip".Translate()
    };

    private readonly bool transferingDownOnly;

    private readonly bool transferingUpOnly;

    public Trader_Warehouse(bool upOnly = false, bool downOnly = false)
    {
        transferingDownOnly = downOnly;
        transferingUpOnly = upOnly;
    }

    public override IEnumerable<Thing> Goods
    {
        get
        {
            if (transferingUpOnly)
            {
                return new List<Thing>();
            }

            return Static.contentWarehouse;
        }
    }

    public override string TraderName => "WarehouseLabel".Translate();

    public override IEnumerable<Thing> ColonyThingsWillingToBuy(Pawn playerNegotiator)
    {
        if (transferingDownOnly)
        {
            yield break;
        }

        foreach (var item in TradeUtility.AllLaunchableThingsForTrade(playerNegotiator.Map))
        {
            if (item.def != ThingDefOf.ActiveDropPod && item.def != ThingDefOf.DropPodIncoming &&
                item.def != ThingDefOf.DropPodLeaving)
            {
                yield return item;
            }
        }
    }

    public override void GiveSoldThingToPlayer(Thing toGive, int countToGive, Pawn playerNegotiator)
    {
        var thing = toGive.SplitOff(countToGive);
        thing.PreTraded(TradeAction.PlayerBuys, playerNegotiator, this);
        if (thing == toGive)
        {
            Static.contentWarehouse.Remove(thing);
        }

        TradeUtility.SpawnDropPod(Utility.FindDropSpotWith(playerNegotiator, transferingDownOnly), playerNegotiator.Map,
            thing);
    }

    public override void GiveSoldThingToTrader(Thing toGive, int countToGive, Pawn playerNegotiator)
    {
        var thing = toGive.SplitOff(countToGive);
        thing.PreTraded(TradeAction.PlayerSells, playerNegotiator, this);
        if (thing.def == ThingDefOf.Silver || Utility.isBankNote(thing))
        {
            thing.Destroy();
            return;
        }

        var thing2 = TradeUtility.ThingFromStockToMergeWith(this, thing);
        if (thing2 != null)
        {
            if (!thing2.TryAbsorbStack(thing, false))
            {
                thing.Destroy();
            }
        }
        else
        {
            Static.contentWarehouse.Add(thing);
        }
    }

    public override void InvokeTradeUI()
    {
        Find.WindowStack.Add(new Dialog_Warehouse(TradeSession.playerNegotiator, TradeSession.trader));
    }

    public override void CloseTradeUI()
    {
        Find.WindowStack.WindowOfType<Dialog_Warehouse>().Close(false);
    }

    public override Pair<int, int> GetCurrencyFmt()
    {
        return new Pair<int, int>(0, 0);
    }

    public override string TipString(int index)
    {
        return TipStrings[index - 1];
    }
}