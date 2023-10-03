using System;
using System.Collections.Generic;
using System.Formats.Asn1;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace GroupMatcher.Configuration;
public class Input : BaseInput
{
    public Input()
    {
        OneToManyAssociations = Array.Empty<OneToManyAssociation>();
    }

    public OneToManyAssociation[] OneToManyAssociations { get; set; }
    public int MultipleAssociationsPenalty { get; set; }
    public uint? GroupMembersVariance { get; set; }
    public uint? GroupLeadersVariance { get; set; }
    public uint? FemaleGroupMembersVariance { get; set; }
    public uint? MaleGroupMembersVariance { get; set; }
    public uint? FemaleGroupLeadersVariance { get; set; }
    public uint? MaleGroupLeadersVariance { get; set; }

    public BaseInput ToBaseInput()
    {
        //Use serialization for easy copying as no self referncing loops are possible
        var serialized = JsonConvert.SerializeObject(this);
        var baseInput = JsonConvert.DeserializeObject<BaseInput>(serialized);
        var newAssociations = this.OneToManyAssociations
            .Where(a => a.FromPerson != null && a.ToPersons != null && a.ToPersons.Any())
            .SelectMany(a => a.ToPersons
                .Select((p, index) => new ManyAssociation()
                {
                    Weight = GetWeight(a.Weight, index),
                    People = new string[] { a.FromPerson, p }
                }));
        baseInput.ManyAssociations = baseInput.ManyAssociations.Concat(newAssociations).ToArray();

        if (GroupMembersVariance.HasValue && !baseInput.MinGroupMembers.HasValue)
        {
            var allRelevantPeopleCount = (uint)(baseInput.FemaleMembers.Length + baseInput.MaleMembers.Length);
            baseInput.MinGroupMembers = GetGroupCountLimit(allRelevantPeopleCount, baseInput.GroupCount, this.GroupMembersVariance.Value, false);
        }
        if (GroupMembersVariance.HasValue && !baseInput.MaxGroupMembers.HasValue)
        {
            var allRelevantPeopleCount = (uint)(baseInput.FemaleMembers.Length + baseInput.MaleMembers.Length);
            baseInput.MaxGroupMembers = GetGroupCountLimit(allRelevantPeopleCount, baseInput.GroupCount, this.GroupMembersVariance.Value, true);
        }

        if (GroupLeadersVariance.HasValue && !baseInput.MinGroupLeaders.HasValue)
        {
            var allRelevantPeopleCount = (uint)(baseInput.FemaleLeaders.Length + baseInput.MaleLeaders.Length);
            baseInput.MinGroupLeaders = GetGroupCountLimit(allRelevantPeopleCount, baseInput.GroupCount, this.GroupLeadersVariance.Value, false);
        }
        if (GroupLeadersVariance.HasValue && !baseInput.MaxGroupLeaders.HasValue)
        {
            var allRelevantPeopleCount = (uint)(baseInput.FemaleLeaders.Length + baseInput.MaleLeaders.Length);
            baseInput.MaxGroupLeaders = GetGroupCountLimit(allRelevantPeopleCount, baseInput.GroupCount, this.GroupLeadersVariance.Value, true);
        }

        if (FemaleGroupMembersVariance.HasValue && !baseInput.MinFemaleGroupMembers.HasValue)
        {
            var allRelevantPeopleCount = (uint)baseInput.FemaleMembers.Length;
            baseInput.MinFemaleGroupMembers = GetGroupCountLimit(allRelevantPeopleCount, baseInput.GroupCount, this.FemaleGroupMembersVariance.Value, false);
        }
        if (FemaleGroupMembersVariance.HasValue && !baseInput.MaxFemaleGroupMembers.HasValue)
        {
            var allRelevantPeopleCount = (uint)baseInput.FemaleMembers.Length;
            baseInput.MaxFemaleGroupMembers = GetGroupCountLimit(allRelevantPeopleCount, baseInput.GroupCount, this.FemaleGroupMembersVariance.Value, true);
        }

        if (MaleGroupMembersVariance.HasValue && !baseInput.MinMaleGroupMembers.HasValue)
        {
            var allRelevantPeopleCount = (uint)baseInput.MaleMembers.Length;
            baseInput.MinMaleGroupMembers = GetGroupCountLimit(allRelevantPeopleCount, baseInput.GroupCount, this.MaleGroupMembersVariance.Value, false);
        }
        if (MaleGroupMembersVariance.HasValue && !baseInput.MaxMaleGroupMembers.HasValue)
        {
            var allRelevantPeopleCount = (uint)baseInput.MaleMembers.Length;
            baseInput.MaxMaleGroupMembers = GetGroupCountLimit(allRelevantPeopleCount, baseInput.GroupCount, this.MaleGroupMembersVariance.Value, true);
        }

        if (FemaleGroupLeadersVariance.HasValue && !baseInput.MinFemaleGroupLeaders.HasValue)
        {
            var allRelevantPeopleCount = (uint)baseInput.FemaleLeaders.Length;
            baseInput.MinFemaleGroupLeaders = GetGroupCountLimit(allRelevantPeopleCount, baseInput.GroupCount, this.FemaleGroupLeadersVariance.Value, false);
        }
        if (FemaleGroupLeadersVariance.HasValue && !baseInput.MaxFemaleGroupLeaders.HasValue)
        {
            var allRelevantPeopleCount = (uint)baseInput.FemaleLeaders.Length;
            baseInput.MaxFemaleGroupLeaders = GetGroupCountLimit(allRelevantPeopleCount, baseInput.GroupCount, this.FemaleGroupLeadersVariance.Value, true);
        }

        if (MaleGroupLeadersVariance.HasValue && !baseInput.MinMaleGroupLeaders.HasValue)
        {
            var allRelevantPeopleCount = (uint)baseInput.MaleLeaders.Length;
            baseInput.MinMaleGroupLeaders = GetGroupCountLimit(allRelevantPeopleCount, baseInput.GroupCount, this.MaleGroupLeadersVariance.Value, false);
        }
        if (MaleGroupLeadersVariance.HasValue && !baseInput.MaxMaleGroupLeaders.HasValue)
        {
            var allRelevantPeopleCount = (uint)baseInput.MaleLeaders.Length;
            baseInput.MaxMaleGroupLeaders = GetGroupCountLimit(allRelevantPeopleCount, baseInput.GroupCount, this.MaleGroupLeadersVariance.Value, true);
        }

        return baseInput;
    }

    private static uint? GetGroupCountLimit(uint allRelevantPeopleCount, uint groupCount, uint variance, bool isUpperBound)
    {
        var perGroupCount = allRelevantPeopleCount / groupCount;
        int? limit = (int)perGroupCount;
        if (isUpperBound)
        {
            limit += (int)variance;
        }
        else
        {
            limit -= (int)variance;
            if (limit < 0)
            {
                limit = null;
            }
        }
        return (uint?) limit;
    }

    public static Input ReadConfig(string filePath)
    {
        var contents = File.ReadAllText(filePath);
        return JsonConvert.DeserializeObject<Input>(contents) ?? throw new InvalidDataException("Could not parse json contents of " + filePath);
    }

    public int? GetWeight(int? baseWeight, int index)
    {
        if (!baseWeight.HasValue || MultipleAssociationsPenalty == 0 || index == 0 || (baseWeight < 1 && baseWeight > -1))
        {
            return baseWeight;
        }

        var negativeWeight = baseWeight.Value < 0;
        var correctPrefixPenalty = negativeWeight ? (-1) * this.MultipleAssociationsPenalty : this.MultipleAssociationsPenalty;
        var multipliedPenalty = index * correctPrefixPenalty;
        var negativePenalty = multipliedPenalty < 0;
        if (((negativeWeight && !negativePenalty) || (!negativeWeight && negativePenalty))
            && Math.Abs(baseWeight.Value) <= Math.Abs(multipliedPenalty))
        {
            return negativeWeight ? -1 : 1;
        }
        try
        {
            return checked(baseWeight.Value + multipliedPenalty);
        }
        catch (OverflowException)
        {
            return negativeWeight ? Int32.MinValue : Int32.MaxValue;
        }
    }
}
