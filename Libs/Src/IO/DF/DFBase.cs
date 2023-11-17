/*
 * Copyright (c) 2014-2018 Anrá aka Nox
 * 
 * This code is licensed under the MIT license (MIT) 
 * 
 * Permission is hereby granted, free of charge, to any person obtaining a copy 
 * of this software and associated documentation files (the "Software"), to deal 
 * in the Software without restriction, including without limitation the rights 
 * to use, copy, modify, merge, publish, distribute, sublicense, and/or sell 
 * copies of the Software, and to permit persons to whom the Software is 
 * furnished to do so, subject to the following conditions:
 * 
 * The above copyright notice and this permission notice shall be included 
 * in all copies or substantial portions of the Software.
 * 
 * THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR 
 * IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY, 
 * FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE 
 * AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER 
 * LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, 
 * OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN 
 * THE SOFTWARE.
 * 
*/
using Nox.Component;
using Nox.IO.Buffer;
using Nox.Security;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Security.Cryptography;
using System.Text;
using M = System.Math;

namespace Nox.IO.DF;

public abstract class DFBase
    : ObservableObject, IDFItemBase
{
    private const uint HASH_MASTER = 0x1494BFDA;
    private readonly uint _DefaultSignature;

    #region Properties
    /// <summary>
    /// reference to the "great" object
    /// </summary>
    public IDF DF { get; } = null!;

    public uint DefaultSignature { get => _DefaultSignature; }
    #endregion

    #region Helpers
    public static byte[] GetStringBytes(string Value, int Length = -1)
    {
        if (Length == -1)
            return Encoding.ASCII.GetBytes(Value);
        else
            if (Value.Length > Length)
            return Encoding.ASCII.GetBytes(Value.Substring(0, Length));
        else
            return Encoding.ASCII.GetBytes(Value.PadRight(Length));
    }
    public static string BytesToString(byte[] Raw)
        => Encoding.ASCII.GetString(Raw).TrimEnd();

    public static uint HashSignature()
        => (uint)HASH_MASTER ^ Hash.HashFNV1a32(MethodBase.GetCurrentMethod().DeclaringType.FullName);
    #endregion

    public DFBase(IDF DF)
    {
        this.DF = DF;
        this._DefaultSignature = HashSignature();
    }
}
