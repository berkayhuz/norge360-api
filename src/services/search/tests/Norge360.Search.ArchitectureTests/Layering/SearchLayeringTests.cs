// <copyright file="SearchLayeringTests.cs" company="Norge360">
// Copyright (c) 2026 Norge360. All rights reserved.
// Norge360 is proprietary software. See the LICENSE file in the repository root.
// </copyright>

using FluentAssertions;
using NetArchTest.Rules;
using Norge360.Search.API.Endpoints;
using Norge360.Search.Application.DependencyInjection;
using Norge360.Search.Contracts;
using Norge360.Search.Domain;
using Norge360.Search.Infrastructure.DependencyInjection;
using Norge360.Search.Worker;

namespace Norge360.Search.ArchitectureTests.Layering;

public sealed class SearchLayeringTests
{
    [Fact]
    public void Domain_Should_Not_Depend_On_Application_Infrastructure_Or_Api()
    {
        var result = Types.InAssembly(typeof(SearchDomainMarker).Assembly)
            .ShouldNot()
            .HaveDependencyOnAny("Norge360.Search.Application", "Norge360.Search.Infrastructure", "Norge360.Search.API", "Norge360.Search.Worker")
            .GetResult();

        result.IsSuccessful.Should().BeTrue(string.Join(Environment.NewLine, result.FailingTypeNames ?? []));
    }

    [Fact]
    public void Application_Should_Not_Depend_On_Infrastructure_Or_Api()
    {
        var result = Types.InAssembly(typeof(SearchApplicationServiceCollectionExtensions).Assembly)
            .ShouldNot()
            .HaveDependencyOnAny("Norge360.Search.Infrastructure", "Norge360.Search.API", "Norge360.Search.Worker")
            .GetResult();

        result.IsSuccessful.Should().BeTrue(string.Join(Environment.NewLine, result.FailingTypeNames ?? []));
    }

    [Fact]
    public void Infrastructure_Should_Not_Depend_On_Api()
    {
        var result = Types.InAssembly(typeof(SearchInfrastructureServiceCollectionExtensions).Assembly)
            .ShouldNot()
            .HaveDependencyOnAny("Norge360.Search.API", "Norge360.Search.Worker")
            .GetResult();

        result.IsSuccessful.Should().BeTrue(string.Join(Environment.NewLine, result.FailingTypeNames ?? []));
    }

    [Fact]
    public void Contracts_Should_Not_Depend_On_Domain_Application_Infrastructure_Or_Api()
    {
        var result = Types.InAssembly(typeof(SearchContractsMarker).Assembly)
            .ShouldNot()
            .HaveDependencyOnAny("Norge360.Search.Domain", "Norge360.Search.Application", "Norge360.Search.Infrastructure", "Norge360.Search.API", "Norge360.Search.Worker")
            .GetResult();

        result.IsSuccessful.Should().BeTrue(string.Join(Environment.NewLine, result.FailingTypeNames ?? []));
    }

    [Fact]
    public void Worker_Should_Not_Depend_On_Api()
    {
        var result = Types.InAssembly(typeof(SearchWorkerMarker).Assembly)
            .ShouldNot()
            .HaveDependencyOn("Norge360.Search.API")
            .GetResult();

        result.IsSuccessful.Should().BeTrue(string.Join(Environment.NewLine, result.FailingTypeNames ?? []));
    }

    [Fact]
    public void Api_Should_Not_Depend_On_Worker()
    {
        var result = Types.InAssembly(typeof(SearchEndpoints).Assembly)
            .ShouldNot()
            .HaveDependencyOn("Norge360.Search.Worker")
            .GetResult();

        result.IsSuccessful.Should().BeTrue(string.Join(Environment.NewLine, result.FailingTypeNames ?? []));
    }
}
