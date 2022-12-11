using System.Text;
using GroupMatcher.Configuration;
using Microsoft.Z3;
using Newtonsoft.Json;

namespace GroupMatcher;

internal class Program
{
    static void Main(string[] args)
    {
        var path = args.Length > 0 ? args[0] : "input.json";
        var input = Input.ReadConfig(path);
        var baseInput = input.ToBaseInput();

        if (baseInput.GroupCount < 2)
        {
            throw new InvalidDataException("At least two groups must be filled");
        }

        Global.SetParameter("parallel.enable", "true");
        using (var solver = new Solver(baseInput))
        {
            solver.Solve();
        }
    }
}
