using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using GroupMatcher.Configuration;

namespace GroupMatcher;
public class Test
{
    public static Input GetExampleConfig()
    {
        return new Input()
        {
            ManyAssociations = new ManyAssociation[]
            {
                new ManyAssociation()
                {
                    People = new string[] { "Adam", "Eva" },
                    Weight = null
                },
                new ManyAssociation()
                {
                    People = new string[] { "Kain", "Abel" },
                    Weight = -100
                },
                new ManyAssociation()
                {
                    People = new string[] { "David", "Saul", "Salomo" },
                    Weight = 1
                }
            },
            OneToManyAssociations = new OneToManyAssociation[]
            {
                new OneToManyAssociation()
                {
                    FromPerson = "David",
                    Weight = 100,
                    ToPersons = new string[] { "Bathseba" }
                }, new OneToManyAssociation()
                {
                    FromPerson = "Abraham",
                    Weight = 200,
                    ToPersons = new string[] { "Sarah", "Hagar" }
                }
            },
            FemaleLeaders = new string[] { "Esther", "Sarah", "Hagar" },
            MaleLeaders = new string[] { "David", "Saul", "Salomo", "Abraham" },
            FemaleMembers = new string[] { "Eva", "Bathseba",  },
            MaleMembers = new string[] { "Adam", "Kain", "Abel"},
            GroupCount = 2,
            MaxFemaleGroupLeaders = null,
            MaxFemaleGroupMembers = 4,
            MaxGroupLeaders = 2,
            MaxGroupMembers = null,
            MaxMaleGroupLeaders = 2,
            MaxMaleGroupMembers = null,
            MinFemaleGroupLeaders = 1,
            MinFemaleGroupMembers = 1,
            MinGroupLeaders = 1,
            MinGroupMembers = 1,
            MinMaleGroupLeaders = 1,
            MinMaleGroupMembers = 1
        };
    }
}
