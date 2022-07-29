using System.Text;
using Microsoft.Z3;
using Newtonsoft.Json;

namespace GroupMatcher;

internal class Program
{
    static void Main(string[] args)
    {


        var path = args.Length > 0 ? args[0] : "input.json";
        var config = Config.ReadConfig(path);

        if (config.GroupCount < 2)
        {
            throw new InvalidDataException("At least two groups must be filled");
        }

        Global.SetParameter("parallel.enable", "true");
        using (var solver = new Solver(config))
        {
            solver.Solve();
        }
    }
}
