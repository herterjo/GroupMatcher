using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GroupMatcher.Configuration;
public class ManyAssociation : BaseAssociation
{
    public ManyAssociation()
    {
        People = Array.Empty<string>();
    }
    public string[] People { get; set; }
}
