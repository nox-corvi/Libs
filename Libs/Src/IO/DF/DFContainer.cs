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
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Text;
using M = System.Math;

namespace Nox.IO.DF;

public abstract class DFContainer
    : DFBase, IDFContainer, IDFCRCSupport, IDFCRCElement
{
    private uint _Signature;
    
    private byte[] _Name;
    
    private DateTime _Created;
    private DateTime _Modified;

    private uint _CRC;

    #region Properties
    public int Index { get; }

    public string Name
    {
        get => BytesToString(_Name);
        set => SetProperty(ref _Name, GetStringBytes(value, 32));
    }

    public DateTime Created
    {
        get => _Created;
        set => SetProperty(ref _Created, value);
    }

    public DateTime Modified
    {
        get => _Modified;
        set => SetProperty(ref _Modified, value);
    }

    public virtual bool Dirty { get; protected set; }

    public uint CRC { get => _CRC; }
    #endregion

    public virtual void Read()
    {
        try
        {
            DF.FileHandle.Position = DF.Header.ContainerOffset(Index);
            
            BinaryReader Reader = new BinaryReader(DF.FileHandle);
            if ((_Signature = Reader.ReadUInt32()) != DefaultSignature)
                throw new InvalidDataException("signature mismatch");


            var CryptoStream = new CryptoStream(DF.FileHandle, DF.Laverna.CreateDecryptorTransformObject(), CryptoStreamMode.Read);
            BinaryReader CSReader = new BinaryReader(CryptoStream);

            ReadUserData(CSReader);

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
                DF.FileHandle.Position = DF.Header.ContainerOffset(Index);

                // unencrypted writer
                BinaryWriter Writer = new BinaryWriter(DF.FileHandle);
                Writer.Write(_Signature);

                // cryptowriter 
                var CryptoStream = new CryptoStream(DF.FileHandle, DF.Laverna.CreateEncryptorTransformObject(), CryptoStreamMode.Write);
                BinaryWriter CSWriter = new BinaryWriter(CryptoStream);

                // userdata are always encrypted
                WriteUserData(CSWriter);

                // Leeren des Schreib-Puffers erzwingen
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


    #region Helpers
    public abstract void UserDataCRC(tinyCRC CRC);

    /// <summary>
    /// Berechnet die Checksumme für den Kopfsatz.
    /// </summary>
    /// <returns>der CRC32 des Kopfsatzes</returns>
    public uint ReCRC()
    {
        var CRC = new tinyCRC();
        CRC.Push(_Signature);
        CRC.Push(_Name);

        CRC.Push(_Created.ToFileTimeUtc());
        CRC.Push(_Modified.ToFileTimeUtc());

        UserDataCRC(CRC);

        return CRC.CRC32;
    }

    public abstract int UserDataSize();

    public int ContainerSize()
        => sizeof(uint) +   // _Signature
            32 +            // _Name
            sizeof(long) +  // _Created
            sizeof(long) +  // _Modified
        UserDataSize();
    #endregion


    public DFContainer(IDF DF, int Index)
        : this(DF)
    {
        this.Index = Index;
        Dirty = true;
    }

    public DFContainer(IDF DF)
        : base(DF)
    {
        PropertyChanged += (s, e) =>
        {
            if (e.PropertyName != nameof(Modified))
            {
                _Modified = DateTime.UtcNow;
            }

            Dirty = true;
        };

        _Signature = DefaultSignature;

        _Name = GetStringBytes(Guid.NewGuid().ToString(), 64);

        _Created = DateTime.UtcNow;
        _Modified = DateTime.UtcNow;

        _CRC = ReCRC();

        Dirty = true;
    }
}