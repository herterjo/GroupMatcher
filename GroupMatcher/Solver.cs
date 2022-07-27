using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Z3;

namespace GroupMatcher;
public class Solver : Context
{
    private readonly Config Config;
    private readonly IEnumerable<IntNum> GroupEnumerable;
    private readonly IntExpr[] FemaleMembers;
    private readonly IntExpr[] MaleMembers;
    private readonly IntExpr[] FemaleLeaders;
    private readonly IntExpr[] MaleLeaders;

    private readonly IntNum Zero;
    private readonly IntNum One;

    public Solver(Config config) : base(new Dictionary<string, string>() { { "model", "true" } })
    {
        this.Config = config ?? throw new ArgumentNullException(nameof(config));
        this.GroupEnumerable = Enumerable.Repeat(0, (int)Config.GroupCount).Select((_, index)=> this.MkInt(index)).ToList();

        FemaleMembers = GetZ3Variables(config.FemaleMembers);
        MaleMembers = GetZ3Variables(config.MaleMembers);
        FemaleLeaders = GetZ3Variables(config.FemaleLeaders);
        MaleLeaders = GetZ3Variables(config.MaleLeaders);

        Zero = this.MkInt(0);
        One = this.MkInt(1);
    }

    public void Solve()
    {
        var now = DateTime.Now.ToString("yyyy-MM-dd_HH-mm-ss");
        var groupEnumerable = Enumerable.Repeat("", (int)Config.GroupCount);

        using (var solver = this.MkOptimize())
        {
            solver.Assert(AllInSomeGroup(FemaleMembers));
            solver.Assert(AllInSomeGroup(MaleMembers));
            solver.Assert(AllInSomeGroup(FemaleLeaders));
            solver.Assert(AllInSomeGroup(MaleLeaders));

            solver.Assert(GetMinMaxExpr(FemaleMembers, Config.MinFemaleGroupMembers, Config.MaxFemaleGroupMembers));
            solver.Assert(GetMinMaxExpr(MaleMembers, Config.MinMaleGroupMembers, Config.MaxMaleGroupMembers));
            solver.Assert(GetMinMaxExpr(FemaleLeaders, Config.MinFemaleGroupLeaders, Config.MaxFemaleGroupLeaders));
            solver.Assert(GetMinMaxExpr(MaleLeaders, Config.MinMaleGroupLeaders, Config.MaxMaleGroupLeaders));

            solver.Assert(GetMinMaxExpr(FemaleLeaders.Concat(MaleLeaders), Config.MinGroupLeaders, Config.MaxGroupLeaders));
            solver.Assert(GetMinMaxExpr(FemaleMembers.Concat(MaleMembers), Config.MinGroupMembers, Config.MaxGroupMembers));


            var status = solver.Check();

            File.WriteAllText($"output_{now}.z3", solver.ToString());

            if (status != Status.SATISFIABLE)
            {
                throw new Exception("Could not evaluate with these static count parameters");
                //solver = this.MkOptimize();
                //TODO: assert soft basic int constraints, check which are true
            }

            using (var model = solver.Model)
            {
                var evaluatedFemaleMembers = GetGrouped(model, FemaleMembers);
                var evaluatedMaleMembers = GetGrouped(model, MaleMembers);
                var evaluatedFemaleLeaders = GetGrouped(model, FemaleLeaders);
                var evaluatedMaleLeaders = GetGrouped(model, MaleLeaders);

                var csvContent = new StringBuilder("");

                csvContent = csvContent.AppendCsvLine(groupEnumerable.Select((_, index) => index + 1));

                csvContent = AppendCsvPeople(csvContent, evaluatedFemaleLeaders);
                csvContent = AppendCsvPeople(csvContent, evaluatedMaleLeaders);
                csvContent = AppendCsvPeople(csvContent, evaluatedFemaleMembers);
                csvContent = AppendCsvPeople(csvContent, evaluatedMaleMembers);

                File.WriteAllText($"output_{now}.csv", csvContent.ToString());

                var txtContent = new StringBuilder("");
                for (var i = 0; i < Config.GroupCount; i++)
                {
                    txtContent = txtContent.Append("Group " + (i + 1));
                    txtContent = AppendTxtPeople(txtContent, evaluatedFemaleLeaders, i);
                    txtContent = AppendTxtPeople(txtContent, evaluatedMaleLeaders, i);
                    txtContent = AppendTxtPeople(txtContent, evaluatedFemaleMembers, i);
                    txtContent = AppendTxtPeople(txtContent, evaluatedMaleMembers, i);

                    txtContent = txtContent.AppendLine();
                    txtContent = txtContent.AppendLine();
                }

                File.WriteAllText($"output_{now}.txt", txtContent.ToString());
            }
        }
    }

    private IEnumerable<BoolExpr> GetMinMaxExpr(IEnumerable<IntExpr> people, uint? min, uint? max)
    {
        if (!min.HasValue && !max.HasValue)
        {
            return Enumerable.Empty<BoolExpr>();
        }
        var allExpr = new LinkedList<BoolExpr>();
        var minExpr = min.HasValue ? this.MkInt(min.Value) : null;
        var maxExpr = max.HasValue ? this.MkInt(max.Value) : null;
        foreach (var group in GroupEnumerable)
        {
            var groupCount = people.Select(p => (IntExpr)this.MkITE(this.MkEq(p, group), One, Zero)).Aggregate((ArithExpr)Zero, (prev, current) => prev + current);
            if (minExpr != null)
            {
                allExpr.AddFirst(groupCount >= minExpr);
            }
            if (maxExpr != null)
            {
                allExpr.AddFirst(groupCount <= maxExpr);
            }
        }
        return allExpr;
    }

    private BoolExpr AllInSomeGroup(IEnumerable<IntExpr> people)
    {
        var allInSomeGroup = this.MkTrue();
        foreach (var person in people)
        {
            var personInSomeGroup = this.MkFalse();
            foreach (var group in GroupEnumerable)
            {
                personInSomeGroup |= this.MkEq(group, person);
            }
            allInSomeGroup &= personInSomeGroup;
        }
        return allInSomeGroup;
    }

    private static StringBuilder AppendTxtPeople(StringBuilder txtContent, Dictionary<int, string[]> groupedPeople, int groupNumber)
    {
        txtContent.AppendLine("");
        if (!groupedPeople.TryGetValue(groupNumber, out string[]? people) || people == null)
        {
            return txtContent;
        }
        foreach (var person in people)
        {
            txtContent.AppendLine(person);
        }
        return txtContent;
    }

    private StringBuilder AppendCsvPeople(StringBuilder csvContent, Dictionary<int, string[]> groupedPeople)
    {
        var maxPeople = groupedPeople.Max(kv => kv.Value.Length);
        for (var i = 0; i < maxPeople; i++)
        {
            csvContent = csvContent.AppendCsvLine(GroupEnumerable.Select((_, index) => GetEntryOrDefault(groupedPeople, index, i)));
        }

        csvContent.AppendCsvLine(GroupEnumerable.Select(_ => ""));
        return csvContent;
    }

    private IntExpr[] GetZ3Variables(string[] people)
    {
        return people.Select(this.MkIntConst).ToArray();
    }

    private static Dictionary<int, string[]> GetGrouped(Model model, IntExpr[] expr)
    {
        return expr.Select(e => GetEvaluated(model, e)).GroupBy(r => r.Group).ToDictionary(g => g.Key, g => g.Select(r => r.Name).ToArray());
    }

    private static (int Group, string Name) GetEvaluated(Model model, IntExpr expr)
    {
        return (((IntNum)model.Eval(expr)).Int, expr.ToString());
    }

    private static string GetEntryOrDefault(Dictionary<int, string[]> groupsMembers, int group, int peopleIndex, string defaultReturn = "")
    {
        if (!groupsMembers.TryGetValue(group, out string[]? groupMembers) || groupMembers == null)
        {
            return defaultReturn;
        }
        if (groupMembers.Length <= peopleIndex)
        {
            return defaultReturn;
        }
        return groupMembers[peopleIndex];
    }
}
