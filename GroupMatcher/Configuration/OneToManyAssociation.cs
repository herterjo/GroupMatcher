using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GroupMatcher.Configuration;
public class OneToManyAssociation : BaseAssociation
{
    public OneToManyAssociation()
    {
        ToPersons = Array.Empty<string>();
    }

    public string? FromPerson { get; set; }
    public string[] ToPersons { get; set; }
}
