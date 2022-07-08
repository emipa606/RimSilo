using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using RimBank.Trade.Ext;
using RimWorld;
using RimWorld.Planet;
using UnityEngine;
using Verse;

namespace RimBank.Ext.Deposit;

internal static class Utility
{
    private static int maintainanceFee;

    private static HashSet<Thing> cachedSilver;

    private static int currentColumnType;

    private static bool? allowDateColumnType;

    private static readonly string[] columnSelectionLabels;

    private static readonly float[] spacewidth;

    private static ThingDef bankNoteDef;

    static Utility()
    {
        maintainanceFee = -1;
        currentColumnType = 0;
        allowDateColumnType = null;
        columnSelectionLabels = new string[]
        {
            "LabelMarketValue".Translate(),
            "LabelMass".Translate(),
            "LabelDaysLeft".Translate()
        };
        spacewidth = null;
        spacewidth = new float[3];
        var font = Text.Font;
        Text.Font = GameFont.Tiny;
        spacewidth[0] = Text.CalcSize(" ").x;
        Text.Font = GameFont.Small;
        spacewidth[1] = Text.CalcSize(" ").x;
        Text.Font = GameFont.Medium;
        spacewidth[2] = Text.CalcSize(" ").x;
        Text.Font = font;
    }

    public static double MassValue => 0.5;

    public static int StaticChamberFeePerPawn => 200;

    public static int VaultRent => CapacityExpansion.VaultRent;

    public static int WarehouseRent => CapacityExpansion.WarehouseRent;

    public static int FineForDelay => 150;

    public static int WarehouseCapacity => CapacityExpansion.WarehouseCapacity;

    public static int VaultCapacity => CapacityExpansion.VaultCapacity;

    public static int DropPodCapacityPerPod => 300;

    public static int DropPodCapacityTotal => DropPodCapacityPerPod * Static.dropPodCount;

    public static int DropPodCost => 500;

    public static int WarehouseMaintainanceFee
    {
        get
        {
            if (maintainanceFee < 0)
            {
                maintainanceFee = (int)MarketValueMultiplier.Resolve(CalculateWarehouseMarketValue());
            }

            return maintainanceFee;
        }
    }

    public static void UpdateCurrencyCountMassBased(this TradeDeal This)
    {
        var pair = This.CalculateMass();
        var num = (int)Math.Ceiling(((pair.First + pair.Second) * MassValue) - 0.0001);
        This.CurrencyTradeable.ForceTo(-num);
    }

    public static void UpdateCurrencyCountStaticChamber(this TradeDeal This)
    {
        var num = 0;
        foreach (var allTradeable in This.AllTradeables)
        {
            if (allTradeable is Tradeable_Pawn { ActionToDo: TradeAction.PlayerSells } tradeable_Pawn)
            {
                num -= tradeable_Pawn.CountToTransfer * StaticChamberFeePerPawn;
            }
        }

        This.CurrencyTradeable.ForceTo(-num);
    }

    public static Pair<double, double> CalculateMass(this TradeDeal This, bool skipPawn = false)
    {
        var num = 0.0;
        var num2 = 0.0;
        foreach (var allTradeable in This.AllTradeables)
        {
            if (!ShouldBeTradeable(allTradeable) || (!skipPawn || allTradeable is Tradeable_Pawn) && skipPawn)
            {
                continue;
            }

            if (allTradeable.ActionToDo == TradeAction.PlayerBuys)
            {
                num += allTradeable.CountToTransfer * allTradeable.ThingDef.BaseMass;
            }
            else if (allTradeable.ActionToDo == TradeAction.PlayerSells)
            {
                num2 -= allTradeable.CountToTransfer * allTradeable.ThingDef.BaseMass;
            }
        }

        return new Pair<double, double>(num, num2);
    }

    public static double CalculateMarketValue(this TradeDeal This)
    {
        var num = 0.0;
        var num2 = 0.0;
        foreach (var allTradeable in This.AllTradeables)
        {
            if (!ShouldBeTradeable(allTradeable))
            {
                continue;
            }

            if (allTradeable.ActionToDo == TradeAction.PlayerBuys)
            {
                num += allTradeable.CountToTransfer * allTradeable.AnyThing.MarketValue;
            }
            else if (allTradeable.ActionToDo == TradeAction.PlayerSells)
            {
                num2 -= allTradeable.CountToTransfer * allTradeable.AnyThing.MarketValue;
            }
        }

        return num2 - num;
    }

    public static double CalculateWarehouseUsage()
    {
        var num = 0.0;
        foreach (var item in Static.contentWarehouse)
        {
            num += item.stackCount * item.def.BaseMass;
        }

        return num;
    }

    public static double CalculateWarehouseMarketValue()
    {
        var num = 0.0;
        foreach (var item in Static.contentWarehouse)
        {
            num += item.MarketValue * item.stackCount;
        }

        return num;
    }

    public static int CalculateVaultUsage()
    {
        return Static.contentVaultSilver + (Static.contentVaultBanknote * 1000);
    }

    public static bool CheckWarehouseViolation()
    {
        foreach (var allTradeable in TradeSession.deal.AllTradeables)
        {
            if (Static.IsWarehousePutRestricted && !allTradeable.IsCurrency &&
                allTradeable.ActionToDo == TradeAction.PlayerSells)
            {
                return true;
            }

            if (Static.IsWarehouseGetRestricted && allTradeable.ActionToDo == TradeAction.PlayerBuys)
            {
                return true;
            }
        }

        return false;
    }

    [Obsolete("Use Utility.MakeFloatMenuSelectPaymentSource() instead.", true)]
    public static bool CheckSilverEnoughForFine(int fine)
    {
        cachedSilver = new HashSet<Thing>();
        var visibleMap = Find.CurrentMap;
        foreach (var item in Building_OrbitalTradeBeacon.AllPowered(visibleMap))
        {
            foreach (var tradeableCell in item.TradeableCells)
            {
                var thingList = tradeableCell.GetThingList(visibleMap);
                foreach (var thing in thingList)
                {
                    if (thing.def == ThingDefOf.Silver && !cachedSilver.Contains(thing))
                    {
                        cachedSilver.Add(thing);
                    }
                }
            }
        }

        foreach (var item2 in cachedSilver)
        {
            fine -= item2.stackCount;
            if (fine <= 0)
            {
                return true;
            }
        }

        return false;
    }

    [Obsolete("Use Utility.MakeFloatMenuSelectPaymentSource() instead.", true)]
    public static void ExtractSilverForFine(int fine)
    {
        foreach (var item in cachedSilver)
        {
            fine -= item.stackCount;
            if (fine > 0)
            {
                item.DeSpawn();
                if (!item.Destroyed)
                {
                    item.Destroy();
                }

                continue;
            }

            item.stackCount = -fine;
            break;
        }
    }

    public static Pair<int, int> SimulateExchange(int silver, int notes, int rent)
    {
        var num = 1.000002f + ExtUtil.BrokerageFactor(-1);
        var num2 = rent % 1000;
        var num3 = rent / 1000;
        if (num2 > silver)
        {
            if (num2 >= (int)(num * 1000f))
            {
                num2 = 0;
                num3++;
            }
            else
            {
                notes--;
                silver += (int)(num * 1000f);
            }
        }

        silver -= num2;
        notes -= num3;
        return new Pair<int, int>(notes, silver);
    }

    public static void Recache()
    {
        cachedSilver = new HashSet<Thing>();
        maintainanceFee = -1;
    }

    public static void InvokeCurrencyConsumer(Window parent, int expense, string[] tipstrings,
        bool isVaultDefault = false)
    {
        var pawn = isVaultDefault
            ? Find.WorldPawns.AllPawnsAlive.First()
            : Find.CurrentMap.mapPawns.FreeColonistsSpawned.First();
        TradeSession.SetupWith(new Trader_CurrencyConsumer(parent, tipstrings, isVaultDefault), pawn, false);
        Trade.Utility.CacheNotes();
        var silverTradeable = TradeSession.deal.CurrencyTradeable;
        silverTradeable.ForceTo(-Math.Abs(expense));
        Trade.Utility.AskPayByBankNotes(silverTradeable, true);
    }

    public static void MakeFloatMenuSelectPaymentSource(Window parent, int expense, string[] tipstrings = null,
        Action preAction = null, Action localPaymentHandler = null, Action vaultPaymentHandler = null,
        bool enableVaultEvenRestricted = false)
    {
        if (preAction == null)
        {
            preAction = delegate { };
        }

        var list = new List<FloatMenuOption>();
        string text;
        bool disabled;
        if (Static.IsVaultRented)
        {
            if (Static.IsVaultRestricted && !enableVaultEvenRestricted)
            {
                text = "PaySourceVaultRestricted".Translate();
                disabled = true;
            }
            else if (CalculateVaultUsage() < expense)
            {
                text = "PaySourceVaultNotEnough".Translate();
                disabled = true;
            }
            else
            {
                text = "PaySourceVault".Translate();
                disabled = false;
            }
        }
        else
        {
            text = "PaySourceVaultNotRented".Translate();
            disabled = true;
        }

        var floatMenuOption = new FloatMenuOption(text, delegate
        {
            if (vaultPaymentHandler != null)
            {
                vaultPaymentHandler();
            }
            else
            {
                preAction();
                ConsumeCurrencyVault(parent, expense);
            }
        })
        {
            Disabled = disabled
        };
        list.Add(floatMenuOption);
        list.Add(new FloatMenuOption("PaySourceLocal".Translate(), delegate
        {
            if (localPaymentHandler != null)
            {
                localPaymentHandler();
            }
            else
            {
                preAction();
                InvokeCurrencyConsumer(parent, expense, tipstrings);
            }
        }));
        Find.WindowStack.Add(new FloatMenu(list));
    }

    public static void ConsumeCurrencyVault(Window parent, int expense)
    {
        var pair = SimulateExchange(Static.contentVaultSilver, Static.contentVaultBanknote, expense);
        Static.contentVaultBanknote = pair.First;
        Static.contentVaultSilver = pair.Second;
        ((ICurrencyConsumer)parent).Consumed = true;
        Messages.Message("MsgPaymentVaultComplete".Translate(expense), MessageTypeDefOf.PositiveEvent);
    }

    public static int PodCountToSendMassBased(int mass)
    {
        var num = mass / DropPodCapacityPerPod;
        if (mass % DropPodCapacityPerPod != 0)
        {
            num++;
        }

        return num;
    }

    public static void DrawColumnSelectionButton(Rect rect)
    {
        if (!allowDateColumnType.HasValue)
        {
            allowDateColumnType = TradeSession.trader is Trader_StaticChamber ||
                                  TradeSession.trader is Trader_GlobalDropPod && !Static.IsStaticChamberNull;
        }

        if (Widgets.ButtonText(rect, columnSelectionLabels[currentColumnType], true, false))
        {
            var list = new List<int>
            {
                0,
                1
            };
            if (allowDateColumnType == true)
            {
                list.Add(2);
            }

            FloatMenuUtility.MakeMenu(list, i => columnSelectionLabels[i], i => delegate { currentColumnType = i; });
        }

        TooltipHandler.TipRegion(rect, "TipSelectColumn".Translate());
    }

    public static void SetColumnToDateColumn()
    {
        currentColumnType = 2;
    }

    public static void ResetColumnType()
    {
        currentColumnType = 0;
        allowDateColumnType = null;
    }

    public static void TryDrawCustomColumn(Rect rect, Tradeable trad, bool drawMassForCurrency = false)
    {
        switch (currentColumnType)
        {
            case 0:
                DrawMarketValueColumn(rect, trad);
                break;
            case 1:
                DrawMassColumn(rect, trad, drawMassForCurrency);
                break;
            case 2:
                DrawDaysBeforeExpired(rect, trad);
                break;
        }
    }

    public static void DrawMarketValueColumn(Rect rect, Tradeable trad)
    {
        if (!ShouldBeTradeable(trad))
        {
            return;
        }

        rect = rect.Rounded();
        if (Mouse.IsOver(rect))
        {
            Widgets.DrawHighlight(rect);
        }

        TooltipHandler.TipRegion(rect, "TipTradeableMarketValue".Translate());
        GUI.color = Color.white;
        var rect2 = new Rect(rect);
        rect2.xMax -= 5f;
        rect2.xMin += 5f;
        if (Text.Anchor == TextAnchor.MiddleLeft)
        {
            rect2.xMax += 300f;
        }

        Widgets.Label(rect2, trad.BaseMarketValue.ToStringMoney());
    }

    public static void DrawMassColumn(Rect rect, Tradeable trad, bool drawForCurrency = false)
    {
        if (!ShouldBeTradeable(trad) && !drawForCurrency)
        {
            return;
        }

        rect = rect.Rounded();
        if (Mouse.IsOver(rect))
        {
            Widgets.DrawHighlight(rect);
        }

        TooltipHandler.TipRegion(rect, "TipTradeableMass".Translate());
        GUI.color = Color.white;
        var rect2 = new Rect(rect);
        rect2.xMax -= 5f;
        rect2.xMin += 5f;
        if (Text.Anchor == TextAnchor.MiddleLeft)
        {
            rect2.xMax += 300f;
        }

        Widgets.Label(rect2, "FormatMass".Translate(trad.ThingDef.BaseMass.ToString("F2")));
    }

    public static void DrawDaysBeforeExpired(Rect rect, Tradeable trad)
    {
        if (!(trad is Tradeable_Pawn tradeable_Pawn) || tradeable_Pawn.CountHeldBy(Transactor.Trader) <= 0)
        {
            return;
        }

        rect = rect.Rounded();
        if (Mouse.IsOver(rect))
        {
            Widgets.DrawHighlight(rect);
        }

        TooltipHandler.TipRegion(rect, new TipSignal("TipDaysLeft".Translate(), rect.GetHashCode() / 2));
        var array = new double[tradeable_Pawn.thingsTrader.Count];
        int i;
        for (i = 0; i < array.Length; i++)
        {
            array[i] = 420000 - Find.TickManager.TicksAbs +
                       Static.ticksStaticChamber[
                           Static.contentStaticChamber.IndexOf((Pawn)tradeable_Pawn.thingsTrader[i])];
            array[i] /= 60000.0;
        }

        Array.Sort(array);
        if (i > 0)
        {
            for (i = 1; i < array.Length; i++)
            {
                TooltipHandler.TipRegion(rect,
                    new TipSignal("FormatDaysLeft".Translate(array[i].ToString("F1")),
                        rect.GetHashCode() * i));
            }
        }

        if (array[0] < 1.501)
        {
            GUI.color = Color.red;
        }

        var rect2 = new Rect(rect);
        rect2.xMax -= 5f;
        rect2.xMin += 5f;
        if (Text.Anchor == TextAnchor.MiddleLeft)
        {
            rect2.xMax += 300f;
        }

        Widgets.Label(rect2, "FormatDaysLeft".Translate(array[0].ToString("F1")));
        GUI.color = Color.white;
    }

    public static void DrawTradeableRow(Rect rect, Tradeable trad, int index, bool forceAdjustCurrency = false,
        bool forceDrawMass = false)
    {
        if (index % 2 == 1)
        {
            GUI.DrawTexture(rect, TexUI.GrayTextBG);
        }

        Text.Font = GameFont.Small;
        GUI.BeginGroup(rect);
        var width = rect.width;
        var num = trad.CountHeldBy(Transactor.Trader);
        if (num != 0)
        {
            var rect2 = new Rect(width - 75f, 0f, 75f, rect.height);
            if (Mouse.IsOver(rect2))
            {
                Widgets.DrawHighlight(rect2);
            }

            Text.Anchor = TextAnchor.MiddleRight;
            var rect3 = rect2;
            rect3.xMin += 5f;
            rect3.xMax -= 5f;
            Widgets.Label(rect3, num.ToStringCached());
            TooltipHandler.TipRegion(rect2, "TraderCount".Translate());
            var unused = new Rect(rect2.x - 100f, 0f, 100f, rect.height);
            Text.Anchor = TextAnchor.MiddleRight;
        }

        width -= 175f;
        var rect4 = new Rect(width - 240f, 0f, 240f, rect.height);
        if (forceAdjustCurrency)
        {
            ExtUtil.DoCountAdjustInterfaceForSilver(rect4, trad, index, -trad.CountHeldBy(Transactor.Colony),
                trad.CountHeldBy(Transactor.Trader), false);
        }
        else
        {
            TransferableUIUtility.DoCountAdjustInterface(rect4, trad, index, -trad.CountHeldBy(Transactor.Colony),
                trad.CountHeldBy(Transactor.Trader));
        }

        width -= 240f;
        var rect5 = new Rect(width - 100f, 0f, 100f, rect.height);
        Text.Anchor = TextAnchor.MiddleLeft;
        TryDrawCustomColumn(rect5, trad, forceDrawMass);
        var num2 = trad.CountHeldBy(Transactor.Colony);
        if (num2 != 0)
        {
            var rect6 = new Rect(rect5.x - 75f, 0f, 75f, rect.height);
            if (Mouse.IsOver(rect6))
            {
                Widgets.DrawHighlight(rect6);
            }

            Text.Anchor = TextAnchor.MiddleLeft;
            var rect7 = rect6;
            rect7.xMin += 5f;
            rect7.xMax -= 5f;
            Widgets.Label(rect7, num2.ToStringCached());
            TooltipHandler.TipRegion(rect6, "ColonyCount".Translate());
        }

        width -= 175f;
        var idRect = new Rect(0f, 0f, width, rect.height);
        TransferableUIUtility.DrawTransferableInfo(trad, idRect, Color.white);
        GenUI.ResetLabelAlign();
        GUI.EndGroup();
    }

    public static string PadLeft(this string str, float width)
    {
        width -= Text.CalcSize(str).x;
        var num = (int)Math.Round(width / spacewidth[(uint)Text.Font]);
        var text = str;
        str = text.PadLeft(text.Length + num);
        return str;
    }

    public static void _debugOutputContentWarehouse()
    {
        foreach (var item in Static.contentWarehouse)
        {
            Log.Message(item.def.defName + "*" + item.stackCount);
        }
    }

    public static void _debugOutputContentStaticChamber()
    {
        foreach (var item in Static.contentStaticChamber)
        {
            Log.Message(item.NameShortColored + ",Humanlike=" + (item.RaceProps is { Humanlike: true }) +
                        ",IsPrisoner=" + item.IsPrisoner);
        }

        foreach (var item2 in Static.ticksStaticChamber)
        {
            Log.Message("enterTick=" + item2);
        }
    }

    [Obsolete("Deprecated debug tool.")]
    public static void _debugOutputMarketVal()
    {
        var text = "";
        foreach (var item in MarketValueMultiplier.ResolveExplanation(CalculateWarehouseMarketValue()))
        {
            text = text + item + "\n";
        }

        Log.Message(text);
    }

    public static IntVec3 FindDropSpotWith(Pawn pawn, bool pawnCentered = false)
    {
        if (!pawnCentered)
        {
            return DropCellFinder.TradeDropSpot(pawn.Map);
        }

        DropCellFinder.TryFindDropSpotNear(pawn.Position, pawn.Map, out var result, false, false);
        return result;
    }

    public static void TryDelieverThingLocal(Thing thing, Map map)
    {
        if (thing is Pawn { IsPrisoner: true } pawn)
        {
            TradeUtility.SpawnDropPod(Trader_StaticChamber.IndoorDropCell(map), map, pawn);
        }
        else
        {
            TradeUtility.SpawnDropPod(DropCellFinder.TradeDropSpot(map), map, thing);
        }
    }

    public static void TryDeliverThingsLocalNearPos(List<Thing> list, Map map, IntVec3 center)
    {
        foreach (var item in list)
        {
            if (!DropCellFinder.TryFindDropSpotNear(center, map, out var dropSpot, false, false) &&
                !DropCellFinder.TryFindDropSpotNear(center, map, out dropSpot, false, true))
            {
                Log.ErrorOnce("Failed to find a valid cell for drop pods.", 179573);
            }

            TradeUtility.SpawnDropPod(dropSpot, map, item);
        }
    }

    public static void TryDeliverThingsGlobal(List<Thing> list)
    {
        Find.WorldObjects.Caravans.Where(c => c.IsPlayerControlled).TryRandomElement(out var result);
        TryDeliverThingsGlobal(list, result, ref PawnsArrivalModeDefOf.EdgeDrop);
    }

    public static void TryDeliverThingsGlobal(List<Thing> list, WorldObject target, ref PawnsArrivalModeDef arriveMode,
        bool forceCreateCaravan = false, bool forceAttack = false)
    {
        if (arriveMode == null)
        {
            arriveMode = PawnsArrivalModeDefOf.EdgeDrop;
        }

        //IL_0052: Unknown result type (might be due to invalid IL or missing references)
        //IL_0054: Unknown result type (might be due to invalid IL or missing references)
        var activeDropPodInfo = new ActiveDropPodInfo();
        activeDropPodInfo.innerContainer.TryAddRangeOrTransfer(list);
        var pod = new TravelingTransportPods
        {
            def = WorldObjectDefOf.TravelingTransportPods
        };
        pod.SetFaction(Faction.OfPlayer);
        pod.arrivalAction = new TransportPodsArrivalAction_GiveGift();
        pod.Tile = target.Tile;
        pod.destinationTile = target.Tile;
        Find.WorldObjects.Add(pod);
        pod.AddPod(activeDropPodInfo, false);
        if (!forceCreateCaravan && target is MapParent { Map: { } } mapParent)
        {
            LongEventHandler.QueueLongEvent(delegate
            {
                var orGenerateMap = GetOrGenerateMapUtility.GetOrGenerateMap(mapParent.Tile, null);
                string text = null;
                if (forceAttack && mapParent.Faction != null && !mapParent.Faction.HostileTo(Faction.OfPlayer))
                {
                    mapParent.Faction.TryAffectGoodwillWith(Faction.OfPlayer,
                        mapParent.Faction.GoodwillToMakeHostile(Faction.OfPlayer));
                    text = "MessageTransportPodsArrived_BecameHostile".Translate(mapParent.Faction.Name)
                        .CapitalizeFirst();
                }

                Find.TickManager.CurTimeSpeed = TimeSpeed.Paused;
                typeof(TravelingTransportPods)
                    .GetMethod("SpawnDropPodsInMap", BindingFlags.Instance | BindingFlags.NonPublic)
                    ?.Invoke(pod, new object[] { orGenerateMap, text });
            }, "GeneratingMapForNewEncounter", false, null);
            return;
        }

        var noPawns = true;
        foreach (var item in list)
        {
            if (item is not Pawn)
            {
                continue;
            }

            noPawns = false;
            break;
        }

        if (noPawns && !forceCreateCaravan)
        {
            typeof(TravelingTransportPods)
                .GetMethod("GivePodContentsToCaravan", BindingFlags.Instance | BindingFlags.NonPublic)
                ?.Invoke(pod, new object[] { (Caravan)target });
        }
        else
        {
            typeof(TravelingTransportPods)
                .GetMethod("SpawnCaravanAtDestinationTile", BindingFlags.Instance | BindingFlags.NonPublic)
                ?.Invoke(pod, null);
        }
    }

    public static void TryDeliverThings(List<Thing> list)
    {
        if (list.Count == 0)
        {
            return;
        }

        var visibleMap = Find.CurrentMap;
        if (visibleMap != null)
        {
            foreach (var item in list)
            {
                TryDelieverThingLocal(item, visibleMap);
            }

            return;
        }

        TryDeliverThingsGlobal(list);
    }

    public static bool RemoveAllTickComponentsFromGame()
    {
        var result = false;
        var components = Find.World.components;
        for (var i = 0; i < components.Count; i++)
        {
            if (components[i] is not WorldComponent_RimBankExtDeposit)
            {
                continue;
            }

            components.RemoveAt(i);
            result = true;
        }

        return result;
    }

    public static bool ShouldBeTradeable(Tradeable t)
    {
        return !t.IsCurrency && !isBankNote(t);
    }

    public static bool isBankNote(Tradeable t)
    {
        return t.ThingDef.defName == "BankNote";
    }

    public static bool isBankNote(Thing t)
    {
        return t.def.defName == "BankNote";
    }

    public static ThingDef BankNoteDef()
    {
        if (bankNoteDef == null)
        {
            bankNoteDef = DefDatabase<ThingDef>.GetNamed("BankNote");
        }

        return bankNoteDef;
    }
}