// <copyright file="Program.cs" company="Norge360">
// Copyright (c) 2026 Norge360. All rights reserved.
// Norge360 is proprietary software. See the LICENSE file in the repository root.
// </copyright>

using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Running;

namespace Norge360.Auth.Benchmarks;

public static class BenchmarkProgram
{
    public static void Main(string[] args)
    {
        var config = DefaultConfig.Instance;
        BenchmarkSwitcher
            .FromAssembly(typeof(BenchmarkProgram).Assembly)
            .Run(args, config);
    }
}
