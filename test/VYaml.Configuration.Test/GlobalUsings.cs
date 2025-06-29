// <copyright file="GlobalUsings.cs" company="Mistial Developer">
// Copyright (c) 2025 Mistial Developer. All rights reserved.
// Licensed under the MIT License. See docs/LICENSE for details.
// </copyright>

#if NETSTANDARD2_0
global using Assert = NUnit.Framework.Assert;
#else
global using Assert = NUnit.Framework.Legacy.ClassicAssert;
#endif
