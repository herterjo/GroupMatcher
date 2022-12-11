using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace GroupMatcher.Configuration;
public class BaseInput
{
    public BaseInput()
    {
        FemaleMembers = Array.Empty<string>();
        MaleMembers = Array.Empty<string>();
        FemaleLeaders = Array.Empty<string>();
        MaleLeaders = Array.Empty<string>();
        ManyAssociations = Array.Empty<ManyAssociation>();
    }

    public uint? MinGroupMembers { get; set; }
    public uint? MaxGroupMembers { get; set; }

    public uint? MinGroupLeaders { get; set; }
    public uint? MaxGroupLeaders { get; set; }

    public uint? MinFemaleGroupMembers { get; set; }
    public uint? MaxFemaleGroupMembers { get; set; }
    public uint? MinMaleGroupMembers { get; set; }
    public uint? MaxMaleGroupMembers { get; set; }

    public uint? MinFemaleGroupLeaders { get; set; }
    public uint? MaxFemaleGroupLeaders { get; set; }
    public uint? MinMaleGroupLeaders { get; set; }
    public uint? MaxMaleGroupLeaders { get; set; }

    public string[] FemaleMembers { get; set; }
    public string[] MaleMembers { get; set; }

    public string[] FemaleLeaders { get; set; }
    public string[] MaleLeaders { get; set; }

    public uint GroupCount { get; set; }
    public ManyAssociation[] ManyAssociations { get; set; }
}
