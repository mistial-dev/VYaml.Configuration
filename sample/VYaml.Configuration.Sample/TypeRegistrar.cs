// <copyright file="TypeRegistrar.cs" company="Mistial Developer">
// Copyright (c) 2025 Mistial Developer. All rights reserved.
// Licensed under the MIT License. See docs/LICENSE for details.
// </copyright>

namespace VYaml.Configuration.Sample;

using System;
using Microsoft.Extensions.DependencyInjection;
using Spectre.Console.Cli;

/// <summary>
/// Type registrar for Spectre.Console.Cli to integrate with Microsoft DI.
/// </summary>
public sealed class TypeRegistrar : ITypeRegistrar
{
    private readonly IServiceCollection _services;

    /// <summary>
    /// Initializes a new instance of the <see cref="TypeRegistrar"/> class.
    /// </summary>
    /// <param name="services">The service collection.</param>
    public TypeRegistrar(IServiceCollection services)
    {
        _services = services;
    }

    /// <inheritdoc/>
    public ITypeResolver Build()
    {
        return new TypeResolver(_services.BuildServiceProvider());
    }

    /// <inheritdoc/>
    public void Register(Type service, Type implementation)
    {
        _services.AddSingleton(service, implementation);
    }

    /// <inheritdoc/>
    public void RegisterInstance(Type service, object implementation)
    {
        _services.AddSingleton(service, implementation);
    }

    /// <inheritdoc/>
    public void RegisterLazy(Type service, Func<object> factory)
    {
        _services.AddSingleton(service, provider => factory());
    }
}
