using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using GroupMatcher.Configuration;
using Microsoft.Z3;

namespace GroupMatcher;
public class Solver : Context
{
    private readonly BaseInput Config;
    private readonly IEnumerable<IntNum> GroupEnumerable;
    private readonly IntExpr[] FemaleMembers;
    private readonly IntExpr[] MaleMembers;
    private readonly IntExpr[] FemaleLeaders;
    private readonly IntExpr[] MaleLeaders;

    private readonly IntNum Zero;
    private readonly IntNum One;

    public Solver(BaseInput config) : base(new Dictionary<string, string>() { { "model", "true" } })
    {
        this.Config = config ?? throw new ArgumentNullException(nameof(config));
        this.GroupEnumerable = Enumerable.Repeat(0, (int)Config.GroupCount).Select((_, index) => this.MkInt(index)).ToList();

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

        var strongAsserts = GetStrongAsserts();
        var associationAsserts = GetAssociationExpressions();

        using (var solver = this.MkOptimize())
        {
            solver.Assert(strongAsserts);

            foreach (var (assert, weight, groupName) in associationAsserts)
            {
                if (weight.HasValue)
                {
                    solver.AssertSoft(assert, weight.Value, groupName);
                }
                else
                {
                    solver.Assert(assert);
                }
            }

            var status = solver.Check();

            var z3FilePath = Path.GetFullPath($"output_{now}.z3");
            File.WriteAllText(z3FilePath, solver.ToString());
            Console.WriteLine("Generated Z3 code written to " + z3FilePath);

            if (status == Status.SATISFIABLE)
            {
                Print(now, solver);
                return;
            }
        }

        Console.WriteLine("Could not evaluate with these strong asserts");

        var allHardAsserts = associationAsserts.Where(a => !a.Weight.HasValue).Select(a => a.Assert).Concat(strongAsserts).ToList();
        AssertAllSoft(allHardAsserts, now);
        throw new ArgumentException("Could not evaluate with these strong asserts");
    }

    private void AssertAllSoft(IEnumerable<BoolExpr> exprs, string now)
    {
        using (var solver = this.MkOptimize())
        {
            foreach (var expr in exprs)
            {
                solver.AssertSoft(expr, 1, expr.ToString() + "______" + Random.Shared.Next());
            }

            var status = solver.Check();
            if (status != Status.SATISFIABLE)
            {
                var solverStringPath = Path.GetFullPath($"unexpectedSolverOutput_{now}.z3");
                File.WriteAllText(solverStringPath, solver.ToString());
                throw new InvalidProgramException($"Solver with only soft asserts returned {status}. " +
                    "Something is very wrong. " +
                    "Generated solver code is written to " + solverStringPath);
            }

            var notMetPath = Path.GetFullPath($"notMetConditions_{now}.txt");
            Console.WriteLine($"Following conditions could not be met during model generation (also written to {notMetPath}):");
            var notMetContent = new StringBuilder();
            using (var model = solver.Model)
            {
                foreach (var expr in exprs)
                {
                    var evalValue = model.Eval(expr).BoolValue;
                    if (evalValue == Z3_lbool.Z3_L_TRUE)
                    {
                        continue;
                    }
                    notMetContent.AppendLine();
                    notMetContent.AppendLine(evalValue.ToString());
                    notMetContent.AppendLine(expr.ToString());
                }
            }

            notMetContent.AppendLine();
            var notMetStringContent = notMetContent.ToString();
            Console.WriteLine(notMetStringContent);
            File.WriteAllText(notMetPath, notMetStringContent);
        }
    }

    private void Print(string now, Optimize solver)
    {
        using (var model = solver.Model)
        {
            var evaluatedFemaleMembers = GetGrouped(model, FemaleMembers);
            var evaluatedMaleMembers = GetGrouped(model, MaleMembers);
            var evaluatedFemaleLeaders = GetGrouped(model, FemaleLeaders);
            var evaluatedMaleLeaders = GetGrouped(model, MaleLeaders);

            var csvContent = new StringBuilder("");

            csvContent = csvContent.AppendCsvLine(GroupEnumerable.Select((_, index) => index + 1));

            csvContent = AppendCsvPeople(csvContent, evaluatedFemaleLeaders);
            csvContent = AppendCsvPeople(csvContent, evaluatedMaleLeaders);
            csvContent = AppendCsvPeople(csvContent, evaluatedFemaleMembers);
            csvContent = AppendCsvPeople(csvContent, evaluatedMaleMembers);

            var csvPath = Path.GetFullPath($"output_{now}.csv");
            File.WriteAllText(csvPath, csvContent.ToString());
            Console.WriteLine("Csv solution written to " + csvPath);

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

            var txtPath = Path.GetFullPath($"output_{now}.txt");
            var txtStringContent = txtContent.ToString();
            File.WriteAllText(txtPath, txtStringContent);
            Console.WriteLine("Txt solution written to " + txtPath);
            Console.WriteLine();
            Console.WriteLine(txtStringContent);
        }
    }

    private IEnumerable<BoolExpr> GetStrongAsserts()
    {
        var expressions = new List<BoolExpr>();
        expressions.AddRange(AllInSomeGroup(FemaleMembers));
        expressions.AddRange(AllInSomeGroup(MaleMembers));
        expressions.AddRange(AllInSomeGroup(FemaleLeaders));
        expressions.AddRange(AllInSomeGroup(MaleLeaders));

        expressions.AddRange(GetMinMaxExpr(FemaleMembers, Config.MinFemaleGroupMembers, Config.MaxFemaleGroupMembers));
        expressions.AddRange(GetMinMaxExpr(MaleMembers, Config.MinMaleGroupMembers, Config.MaxMaleGroupMembers));
        expressions.AddRange(GetMinMaxExpr(FemaleLeaders, Config.MinFemaleGroupLeaders, Config.MaxFemaleGroupLeaders));
        expressions.AddRange(GetMinMaxExpr(MaleLeaders, Config.MinMaleGroupLeaders, Config.MaxMaleGroupLeaders));

        expressions.AddRange(GetMinMaxExpr(FemaleLeaders.Concat(MaleLeaders), Config.MinGroupLeaders, Config.MaxGroupLeaders));
        expressions.AddRange(GetMinMaxExpr(FemaleMembers.Concat(MaleMembers), Config.MinGroupMembers, Config.MaxGroupMembers));
        return expressions;
    }

    private IEnumerable<(BoolExpr Assert, uint? Weight, string Group)> GetAssociationExpressions()
    {
        if (Config.ManyAssociations == null || !Config.ManyAssociations.Any())
        {
            return Enumerable.Empty<(BoolExpr Assert, uint? Weight, string Group)>();
        }

        var allPeopleVars = FemaleMembers.Concat(MaleMembers).Concat(FemaleLeaders).Concat(MaleLeaders).ToDictionary(p => p.ToReadableString(), p => p);

        var expressions = new LinkedList<(BoolExpr Assert, uint? Weight, string Group)>();
        foreach (var association in Config.ManyAssociations)
        {
            if (association.People == null || !association.People.Any())
            {
                continue;
            }
            var peopleVars = new List<IntExpr>(association.People.Length);
            foreach (var person in association.People.Distinct())
            {
                if (allPeopleVars.TryGetValue(person, out var personVar))
                {
                    peopleVars.Add(personVar);
                } 
                else
                {
                    throw new ArgumentException("Could not get matching person from people list: " + person);
                }
            }
            if (peopleVars.Count < 2)
            {
                continue;
            }
            var prev = peopleVars.First();
            var allInSameGroup = this.MkTrue();
            foreach (var curr in peopleVars.Skip(1))
            {
                allInSameGroup &= this.MkEq(prev, curr);
                prev = curr;
            }

            uint? weight;
            if (association.Weight.HasValue && association.Weight < 0)
            {
                allInSameGroup = this.MkNot(allInSameGroup);
                weight = (uint)-association.Weight;
            }
            else
            {
                weight = (uint?)association.Weight;
            }

            var groupName = String.Join("_AND_", peopleVars.Select(p => p.ToString()));
            expressions.AddFirst((allInSameGroup, weight, groupName));
        }
        return expressions;
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

    private IEnumerable<BoolExpr> AllInSomeGroup(IEnumerable<IntExpr> people)
    {
        var allInSomeGroup = new LinkedList<BoolExpr>();
        foreach (var person in people)
        {
            var personInSomeGroup = this.MkFalse();
            foreach (var group in GroupEnumerable)
            {
                personInSomeGroup |= this.MkEq(group, person);
            }
            allInSomeGroup.AddFirst(personInSomeGroup);
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
        return people.Distinct().Select(this.MkIntConst).ToArray();
    }

    private static Dictionary<int, string[]> GetGrouped(Model model, IntExpr[] expr)
    {
        return expr.Select(e => GetEvaluated(model, e))
            .GroupBy(r => r.Group)
            .ToDictionary(
                g => g.Key, 
                g => g.Select(r => r.Name).ToArray());
    }

    private static (int Group, string Name) GetEvaluated(Model model, IntExpr expr)
    {
        return (((IntNum)model.Eval(expr)).Int, expr.ToReadableString());
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
