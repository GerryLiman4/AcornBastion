using System;
using System.Collections.Generic;

public readonly struct CampaignSpawn
{
    public readonly UnitId Id;
    public readonly bool Marked;
    public CampaignSpawn(UnitId id, bool marked) { Id = id; Marked = marked; }
}

public static class CampaignWavePlanner
{
    public static List<CampaignSpawn> Build(CampaignRound round, Random random)
    {
        var types = new List<UnitId>(round.Total);
        int opening = Math.Min(3, round.grunts);
        for (int i = 0; i < opening; i++) types.Add(UnitId.ShadowGrunt);
        int remaining = round.grunts - opening;
        int groups = round.brutes + 1;
        for (int group = 0; group < groups; group++)
        {
            int grunts = remaining / groups + (group < remaining % groups ? 1 : 0);
            for (int i = 0; i < grunts; i++) types.Add(UnitId.ShadowGrunt);
            if (group >= round.brutes) continue;
            types.Add(UnitId.ShadowBrute);
            if (group < round.elders) types.Add(UnitId.ShadowElder);
        }

        var eligible = new List<int>();
        var early = new List<int>();
        for (int i = 0; i < types.Count; i++)
        {
            if (types[i] != UnitId.ShadowGrunt) continue;
            eligible.Add(i);
            if (i < types.Count / 2) early.Add(i);
        }
        var marked = new HashSet<int>();
        if (round.marked > 0 && early.Count > 0)
        {
            int index = early[random.Next(early.Count)];
            marked.Add(index);
            eligible.Remove(index);
        }
        while (marked.Count < round.marked && eligible.Count > 0)
        {
            int choice = random.Next(eligible.Count);
            marked.Add(eligible[choice]);
            eligible.RemoveAt(choice);
        }
        var result = new List<CampaignSpawn>(types.Count);
        for (int i = 0; i < types.Count; i++) result.Add(new CampaignSpawn(types[i], marked.Contains(i)));
        return result;
    }
}
