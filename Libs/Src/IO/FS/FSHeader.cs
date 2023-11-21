/*
 * Copyright (c) 2014-20203 Anrá aka Nox
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

using Nox.IO.Buffer;
using Nox.IO.DF;
using Nox.Security;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace Nox.IO.FS;

public class FSHeader
    : DFContainer
{
    // Konstanten
    private const uint CURRENT_REVISION = 0x10A0;
    private const ushort NODE_SIZE = 128;

    // Variablen
    private uint _Revision = CURRENT_REVISION;
    private int _NodesPerBlock = -1;

    #region Properties
    public uint Revision
    {
        get => _Revision;
    }

    /// <summary>
    /// Liefert die Größe eines Knoten in Bytes zurück.
    /// </summary>
    public int NodeSize
    {
        get => NODE_SIZE;
    }

    /// <summary>
    /// Liefert die maximal für Nutzdaten verwendbare Größe zurück.
    /// </summary>
    public int UseableClusterSize
    {
        get => GuardianGet.ClusterSize - 16;
    }

    /// <summary>
    /// Liefert die maximale Anzahl an Knoten zurück, welche in einem NodeBlock gespeichert werden können.
    /// </summary>
    public int NodesPerBlock
    {
        get
        {
            if (_NodesPerBlock == -1)
            {
                _NodesPerBlock = 32;
                while (_NodesPerBlock * NodeSize < UseableClusterSize)
                    _NodesPerBlock++;

                while (_NodesPerBlock * NodeSize > UseableClusterSize)
                    _NodesPerBlock--;
            }

            return _NodesPerBlock;
        }
    }

    /// <summary>
    /// Liefert die Anzahl an möglichen Positionen pro Cluster zurück.
    /// </summary>
    public int PositionsPerCluster
    {
        get => GuardianGet.ClusterSize - 12;
    }
    #endregion

    public FSHeader(IDFGuardian guardian)
        : base(guardian)
    {
    }

    public override void UserDataCRC(tinyCRC CRC)
    {

    }
}
