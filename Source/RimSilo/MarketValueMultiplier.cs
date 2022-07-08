using System;
using System.Collections.Generic;
using UnityEngine;

namespace RimBank.Ext.Deposit;

internal static class MarketValueMultiplier
{
    public static readonly int[] stages = { 0, 30000, 50000, 100000, 200000 };

    public static readonly double[] multiplier = { 0.0, 0.01, 0.02, 0.04, 0.05 };

    public static double Resolve(double val)
    {
        var num = 0.0;
        for (var num2 = stages.Length - 1; num2 > -1; num2--)
        {
            if (!(val > stages[num2]))
            {
                continue;
            }

            num += (val - stages[num2]) * multiplier[num2];
            val = stages[num2];
        }

        return num;
    }

    public static double ResolveMaxMultiplier(double val)
    {
        int i;
        for (i = 1; i < stages.Length && !(val <= stages[i]); i++)
        {
        }

        return multiplier[i - 1];
    }

    public static string ResolveMaxMultiplierString(double val)
    {
        return (ResolveMaxMultiplier(val) * 100.0).ToString("F0") + "%";
    }

    public static List<string> ResolveRawExplanationString(double val)
    {
        if (val == 0.0)
        {
            return new List<string>();
        }

        int i;
        for (i = 1; i < stages.Length && !(val <= stages[i]); i++)
        {
        }

        var list = new List<string>(i * 4);
        for (i--; i > -1; i--)
        {
            list.Add((multiplier[i] * 100.0).ToString("F0") + "%");
            list.Add(((val - stages[i]) * multiplier[i]).ToString("F0"));
            list.Add(val.ToString("F0"));
            list.Add(stages[i].ToString());
            val = stages[i];
        }

        return list;
    }

    [Obsolete("Deprecated debug tool.")]
    public static List<string> ResolveExplanation(double val)
    {
        var list = new List<string>();
        for (var num = stages.Length - 1; num > -1; num--)
        {
            if (!(val > stages[num]))
            {
                continue;
            }

            var num2 = (val - stages[num]) * multiplier[num];
            var text = stages[num].ToString().PadLeft(6) + " ~ " + val.ToString("F2");
            if (text.Length < 15)
            {
                text = text.PadRight(15);
            }

            text = text + "  :  $ " + num2.ToString("F0");
            text = text + " (" + (multiplier[num] * 100.0).ToString("F0") + "%)";
            list.Add(text);
            val = stages[num];
        }

        list.Reverse();
        return list;
    }

    public static Color ResolveColor(double val)
    {
        int i;
        for (i = 1; i < stages.Length && !(val <= stages[i]); i++)
        {
        }

        return i switch
        {
            1 => new Color(0.1f, 1f, 0.1f),
            2 => Color.white,
            3 => new Color(1f, 1f, 0f),
            4 => new Color(1f, 0.7216f, 0.1686f),
            5 => new Color(1f, 0f, 0f),
            _ => Color.white
        };
    }
}