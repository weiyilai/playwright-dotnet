/*
 * MIT License
 *
 * Copyright (c) Microsoft Corporation.
 *
 * Permission is hereby granted, free of charge, to any person obtaining a copy
 * of this software and associated documentation files (the "Software"), to deal
 * in the Software without restriction, including without limitation the rights
 * to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
 * copies of the Software, and to permit persons to whom the Software is
 * furnished to do so, subject to the following conditions:
 *
 * The above copyright notice and this permission notice shall be included in all
 * copies or substantial portions of the Software.
 *
 * THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
 * IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
 * FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
 * AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
 * LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
 * OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
 * SOFTWARE.
 */

using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Microsoft.Playwright.Helpers;
using Microsoft.Playwright.Transport;
using Microsoft.Playwright.Transport.Protocol;

[assembly: InternalsVisibleTo("Microsoft.Playwright.MSTest, PublicKey=0024000004800000940000000602000000240000525341310004000001000100059a04ca5ca77c9b4eb2addd1afe3f8464b20ee6aefe73b8c23c0e6ca278d1a378b33382e7e18d4aa8300dd22d81f146e528d88368f73a288e5b8157da9710fe6f9fa9911fb786193f983408c5ebae0b1ba5d1d00111af2816f5db55871db03d7536f4a7a6c5152d630c1e1886b1a0fb68ba5e7f64a7f24ac372090889be2ffb")]
[assembly: InternalsVisibleTo("Microsoft.Playwright.NUnit, PublicKey=0024000004800000940000000602000000240000525341310004000001000100059a04ca5ca77c9b4eb2addd1afe3f8464b20ee6aefe73b8c23c0e6ca278d1a378b33382e7e18d4aa8300dd22d81f146e528d88368f73a288e5b8157da9710fe6f9fa9911fb786193f983408c5ebae0b1ba5d1d00111af2816f5db55871db03d7536f4a7a6c5152d630c1e1886b1a0fb68ba5e7f64a7f24ac372090889be2ffb")]
[assembly: InternalsVisibleTo("Microsoft.Playwright.Xunit, PublicKey=0024000004800000940000000602000000240000525341310004000001000100059a04ca5ca77c9b4eb2addd1afe3f8464b20ee6aefe73b8c23c0e6ca278d1a378b33382e7e18d4aa8300dd22d81f146e528d88368f73a288e5b8157da9710fe6f9fa9911fb786193f983408c5ebae0b1ba5d1d00111af2816f5db55871db03d7536f4a7a6c5152d630c1e1886b1a0fb68ba5e7f64a7f24ac372090889be2ffb")]

namespace Microsoft.Playwright.Core;

internal abstract class AssertionsBase
{
    private static float _defaultTimeout = 5_000;

    public AssertionsBase(bool isNot)
    {
        IsNot = isNot;
    }

    protected bool IsNot { get; }

    protected abstract Task<FrameExpectResult> CallExpectAsync(string expression, FrameExpectOptions expectOptions, string title);

    protected async Task ExpectImplAsync(string expression, ExpectedTextValue textValue, object expected, string message, string title, FrameExpectOptions options)
    {
        await ExpectImplAsync(expression, new ExpectedTextValue[] { textValue }, expected, message, title, options).ConfigureAwait(false);
    }

    protected async Task ExpectImplAsync(string expression, ExpectedTextValue[]? expectedText, object? expected, string message, string title, FrameExpectOptions options)
    {
        options ??= new();
        options.ExpectedText = expectedText;
        options.IsNot = IsNot;
        await ExpectImplAsync(expression, options, expected, message, title).ConfigureAwait(false);
    }

    protected async Task ExpectImplAsync(string expression, FrameExpectOptions expectOptions, object? expected, string message, string title)
    {
        if (expectOptions.Timeout == null)
        {
            expectOptions.Timeout = _defaultTimeout;
        }
        if (expectOptions.IsNot)
        {
            message = message.Replace("expected to", "expected not to");
        }
        var result = await CallExpectAsync(expression, expectOptions, title).ConfigureAwait(false);
        if (result.Matches == IsNot)
        {
            var actual = result.Received;
            var log = Connection.FormatCallLog(result.Log);
            if (expected == null)
            {
                throw new PlaywrightException($"{message} {log}");
            }
            throw new PlaywrightException($"{message} '{FormatValue(expected)}'\nBut was: '{FormatValue(actual)}' {log}");
        }
    }

    protected ExpectedTextValue ExpectedRegex(Regex pattern, ExpectedTextValue? options = null)
    {
        if (pattern == null)
        {
            throw new ArgumentNullException(nameof(pattern));
        }

        ExpectedTextValue textValue = options ?? new() { };
        textValue.RegexSource = pattern.ToString();
        textValue.RegexFlags = pattern.Options.GetInlineFlags();
        return textValue;
    }

    protected FrameExpectOptions ConvertToFrameExpectOptions(object? source) => ClassUtils.Clone<FrameExpectOptions>(source);

    private string FormatValue(object value)
    {
        if (value == null)
        {
            return "null";
        }

        if (value is string)
        {
            return (string)value;
        }

        if (value is IEnumerable<object>)
        {
            return "[" + string.Join(", ", ((IEnumerable<object>)value).Select(value => $"'{value}'")) + "]";
        }

        return value.ToString();
    }

    public static void SetDefaultTimeout(float timeout)
        => _defaultTimeout = timeout;
}
