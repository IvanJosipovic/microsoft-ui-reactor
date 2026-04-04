namespace PerfBench.Shared;

public sealed class BenchCliOptions
{
    public int DurationSeconds { get; set; } = 10;
    public bool Headless { get; set; }
    public string Optimization { get; set; } = "off"; // "on" or "off"

    // EXP-2/5: percent of cells to update per tick
    public double Percent { get; set; } = 50;

    public static BenchCliOptions Parse(string[] args)
    {
        var opts = new BenchCliOptions();
        for (int i = 0; i < args.Length; i++)
        {
            switch (args[i])
            {
                case "--duration" when i + 1 < args.Length:
                    opts.DurationSeconds = int.Parse(args[++i]);
                    break;
                case "--headless":
                    opts.Headless = true;
                    break;
                case "--optimization" when i + 1 < args.Length:
                    opts.Optimization = args[++i].ToLowerInvariant();
                    break;
                case "--percent" when i + 1 < args.Length:
                    opts.Percent = double.Parse(args[++i]);
                    break;
            }
        }
        return opts;
    }
}
