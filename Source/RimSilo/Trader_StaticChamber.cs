using System.Collections.Generic;
using System.Linq;
using RimBank.Trade.Ext;
using RimWorld;
using Verse;

namespace RimBank.Ext.Deposit;

public class Trader_StaticChamber(bool upOnly = false, bool downOnly = false) : VirtualTrader
{
    private static readonly string[] tipStrings =
    [
        "TraderStaticChamberTitle".Translate(),
        "TraderStaticChamberTitleTip".Translate(Utility.StaticChamberFeePerPawn),
        "WarehouseSilverTip".Translate(),
        "BankNoteTip".Translate()
    ];

    private static List<IntVec3> dropCells = [];

    private static int lastCachedTick = -1;

    private Dialog_AccountCtrl currentCtrlUI;

    public override string TraderName => "StaticChamberLabel".Translate();

    public override IEnumerable<Thing> Goods
    {
        get
        {
            if (upOnly)
            {
                yield break;
            }

            foreach (var item in Static.contentStaticChamber)
            {
                yield return item;
            }
        }
    }

    public override IEnumerable<Thing> ColonyThingsWillingToBuy(Pawn playerNegotiator)
    {
        if (downOnly)
        {
            yield break;
        }

        foreach (var item in AllPotentialPawnsInMap(Find.CurrentMap))
        {
            yield return item;
        }

        foreach (var item2 in TradeUtility.AllLaunchableThingsForTrade(Find.CurrentMap))
        {
            if (item2.def == ThingDefOf.Silver || Utility.IsBankNote(item2))
            {
                yield return item2;
            }
        }
    }

    public override void GiveSoldThingToTrader(Thing toGive, int countToGive, Pawn playerNegotiator)
    {
        if (toGive is not Pawn pawn)
        {
            var thing = toGive.SplitOff(countToGive);
            if (thing.def == ThingDefOf.Silver || Utility.IsBankNote(thing))
            {
                thing.Destroy();
            }
            else
            {
                Log.Error("Tried to submit a thing to StaticChamber,but it is not a pawn.");
            }

            return;
        }

        var isPrisoner = pawn.guest is { IsPrisoner: true };
        PawnPreEnterChamber(pawn, playerNegotiator);
        if (pawn.Spawned)
        {
            pawn.DeSpawn();
        }

        currentCtrlUI.Notify_PawnEnteredStaticChamber(pawn);
        tryEntitlePrisoner(pawn, isPrisoner);
        Static.EnterStaticChamber(pawn);
    }

    public override void GiveSoldThingToPlayer(Thing toGive, int countToGive, Pawn playerNegotiator)
    {
        if (toGive is not Pawn pawn)
        {
            Log.Error("Tried to give a thing from StaticChamber,but it is not a pawn.");
            return;
        }

        Static.ExitStaticChamber(pawn);
        if (downOnly)
        {
            TradeUtility.SpawnDropPod(Utility.FindDropSpotWith(playerNegotiator, true), playerNegotiator.Map, pawn);
        }
        else
        {
            Utility.TryDelieverThingLocal(pawn, playerNegotiator.Map);
        }
    }

    public override void InvokeTradeUI()
    {
        currentCtrlUI = Find.WindowStack.WindowOfType<Dialog_AccountCtrl>() ?? new Dialog_AccountCtrl();

        Find.WindowStack.Add(new Dialog_StaticChamber(TradeSession.playerNegotiator, TradeSession.trader));
    }

    public override void CloseTradeUI()
    {
        Find.WindowStack.WindowOfType<Dialog_StaticChamber>().Close(false);
    }

    public override string TipString(int index)
    {
        return tipStrings[index - 1];
    }

    public override Pair<int, int> GetCurrencyFmt()
    {
        return new Pair<int, int>(0, 0);
    }

    private IEnumerable<Pawn> AllPotentialPawnsInMap(Map map)
    {
        foreach (var item in map.mapPawns.PrisonersOfColonySpawned)
        {
            if (item.guest.PrisonerIsSecure)
            {
                yield return item;
            }
        }

        foreach (var item2 in map.mapPawns.SpawnedPawnsInFaction(Faction.OfPlayer))
        {
            if (item2.HostFaction == null && !item2.InMentalState)
            {
                yield return item2;
            }
        }
    }

    private void PawnPreEnterChamber(Pawn pawn, Pawn negotiator)
    {
        pawn.Notify_Teleported();
        pawn.PreTraded(TradeAction.None, negotiator, this);
    }

    public static bool NoOneInSquad()
    {
        foreach (var allTradeable in TradeSession.deal.AllTradeables)
        {
            switch (allTradeable.ActionToDo)
            {
                case TradeAction.PlayerSells:
                    continue;
                case TradeAction.None:
                {
                    if (allTradeable.FirstThingColony is Pawn pawn && pawn.RaceProps.Humanlike && !pawn.IsPrisoner)
                    {
                        return false;
                    }

                    break;
                }
                default:
                {
                    if (allTradeable.AnyThing is Pawn pawn2 && pawn2.RaceProps.Humanlike && !pawn2.IsPrisoner)
                    {
                        return false;
                    }

                    break;
                }
            }
        }

        return true;
    }

    private static void tryEntitlePrisoner(Pawn pawn, bool isPrisoner)
    {
        if (!isPrisoner)
        {
            return;
        }

        pawn.guest ??= new Pawn_GuestTracker();

        if (pawn.guest.Released)
        {
            pawn.guest.Released = false;
            pawn.guest.SetNoInteraction();
        }

        pawn.guest.SetGuestStatus(Faction.OfPlayer);
    }

    public static IntVec3 IndoorDropCell(Map map)
    {
        tryCacheDropCells(map);
        if (!dropCells.Any())
        {
            return DropCellFinder.TradeDropSpot(map);
        }

        dropCells.TryRandomElement(out var result);
        return result;
    }

    private static void tryCacheDropCells(Map map)
    {
        if (lastCachedTick == Find.TickManager.TicksAbs)
        {
            return;
        }

        lastCachedTick = Find.TickManager.TicksAbs;
        var list = map.listerBuildings.allBuildingsColonist.Where(b => b.def.IsOrbitalTradeBeacon).ToList();

        list.RemoveAll(b => map.roofGrid.Roofed(b.Position) || noPower(b));
        list.RemoveAll(notEnclosed);
        var list2 = new List<IntVec3>();
        foreach (var item in list)
        {
            foreach (var cell in item.GetRoom(RegionType.Normal | RegionType.Portal).Cells)
            {
                if (!map.roofGrid.Roofed(cell))
                {
                    list2.Add(cell);
                }
            }
        }

        dropCells = list2;
        return;

        static bool noPower(Building b)
        {
            var compPowerTrader = b.TryGetComp<CompPowerTrader>();
            return compPowerTrader is not { PowerOn: true };
        }

        static bool notEnclosed(Building b)
        {
            return b.GetRoom(RegionType.Normal | RegionType.Portal)?.TouchesMapEdge ?? true;
        }
    }
}