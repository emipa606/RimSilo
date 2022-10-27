using System;
using UnityEngine;
using Verse;

namespace RimBank.Ext.Deposit;

public class Dialog_MaintainanceFeeVisual : Window
{
    private double num;

    private float x;

    public override Vector2 InitialSize => new Vector2(1024f, 400f);

    public override void PreOpen()
    {
        base.PreOpen();
        closeOnCancel = true;
        forcePause = true;
        absorbInputAroundWindow = true;
    }

    public override void DoWindowContents(Rect inRect)
    {
        GUI.BeginGroup(inRect);
        Widgets.Label(new Rect(0f, 0f, inRect.width, 250f), "MaintainanceFeeDesc".Translate());
        var position = new Rect(0f, 120f, inRect.width, 35f);
        GUI.DrawTexture(position, TexUI.GrayTextBG);
        if (Mouse.IsOver(new Rect(0f, position.y - 20f, inRect.width, 75f)))
        {
            x = Event.current.mousePosition.x;
            num = x / position.width * 250000f;
        }

        var position2 = new Rect(x - 12f, position.y - 25f, 25f, 25f);
        var color2 = GUI.color = MarketValueMultiplier.ResolveColor(num);
        GUI.DrawTexture(position2, StaticConstructor.TexPin);
        DrawLineVertical(x, position.y - 1f, position.height, color2);
        var text = MarketValueMultiplier.ResolveMaxMultiplierString(num);
        var vector = Text.CalcSize(text);
        Widgets.Label(new Rect(x + 5f, position.yMax - vector.y - 7f, vector.x, vector.y), text);
        GUI.color = Color.gray;
        var yMax = position.yMax;
        var vector2 = default(Vector2);
        foreach (var stage in MarketValueMultiplier.stages)
        {
            var text2 = stage.ToString();
            vector2 = Text.CalcSize(text2);
            var rect = new Rect(inRect.width * stage / 250000f, yMax, vector2.x, vector2.y);
            DrawLineVertical(inRect.width * stage / 250000f, yMax - 5f, 5f, Color.gray);
            Widgets.Label(rect, text2);
        }

        GUI.color = Color.white;
        var rect2 = new Rect(0f, position.yMax + vector2.y, inRect.width, 400f);
        var text3 = "MaintainanceFeeText".Translate(num.ToString("F0"),
            MarketValueMultiplier.Resolve(num).ToString("F0"));
        Widgets.Label(rect2, text3);
        vector2 = Text.CalcSize(text3);
        yMax = rect2.y + vector2.y + 7f;
        var list = MarketValueMultiplier.ResolveRawExplanationString(num);
        var num2 = list.Count / 4;
        var array = new[] { "", "", "", "", "", "" };
        for (num2--; num2 > -1; num2--)
        {
            ref var reference = ref array[5];
            reference = $"{reference}{list[num2 * 4]}\n";
            array[1] += " ~ \n";
            reference = ref array[4];
            reference = $"{reference}{list[(num2 * 4) + 1]}\n";
            array[3] += " :  $ \n";
            reference = ref array[2];
            reference = $"{reference}{list[(num2 * 4) + 2]}\n";
            reference = ref array[0];
            reference = $"{reference}{list[(num2 * 4) + 3]}\n";
        }

        var rect3 = new Rect(25f, yMax, 0f, 0f);
        for (num2 = 0; num2 < 6; num2++)
        {
            vector2 = Text.CalcSize(array[num2]);
            rect3 = new Rect(rect3.xMax + 2f + (num2 == 5 ? 5f : 0f), yMax, vector2.x, vector2.y);
            Widgets.Label(rect3, array[num2]);
        }

        if (Widgets.ButtonText(new Rect(inRect.xMax - 55f - 160f, inRect.yMax - 25f - 40f, 160f, 40f),
                "CloseButton".Translate(), true, false))
        {
            Close();
        }

        GUI.EndGroup();
    }

    public override void PostClose()
    {
        base.PostClose();
        GC.Collect();
    }

    public static void DrawLineVertical(float x, float y, float length, Color color)
    {
        GUI.DrawTexture(new Rect(x, y, 1f, length), SolidColorMaterials.NewSolidColorTexture(color));
    }
}