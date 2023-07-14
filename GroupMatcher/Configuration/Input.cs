using System;
using System.Collections.Generic;
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
        return baseInput;
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
