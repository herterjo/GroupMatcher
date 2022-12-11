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

    public BaseInput ToBaseInput()
    {
        //Use serialization for easy copying as no self referncing loops are possible
        var serialized = JsonConvert.SerializeObject(this);
        var baseInput = JsonConvert.DeserializeObject<BaseInput>(serialized);
        var newAssociations = this.OneToManyAssociations
            .Where(a => a.FromPerson != null && a.ToPersons != null && a.ToPersons.Any())
            .SelectMany(a => a.ToPersons
                .Select(p => new ManyAssociation()
                {
                    Weight = a.Weight,
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
}
