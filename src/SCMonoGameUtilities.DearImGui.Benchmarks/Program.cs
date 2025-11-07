using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Running;
using System.Reflection;

// See https://benchmarkdotnet.org/articles/guides/console-args.html (or run app with --help)
// Also see debug launch profiles for some specific command lines (obv run them in "Release"
// and without a debugger attached).
BenchmarkSwitcher.FromAssembly(Assembly.GetExecutingAssembly()).Run(args, GetGlobalConfig());

static IConfig GetGlobalConfig()
{
    return DefaultConfig.Instance
        .WithOptions(ConfigOptions.DisableOptimizationsValidator);
}