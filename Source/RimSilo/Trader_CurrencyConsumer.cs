using System.Collections.Generic;
using RimBank.Trade.Ext;
using RimWorld;
using Verse;

namespace RimBank.Ext.Deposit;

public class Trader_CurrencyConsumer : VirtualTrader
{
    private readonly bool isVaultSource;

    private readonly string[] tipstrings;
    private readonly Window windowParent;

    public Trader_CurrencyConsumer(Window parent, string[] tipstrings, bool isVaultSource)
    {
        windowParent = parent;
        this.tipstrings = tipstrings;
        this.isVaultSource = isVaultSource;
    }

    public override IEnumerable<Thing> Goods => new List<Thing>();

    public override void CloseTradeUI()
    {
        if (windowParent is ICurrencyConsumer currencyConsumer)
        {
            currencyConsumer.Consumed = true;
        }
    }

    public override IEnumerable<Thing> ColonyThingsWillingToBuy(Pawn playerNegotiator)
    {
        if (isVaultSource)
        {
            foreach (var vaultContent in Trader_Vault.VaultContents)
            {
                yield return vaultContent;
            }

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
    }

    public override void GiveSoldThingToTrader(Thing toGive, int countToGive, Pawn playerNegotiator)
    {
        var thing = toGive.SplitOff(countToGive);
        if (isVaultSource)
        {
            if (thing.def == ThingDefOf.Silver)
            {
                Static.contentVaultSilver -= thing.stackCount;
            }
            else
            {
                Static.contentVaultBanknote -= thing.stackCount;
            }
        }
        else
        {
            thing.Destroy();
        }
    }

    public override string TipString(int index)
    {
        return tipstrings[index - 1];
    }
}