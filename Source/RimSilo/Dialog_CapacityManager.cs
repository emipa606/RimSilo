using System;
using RimWorld;
using UnityEngine;
using Verse;

namespace RimBank.Ext.Deposit;

public class Dialog_CapacityManager : Window, ICurrencyConsumer
{
    private readonly float descPt2YPos;
    private int baseCapacity;

    private int baseRent;
    private Rect[] blocks;

    private string cacheDescString;

    private int capacityTop;

    private int capacityUsage;

    private float[] capPosX;

    private int costPerSlot;

    private int currentSlotCount;

    private int extensionSlotsCount;

    private bool isWarehouse;

    private Rect[] labels;

    private int rentCurrent;

    private int rentPerUnit;

    private string[] reports;

    private int unit;

    private TextAnchor usageAnchor = TextAnchor.MiddleRight;

    private float xMax;

    public Dialog_CapacityManager(bool isVault = false, bool isWarehouse = false)
    {
        if (isVault)
        {
            Init_Vault();
        }
        else if (isWarehouse)
        {
            Init_Warehouse();
        }

        xMax = initReportStringsAndxMax();
        descPt2YPos = Text.CalcHeight(cacheDescString, 1024f);
        closeOnCancel = true;
        forcePause = true;
        absorbInputAroundWindow = true;
        soundAppear = SoundDefOf.CommsWindow_Open;
        soundClose = SoundDefOf.CommsWindow_Close;
        soundAmbient = SoundDefOf.RadioComms_Ambience;
    }

    public override Vector2 InitialSize => new(900f, 400f);

    bool ICurrencyConsumer.Consumed
    {
        set
        {
            if (!value)
            {
                return;
            }

            if (isWarehouse)
            {
                Static.extensionsWarehouse = currentSlotCount;
                Init_Warehouse();
            }
            else
            {
                Static.extensionsVault = currentSlotCount;
                Init_Vault();
            }

            xMax = initReportStringsAndxMax();
            capPosX = null;
        }
    }

    private void Init_Vault()
    {
        isWarehouse = false;
        capacityUsage = Utility.CalculateVaultUsage();
        capacityTop = Utility.VaultCapacity;
        baseCapacity = CapacityExpansion.VaultBaseCapacity;
        baseRent = CapacityExpansion.VaultBaseRent;
        extensionSlotsCount = CapacityExpansion.VaultExpansionSlotsCount;
        unit = CapacityExpansion.VaultCapacityPerUnit;
        rentPerUnit = CapacityExpansion.VaultRentPerUnit;
        rentCurrent = CapacityExpansion.VaultRent;
        costPerSlot = CapacityExpansion.VaultSubscriptionPerUnit;
        cacheDescString =
            "CapacityExpansionDesc".Translate("FormatVaultCapacity".Translate(unit), rentPerUnit, costPerSlot);
        currentSlotCount = Static.extensionsVault;
    }

    private void Init_Warehouse()
    {
        isWarehouse = true;
        capacityUsage = (int)Math.Ceiling(Utility.CalculateWarehouseUsage());
        capacityTop = Utility.WarehouseCapacity;
        baseCapacity = CapacityExpansion.WarehouseBaseCapacity;
        baseRent = CapacityExpansion.WarehouseBaseRent;
        extensionSlotsCount = CapacityExpansion.WarehouseExpansionSlotsCount;
        unit = CapacityExpansion.WarehouseCapacityPerUnit;
        rentPerUnit = CapacityExpansion.WarehouseRentPerUnit;
        rentCurrent = CapacityExpansion.WarehouseRent;
        costPerSlot = CapacityExpansion.WarehouseSubscriptionPerUnit;
        cacheDescString = "CapacityExpansionDesc".Translate("FormatMass".Translate(unit), rentPerUnit, costPerSlot);
        currentSlotCount = Static.extensionsWarehouse;
    }

    public override void DoWindowContents(Rect inRect)
    {
        var y = 120f;
        const float num = 18f;
        GUI.BeginGroup(inRect);
        var rect = new Rect(0f, 0f, inRect.width, 250f);
        _ = new Rect(0f, y, inRect.width, 35f);
        var rect2 = new Rect(35f, y, inRect.width - 70f, 35f);
        var rect3 = new Rect(0f, y, 35f, 35f);
        var rect4 = new Rect(inRect.width - 35f, y, 35f, 35f);
        if (currentSlotCount > 0)
        {
            if (Widgets.ButtonImage(rect3, StaticConstructor.TexDecrease))
            {
                currentSlotCount--;
                if (baseCapacity + (currentSlotCount * unit) < capacityUsage)
                {
                    Messages.Message("MsgCannotShrinkCapUsageLowerBound".Translate(), MessageTypeDefOf.RejectInput);
                    currentSlotCount++;
                }
            }
        }
        else
        {
            GUI.color = Color.gray;
            GUI.DrawTexture(rect3, StaticConstructor.TexDecrease);
        }

        if (currentSlotCount < extensionSlotsCount)
        {
            if (Widgets.ButtonImage(rect4, StaticConstructor.TexIncrease))
            {
                currentSlotCount++;
            }
        }
        else
        {
            GUI.color = Color.gray;
            GUI.DrawTexture(rect4, StaticConstructor.TexIncrease);
        }

        var num2 = rect2.yMax + fillBlocks(rect2);
        if (capPosX == null)
        {
            capPosX = new float[2];
            cachePos(capacityTop, 0);
            cachePos(capacityUsage, 1);
            labels = new Rect[2];
            var vector = Text.CalcSize("LabelCurrentMax".Translate());
            var source = new Rect(capPosX[0] - (vector.x / 2f), rect2.y - num - vector.y + 2f, vector.x, vector.y);
            labels[0] = new Rect(source);
            vector = Text.CalcSize("LabelCurrentUsage".Translate(capacityUsage));
            var num3 = capPosX[1] - vector.x;
            if (num3 < 0f || capPosX[1] - Text.CalcSize(capacityUsage.ToString()).x < blocks[0].x)
            {
                num3 = capPosX[1];
                usageAnchor = TextAnchor.MiddleLeft;
            }

            source = new Rect(num3, rect2.y - num, vector.x, vector.y);
            labels[1] = new Rect(source);
        }

        var start = new Vector2(capPosX[0], rect2.yMax);
        var end = new Vector2(capPosX[0], rect2.y - num);
        Widgets.DrawLine(start, end, Color.white, 3f);
        GUI.color = Color.white;
        Widgets.Label(labels[0], "LabelCurrentMax".Translate());
        start.x = capPosX[1];
        end.x = capPosX[1];
        end.y = rect2.y - num;
        Widgets.DrawLine(start, end, Color.green, 1f);
        GUI.color = Color.green;
        Text.Anchor = usageAnchor;
        Widgets.Label(labels[1], "LabelCurrentUsage".Translate(capacityUsage));
        Text.Anchor = TextAnchor.UpperLeft;
        reportExpense(rect);
        var rect5 = new Rect(0f, num2 + 2f, inRect.width, 100f);
        Widgets.Label(rect5, cacheDescString);
        rect5.y += descPt2YPos;
        Widgets.Label(rect5,
            "CapacityExpansionDesc2".Translate(
                Math.Max((currentSlotCount - ((capacityTop - baseCapacity) / unit)) * costPerSlot, 0)));
        var rect6 = new Rect(inRect.xMax - 55f - 160f, inRect.yMax - 25f - 40f, 160f, 40f);
        if (Widgets.ButtonText(rect6, "Confirm".Translate(), true, false))
        {
            if ((capacityTop - baseCapacity) / unit == currentSlotCount)
            {
                Close();
            }
            else if ((currentSlotCount * unit) + baseCapacity < capacityTop)
            {
                Find.WindowStack.Add(Dialog_MessageBox.CreateConfirmation("MsgBoxConfirmShrinkCap".Translate(),
                    delegate { ((ICurrencyConsumer)this).Consumed = true; }, true,
                    "MsgBoxConfirmShrinkCapTitle".Translate()));
            }
            else
            {
                Utility.MakeFloatMenuSelectPaymentSource(this,
                    (currentSlotCount - ((capacityTop - baseCapacity) / unit)) * costPerSlot, [
                        "TraderCapManTitle".Translate(), "TraderCapManTitleTip".Translate(costPerSlot),
                        "WarehouseSilverTip".Translate(),
                        "BankNoteTip".Translate()
                    ]);
            }

            Event.current.Use();
        }

        if (Widgets.ButtonText(new Rect(rect6.x - 160f - 25f, rect6.y, 160f, 40f), "CancelButton".Translate(), true,
                false))
        {
            Close();
            Event.current.Use();
        }

        GUI.EndGroup();
    }

    public override void PostClose()
    {
        base.PostClose();
        GC.Collect();
    }

    private float fillBlocks(Rect rect)
    {
        const float num = 2f;
        var num2 = extensionSlotsCount;
        if (blocks == null)
        {
            var num3 = rect.width / (num2 + 2);
            var num4 = num3 - (num * 2f);
            var rect2 = new Rect(rect.x + num, rect.y, (num3 = rect.x + num + num4 + (num * 2f) + num4) - rect.x - num,
                rect.height);
            blocks = new Rect[num2 + 1];
            blocks[0] = rect2;
            num3 += num * 2f;
            var source = new Rect(num3, rect.y, num4, rect.height);
            for (var i = 1; i <= num2; i++)
            {
                blocks[i] = new Rect(source);
                source.x = source.xMax + (num * 2f);
            }
        }

        GUI.color = Color.gray;
        var text = "0";
        var vector = Text.CalcSize(text);
        var rect3 = new Rect(blocks[0].x - num, blocks[0].yMax + 1f, vector.x, vector.y);
        Widgets.Label(rect3, text);
        for (var j = 0; j <= num2; j++)
        {
            tryFill(j);
            text = (baseCapacity + (j * unit)).ToString();
            vector = Text.CalcSize(text);
            rect3 = new Rect(blocks[j].xMax + num - (vector.x / 2f), blocks[j].yMax + 1f, vector.x, vector.y);
            if (isWarehouse || j % 2 == 0)
            {
                Widgets.Label(rect3, text);
            }
        }

        return vector.y;
    }

    private void tryFill(int index)
    {
        GUI.DrawTexture(blocks[index],
            index > currentSlotCount
                ? StaticConstructor.FillableTexEmptySlot
                : StaticConstructor.FillableTexOccupiedSlot);
    }

    private void cachePos(int num, int idx)
    {
        for (var i = 0; i <= extensionSlotsCount; i++)
        {
            var num2 = baseCapacity + (i * unit);
            if (num2 == num)
            {
                capPosX[idx] = blocks[i].xMax;
                break;
            }

            if (num >= num2)
            {
                continue;
            }

            if (i != 0)
            {
                num -= num2 - unit;
                capPosX[idx] = (blocks[i].width * (num / (float)unit)) + blocks[i].x;
            }
            else
            {
                capPosX[idx] = (blocks[i].width * (num / (float)baseCapacity)) + blocks[i].x;
            }

            break;
        }
    }

    private float initReportStringsAndxMax()
    {
        reports = new string[6];
        reports[0] = "ReportStrCapacity".Translate(capacityTop);
        reports[1] = "ReportStrBase".Translate(baseCapacity);
        reports[2] = "ReportStrExtension".Translate($"{(capacityTop - baseCapacity) / unit}*{unit}");
        reports[3] = "ReportStrRent".Translate(rentCurrent);
        reports[4] = "ReportStrBase".Translate(baseRent);
        reports[5] = "ReportStrExtension".Translate($"{(rentCurrent - baseRent) / rentPerUnit}*{rentPerUnit}");
        var num = 5f;
        var num2 = 7f;
        var text = "=";
        var val = -1f;
        _ = baseCapacity;
        _ = unit;
        _ = currentSlotCount;
        Text.Font = GameFont.Medium;
        var num3 = Text.CalcSize(reports[3]).x;
        val = Math.Max(val, num3);
        num3 += num + Text.CalcSize(text + (extensionSlotsCount * rentPerUnit)).x;
        val = Math.Max(val, num3);
        num3 = num2 + Text.CalcSize(reports[5]).x;
        val = Math.Max(val, num3);
        num3 += num + Text.CalcSize($"{text}{extensionSlotsCount}*{rentPerUnit}").x;
        return Math.Max(val, num3);
    }

    private void reportExpense(Rect rect)
    {
        const float num = 5f;
        const float num2 = 7f;
        const float num3 = 2f;
        string text;
        string text2 = null;
        var canFillMore = true;
        var num4 = baseCapacity + (unit * currentSlotCount);
        Color color2;
        Color color;
        if (capacityTop == num4)
        {
            color2 = color = Color.white;
            canFillMore = false;
        }
        else if (capacityTop > num4)
        {
            color2 = Color.red;
            color = Color.green;
            text2 = "-";
        }
        else
        {
            color2 = Color.green;
            color = Color.red;
            text2 = "+";
        }

        GUI.color = Color.white;
        Text.Font = GameFont.Medium;
        var size = Text.CalcSize(reports[0]);
        var rect2 = new Rect(rect.position, size);
        Widgets.Label(rect2, reports[0]);
        if (canFillMore)
        {
            GUI.color = color2;
            text = text2 + Math.Abs(capacityTop - num4);
            size = Text.CalcSize(text);
            rect2 = new Rect(rect2.xMax + num, rect2.y, size.x, size.y);
            Widgets.Label(rect2, text);
            GUI.color = Color.white;
        }

        Text.Font = GameFont.Small;
        size = Text.CalcSize(reports[1]);
        rect2 = new Rect(rect.x + num2, rect2.yMax + num3, size.x, size.y);
        Widgets.Label(rect2, reports[1]);
        size = Text.CalcSize(reports[2]);
        rect2 = new Rect(rect.x + num2, rect2.yMax + num3, size.x, size.y);
        Widgets.Label(rect2, reports[2]);
        if (canFillMore)
        {
            GUI.color = color2;
            text = $"{text2}{Math.Abs(((capacityTop - baseCapacity) / unit) - currentSlotCount)}*{unit}";
            size = Text.CalcSize(text);
            rect2 = new Rect(rect2.xMax + num, rect2.y, size.x, size.y);
            Widgets.Label(rect2, text);
            GUI.color = Color.white;
        }

        var num5 = rect.xMax - xMax - num;
        Text.Font = GameFont.Medium;
        size = Text.CalcSize(reports[3]);
        rect2 = new Rect(rect.position, size)
        {
            x = num5
        };
        Widgets.Label(rect2, reports[3]);
        if (canFillMore)
        {
            GUI.color = color;
            text = text2 + (Math.Abs(((capacityTop - baseCapacity) / unit) - currentSlotCount) * rentPerUnit);
            size = Text.CalcSize(text);
            rect2 = new Rect(rect2.xMax + num, rect2.y, size.x, size.y);
            Widgets.Label(rect2, text);
            GUI.color = Color.white;
        }

        Text.Font = GameFont.Small;
        size = Text.CalcSize(reports[4]);
        rect2 = new Rect(num5 + num2, rect2.yMax + num3, size.x, size.y);
        Widgets.Label(rect2, reports[4]);
        size = Text.CalcSize(reports[5]);
        rect2 = new Rect(num5 + num2, rect2.yMax + num3, size.x, size.y);
        Widgets.Label(rect2, reports[5]);
        if (!canFillMore)
        {
            return;
        }

        GUI.color = color;
        text = $"{text2}{Math.Abs(((capacityTop - baseCapacity) / unit) - currentSlotCount)}*{rentPerUnit}";
        size = Text.CalcSize(text);
        rect2 = new Rect(rect2.xMax + num, rect2.y, size.x, size.y);
        Widgets.Label(rect2, text);
        GUI.color = Color.white;
    }
}