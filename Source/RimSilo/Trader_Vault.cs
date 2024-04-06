using System.Collections.Generic;
using RimBank.Trade.Ext;
using RimWorld;
using Verse;

namespace RimBank.Ext.Deposit;

public class Trader_Vault(bool upOnly = false, bool downOnly = false) : VirtualTrader
{
    public override bool UniqueBalanceMethod => true;

    public override bool SilverAlsoAdjustable => true;

    public override string TraderName => "VaultLabel".Translate();

    public override IEnumerable<Thing> Goods => upOnly ? new List<Thing>() : VaultContents;

    public static IEnumerable<Thing> VaultContents
    {
        get
        {
            var thing = ThingMaker.MakeThing(ThingDefOf.Silver);
            thing.stackCount = Static.contentVaultSilver;
            yield return thing;
            thing = ThingMaker.MakeThing(Utility.BankNoteDef());
            thing.stackCount = Static.contentVaultBanknote;
            yield return thing;
        }
    }

    public override IEnumerable<Thing> ColonyThingsWillingToBuy(Pawn playerNegotiator)
    {
        if (downOnly)
        {
            yield break;
        }

        foreach (var item in TradeUtility.AllLaunchableThingsForTrade(playerNegotiator.Map))
        {
            if (item.def == ThingDefOf.Silver || Utility.isBankNote(item))
            {
                yield return item;
            }
        }
    }

    public override void GiveSoldThingToPlayer(Thing toGive, int countToGive, Pawn playerNegotiator)
    {
        var thing = toGive.SplitOff(countToGive);
        thing.PreTraded(TradeAction.PlayerBuys, playerNegotiator, this);
        if (thing.def == ThingDefOf.Silver)
        {
            Static.contentVaultSilver -= thing.stackCount;
        }
        else
        {
            Static.contentVaultBanknote -= thing.stackCount;
        }

        TradeUtility.SpawnDropPod(Utility.FindDropSpotWith(playerNegotiator, downOnly), playerNegotiator.Map,
            thing);
    }

    public override void GiveSoldThingToTrader(Thing toGive, int countToGive, Pawn playerNegotiator)
    {
        var thing = toGive.SplitOff(countToGive);
        thing.PreTraded(TradeAction.PlayerSells, playerNegotiator, this);
        if (thing.def == ThingDefOf.Silver)
        {
            Static.contentVaultSilver += thing.stackCount;
        }
        else
        {
            Static.contentVaultBanknote += thing.stackCount;
        }
    }

    public override void InvokeTradeUI()
    {
        Trade.Utility.CacheNotes();
        Trade.Utility.AskPayByBankNotes(TradeSession.deal.CurrencyTradeable, true);
        Static.TryMessageBoxRestrictedPermissionVault();
    }

    public override string TipString(int index)
    {
        return index switch
        {
            1 => TraderName,
            2 => "TraderVaultTitleTip".Translate(Utility.VaultCapacity),
            3 => "VaultSilverTip".Translate(),
            4 => "VaultBankNoteTip".Translate(),
            _ => null
        };
    }

    public override bool CustomCheckViolation(Tradeable silver, Tradeable notes)
    {
        if (Static.IsVaultRestricted &&
            (silver.ActionToDo == TradeAction.PlayerBuys || notes.ActionToDo == TradeAction.PlayerBuys))
        {
            Static.MessageRestrictedPermissionVault();
            return true;
        }

        if (Utility.CalculateVaultUsage() - silver.CountToTransfer - (notes.CountToTransfer * 1000) <=
            Utility.VaultCapacity)
        {
            return false;
        }

        Messages.Message("MsgExceedsVaultCapacity".Translate(Utility.VaultCapacity),
            MessageTypeDefOf.RejectInput);
        return true;
    }
}