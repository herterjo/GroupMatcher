using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GroupMatcher;
public class Test
{
    public static Config GetExampleConfig()
    {
        return new Config()
        {
            Associations = new Association[]
            {
                new Association()
                {
                    People = new string[]{"Adam","Eva"},
                    Weight = null
                },
                new Association()
                {
                    People = new string[]{"Kain","Abel"},
                    Weight = -100
                },
                new Association()
                {
                    People = new string[]{"David","Bathseba"},
                    Weight = 100
                },
                new Association()
                {
                    People = new string[]{"David","Saul","Salomo"},
                    Weight = 1
                }
            },
            FemaleLeaders = new string[] { "Esther", "Sarah" },
            MaleLeaders = new string[] { "David", "Saul", "Salomo" },
            FemaleMembers = new string[] { "Eva", "Bathseba" },
            MaleMembers = new string[] { "Adam", "Kain", "Abel" },
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
