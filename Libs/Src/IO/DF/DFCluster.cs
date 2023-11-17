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
using Microsoft.Extensions.Logging;
using Nox.IO.Buffer;
using Nox.Security;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Security.Cryptography;
using System.Text;
using M = System.Math;

namespace Nox.IO.DF;

public abstract class DFCluster
    : DFBase, IDFContainer
{
    private uint _Signature;

    private int _Cluster;
    
    // [ .. DATA .. ]

    private uint _CRC;

    #region Properties
    public int Cluster { get { return _Cluster; } }
    
    public uint CRC => _CRC;

    public virtual bool Dirty { get; protected set; }
    #endregion

    public virtual void Read()
    {
        try
        {
            DF.FileHandle.Position = (DF.Header.ClusterSize * Cluster) + DF.Header.FirstClusterOffset();

            BinaryReader Reader = new BinaryReader(DF.FileHandle);
            if ((_Signature = Reader.ReadUInt32()) != DefaultSignature)
                throw new InvalidDataException("signature mismatch");

            _Cluster = Reader.Read();

            var CryptoStream = new CryptoStream(DF.FileHandle, DF.Laverna.CreateDecryptorTransformObject(), CryptoStreamMode.Read);
            BinaryReader CSReader = new BinaryReader(CryptoStream);

            ReadUserData(CSReader);

            _CRC = Reader.ReadUInt32();
            _CRC = Reader.ReadUInt32();
            if (ReCRC() != _CRC)
                throw new InvalidDataException("CRC mismatch");

            Dirty = false;
        }
        catch (Exception ex)
        {
            DF.Log?.LogError($"{ex}");

            throw;
        }
    }

    /// <summary>
    /// Kann überladen werden, um benutzerdefinierte Daten zu lesen
    /// </summary>
    /// <param name="Reader">Der BinaryReader von dem gelesen werden soll</param>
    public abstract void ReadUserData(BinaryReader Reader);

    /// <summary>
    /// Schreibt die Daten in die Datei
    /// </summary>
    public virtual void Write()
    {
        if (Dirty)
        {
            try
            {
                DF.FileHandle.Position = (DF.Header.ClusterSize * Cluster) + DF.Header.FirstClusterOffset();

                // unencrypted writer
                BinaryWriter Writer = new BinaryWriter(DF.FileHandle);
                Writer.Write(_Signature);

                // cryptowriter 
                var CryptoStream = new CryptoStream(DF.FileHandle, DF.Laverna.CreateEncryptorTransformObject(), CryptoStreamMode.Write);
                BinaryWriter CSWriter = new BinaryWriter(CryptoStream);

                WriteUserData(CSWriter);

                CSWriter.Flush();

                // CryptoStream gefüllt, Puffer leeren
                CryptoStream.Flush();

                // und abschliessen
                CryptoStream.FlushFinalBlock();

                // write crc of unencrypted data plain 
                _CRC = ReCRC();
                Writer.Write(_CRC);

                Writer.Flush();

                Dirty = false;
            }
            catch (DFException)
            {
                // pass through
                throw;
            }
            catch (IOException IOe)
            {
                throw new DFException(IOe.Message);
            }
            catch (Exception e)
            {
                throw new DFException(e.Message);
            }
        }
    }

    /// <summary>
    /// Kann überladen werden, um benutzerdefinierte Daten zu schreiben
    /// </summary>
    /// <param name="Writer">Der BinaryWriter in den geschrieben werden soll</param>
    public abstract void WriteUserData(BinaryWriter Writer);

    public int UserDataSize()
        => DF.Header.ClusterSize - sizeof(int) - (sizeof(uint) << 1);

    public int ContainerSize()
        => DF.Header.ClusterSize;

    public abstract void UserDataCRC(tinyCRC CRC);

    public uint ReCRC()
    {
        var CRC = new tinyCRC();
        UserDataCRC(CRC);

        return CRC.CRC32;
    }

    public DFCluster(IDF DF, int Cluster)
        : base(DF)
    {
        PropertyChanged += (s, e) 
            => Dirty = true;

        _Cluster = Cluster;
        Dirty = true;
    }
}