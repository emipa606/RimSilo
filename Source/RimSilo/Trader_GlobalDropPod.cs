using System.Collections.Generic;
using RimBank.Trade;
using RimBank.Trade.Ext;
using RimWorld;
using RimWorld.Planet;
using Verse;

namespace RimBank.Ext.Deposit;

public class Trader_GlobalDropPod : VirtualTrader
{
    private static readonly bool enableMapDrop = true;

    private static readonly bool enableTileDrop = false;

    private static readonly bool enableSettlementDrop = true;

    private static readonly bool enableSettlementAttack = true;

    private static readonly bool enableSettlementGenerateAndEnterMapFriendly = false;

    private static List<Thing> thingsToDrop;

    private static int cachedMass;

    private static PawnsArrivalModeDef randomDrop = DefDatabase<PawnsArrivalModeDef>.GetNamedSilentFail("RandomDrop");

    public override string TraderName => "GlobalDropPodLabel".Translate();

    public override IEnumerable<Thing> Goods
    {
        get
        {
            if (!Static.IsWarehouseGetRestricted)
            {
                foreach (var item in Static.contentWarehouse)
                {
                    yield return item;
                }
            }

            foreach (var vaultContent in Trader_Vault.VaultContents)
            {
                if (Static.IsVaultRestricted)
                {
                    vaultContent.stackCount = 0;
                }

                yield return vaultContent;
            }

            foreach (var item2 in Static.contentStaticChamber)
            {
                yield return item2;
            }
        }
    }

    public override Pair<int, int> GetCurrencyFmt()
    {
        var num = 0;
        var num2 = 0;
        foreach (var allTradeable in TradeSession.deal.AllTradeables)
        {
            if (allTradeable.IsCurrency)
            {
                num += allTradeable.CountToTransfer;
            }

            if (Utility.IsBankNote(allTradeable))
            {
                num2 += allTradeable.CountToTransfer;
            }
        }

        return new Pair<int, int>(num2, num);
    }

    public override void InvokeTradeUI()
    {
        thingsToDrop = [];
        Trade.Utility.CacheNotes();
        Find.WindowStack.Add(new Dialog_GlobalDropPod(TradeSession.playerNegotiator, TradeSession.trader));
        if (Static.IsVaultRestricted || Static.IsWarehouseGetRestricted)
        {
            Find.WindowStack.Add(new Dialog_MessageBox("DlgGlobalDropNotice".Translate()));
        }

        if (Static.dropPodCount == 0)
        {
            Find.WindowStack.Add(new Dialog_MessageBox("DlgNoPods".Translate(), null, null, null, null,
                "DlgTitleNoPods".Translate()));
        }
    }

    public override IEnumerable<Thing> ColonyThingsWillingToBuy(Pawn playerNegotiator)
    {
        return new List<Thing>();
    }

    public override void GiveSoldThingToPlayer(Thing toGive, int countToGive, Pawn playerNegotiator)
    {
        if (toGive is Pawn pawn)
        {
            Static.ExitStaticChamber(pawn);
            thingsToDrop.Add(pawn);
            return;
        }

        var thing = toGive.SplitOff(countToGive);
        thing.PreTraded(TradeAction.PlayerSells, playerNegotiator, this);
        if (thing.def == ThingDefOf.Silver)
        {
            Static.contentVaultSilver -= thing.stackCount;
        }
        else if (Utility.IsBankNote(thing))
        {
            Static.contentVaultBanknote -= thing.stackCount;
        }
        else if (thing == toGive)
        {
            Static.contentWarehouse.Remove(thing);
        }

        thingsToDrop.Add(thing);
    }

    public override void GiveSoldThingToTrader(Thing toGive, int countToGive, Pawn playerNegotiator)
    {
    }

    public static void TrySendQueuedDrop(int mass)
    {
        cachedMass = mass;
        startChoosingDestination();
    }

    private static void preDeliver()
    {
        thingsToDrop = [];
        TradeSession.deal.DoExecute();
        Static.dropPodCount -= Utility.PodCountToSendMassBased(cachedMass);
    }

    private static void finalizeTargeter()
    {
        CameraJumper.TryHideWorld();
        Find.WorldTargeter.StopTargeting();
    }

    private static void startChoosingDestination()
    {
        CameraJumper.TryJump(CameraJumper.GetWorldTarget(TradeSession.playerNegotiator));
        Find.WorldSelector.ClearSelection();
        Find.WorldTargeter.BeginTargeting(choseWorldTarget, enableTileDrop, StaticConstructor.TargeterMouseAttachment,
            false, null, getTileLabel);
    }

    private static TaggedString getTileLabel(GlobalTargetInfo target)
    {
        if (!target.IsValid || Find.World.Impassable(target.Tile))
        {
            return "MessageTransportPodsDestinationIsInvalid".Translate();
        }

        if (target.WorldObject is not MapParent mapParent)
        {
            return null;
        }

        if (!enableMapDrop && mapParent.HasMap)
        {
            return "NoMapDrop".Translate();
        }

        if (!enableSettlementDrop && mapParent is Settlement)
        {
            return "NoSettlementDrop".Translate();
        }

        return null;
    }

    private static bool choseWorldTarget(GlobalTargetInfo target)
    {
        if (!getTileLabel(target).NullOrEmpty())
        {
            Messages.Message("MessageTransportPodsDestinationIsInvalid".Translate(), MessageTypeDefOf.RejectInput);
            return false;
        }

        if (target.WorldObject is MapParent mapParent)
        {
            if (mapParent.HasMap && enableMapDrop)
            {
                var map = mapParent.Map;
                Current.Game.CurrentMap = map;
                finalizeTargeter();
                Find.CameraDriver.JumpToCurrentMapLoc(map.rememberedCameraPos.rootPos);
                Find.Targeter.BeginTargeting(TargetingParameters.ForDropPodsDestination(), delegate(LocalTargetInfo x)
                {
                    preDeliver();
                    Utility.TryDeliverThingsLocalNearPos(thingsToDrop, map, x.Cell);
                }, null, null, StaticConstructor.TargeterMouseAttachment);
                return true;
            }

            if (enableSettlementDrop)
            {
                var settlement = mapParent as Settlement;
                var list = new List<FloatMenuOption>();
                if (settlement is { Visitable: true })
                {
                    list.Add(new FloatMenuOption("VisitSettlement".Translate(target.WorldObject.Label),
                        delegate
                        {
                            preDeliver();
                            Utility.TryDeliverThingsGlobal(thingsToDrop, target.WorldObject,
                                ref PawnsArrivalModeDefOf.EdgeDrop, true);
                            finalizeTargeter();
                        }));
                }

                if (mapParent.Map != null)
                {
                    if (enableSettlementAttack)
                    {
                        list.Add(new FloatMenuOption("DropAtEdge".Translate(), delegate
                        {
                            preDeliver();
                            Utility.TryDeliverThingsGlobal(thingsToDrop, target.WorldObject,
                                ref PawnsArrivalModeDefOf.EdgeDrop, false, true);
                            finalizeTargeter();
                        }));
                        list.Add(new FloatMenuOption("DropInCenter".Translate(), delegate
                        {
                            preDeliver();
                            Utility.TryDeliverThingsGlobal(thingsToDrop, target.WorldObject,
                                ref randomDrop, false, true);
                            finalizeTargeter();
                        }));
                    }

                    if (enableSettlementGenerateAndEnterMapFriendly)
                    {
                        list.Add(new FloatMenuOption("DropInCenter".Translate() + "(Friendly)(Dev)", delegate
                        {
                            preDeliver();
                            Utility.TryDeliverThingsGlobal(thingsToDrop, target.WorldObject,
                                ref randomDrop);
                            finalizeTargeter();
                        }));
                    }
                }

                if (list.Any())
                {
                    Find.WindowStack.Add(new FloatMenu(list));
                    return false;
                }
            }
        }

        if (target.WorldObject is not Caravan { IsPlayerControlled: true } caravan)
        {
            return false;
        }

        preDeliver();
        Utility.TryDeliverThingsGlobal(thingsToDrop, caravan, ref PawnsArrivalModeDefOf.EdgeDrop);
        return true;
    }
}