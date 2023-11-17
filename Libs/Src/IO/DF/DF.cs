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
using Nox.Hosting;
using Nox.Security;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Security.Cryptography;
using System.Text;
using M = System.Math;
using System.Xml.Linq;
using System.Runtime.InteropServices;

namespace Nox.IO.DF;

public abstract class DF<T>
    where T : DFHeader
{
    // Variablen
    private byte[] _DF_KEY = {
            0x72, 0x7A, 0x62, 0x45, 0x66, 0x5A, 0x55, 0x31,
            0x59, 0x63, 0x32, 0x37, 0x61, 0x44, 0x73, 0x37,
            0x51, 0x75, 0x62, 0x4C, 0x64, 0xA7, 0x71, 0x6F,
            0x67, 0x41, 0x75, 0x43, 0x55, 0x31, 0x75, 0x4B };
    private byte[] _DF_IV = {
            0x5F, 0x6E, 0x7D, 0x8C, 0x9B, 0xAA, 0xB9, 0xC8,
            0xD7, 0xE6, 0xF5, 0x04, 0x5F, 0x6E, 0x7D, 0x8C,
            0x9B, 0xAA, 0xB9, 0xC8, 0xD7, 0xE6, 0xF5, 0x04,
            0x5F, 0x6E, 0x7D, 0x8C, 0x9B, 0xAA, 0xB9, 0xC8 };

    private ILogger _Log = null!;
    private Laverna _Laverna = null!;

    private string _Filename = "";
    private FileStream _FileHandle = null;

    private T _Header = null!;
    private DFCache<DFCluster> _Cache;

    #region Properties
    public virtual string DefaultExtension { get => ".dfs"; }

    /// <summary>
    /// Wahr wenn der Container geöffnet ist, sonst Falsch.
    /// </summary>
    public bool isOpen { get => (_FileHandle != null); }

    /// <summary>
    /// Liefert den Datennamen des Containers zurück
    /// </summary>
    public string Filename { get => _Filename; protected set => _Filename = value; }

    /// <summary>
    /// Liefert den Header zurück.
    /// </summary>
    public IHeader Header { get => _Header; }

    public FileStream FileHandle { get => _FileHandle; } 

    public Laverna Laverna { get => _Laverna; }

    public ILogger Log { get => _Log; }

    protected DFCache<DFCluster> Cache { get => _Cache; }
    #endregion

    #region FS-Helpers
    internal void ClearCluster(int Cluster)
    {
        try
        {
            long Offset = _Header.FirstClusterOffset() + (Cluster * _Header.ClusterSize);
            byte[] Buffer = new byte[_Header.ClusterSize];         

            if (_FileHandle.Seek(Offset, SeekOrigin.Begin) == Offset)
                _FileHandle.Write(Buffer, 0, Buffer.Length);
        }
        catch (IOException IOe)
        {
            //Log.WriteException(IOe);
            throw new DFException("could not clear cluster", IOe);
        }
    }
    #endregion

    #region FS-Methods
    /// <summary>
    /// Erstellt eine neues Dateisystem
    /// </summary>
    /// <param name="ForceOverwrite">Überschreibt eine vorhandene Datei wenn wahr.</param>
    /// <param name="VolumeName">Der Name des Dateisystems</param>
    /// <returns>Wahr wenn erfolgreich, sonst Falsch</returns>
    public void Create(bool ForceOverwrite = false, string VolumeName = "")
    {
        Close();

        _Filename = Filename.RemoveExtension();
        if ((File.Exists(this.Filename + DefaultExtension)) & (!ForceOverwrite))
            throw new DFException("file already exists");

        try
        {
            _FileHandle = File.Open(this.Filename + DefaultExtension, FileMode.Create);

            Format(VolumeName);

            _FileHandle.Flush();
        }
        catch (IOException IOe)
        {
            //Log.WriteException(IOe);
            throw new DFException("could not create file.", IOe);
        }
    }

    /// <summary>
    /// Öffnet ein vorhandenes Verzeichnis
    /// </summary>
    /// <returns>Wahr wenn erfolgreich, sonst Falsch</returns>
    public void Open()
    {
        Close();

        _Filename = Filename.RemoveExtension();
        if (!File.Exists(Filename + DefaultExtension))
            throw new DFException("file not found");

        try
        {
            _FileHandle = File.Open(Filename + DefaultExtension, FileMode.Open);

            Reload();

            _FileHandle.Flush();
        }
        catch (IOException IOe)
        {
            //IDXLogs.WriteException(IOe);
            throw new DFException("could not open file", IOe);
        }
    }

    /// <summary>
    /// Schreibt eventuelle Änderungen in das Dateisystem zurück
    /// </summary>
    /// <returns>Wahr wenn erfolgreich, sonst Falsch</returns>
    public virtual void Flush()
    {
        if (isOpen)
        {
            if (_Header != null)
                _Header.Write();

            if (_Cache != null)
                _Cache.Flush();
        }
    }

    /// <summary>
    /// Schliesst das Dateisystem
    /// </summary>
    /// <returns>Wahr wenn erfolgreich, sonst Falsch</returns>
    public virtual void Close()
    {
        if (_FileHandle != null)
        {
            try
            {
                Flush();

                _FileHandle.Close();
                _FileHandle = null;
            }
            catch (IOException IOe)
            {
                //IDXLogs.WriteException(IOe);
                throw new DFException("could not close file", IOe);
            }
            finally
            {
                Dispose();
            }
        }
    }

    /// <summary>
    /// Lädt den Datenbestand erneut vom Dateisystem.
    /// </summary>
    /// <returns>Wahr wenn erfolgreich, sonst Falsch</returns>
    public virtual void Reload()
    {
        _Header.Read();
        _Cache = new DFCache<DFCluster>(this as IDF, 64);
    }

    /// <summary>
    /// Weist dem Dateisystem einen neuen Namen zu
    /// </summary>
    /// <param name="Name">Der neue Name für das Dateisystem</param>
    /// <returns>Wahr wenn erfolgreich, sonst Falsch</returns>
    public void Label(string Name)
    {
        if (isOpen)
            _Header.Name = Name.ToUpper();

        Flush();
    }

    /// <summary>
    /// Liefert den Name des Dateisystems zurück
    /// </summary>
    /// <returns>Der Name des Dateisystems</returns>
    public string GetLabel()
    {
        if (isOpen)
            if (_Header.Name.TrimEnd() != "")
                return _Header.Name;
            else
                return Filename;
        else
            return Filename;
    }

    /// <summary>
    /// Formatiert das Dateisystem und legt das Hauptverzeichnis an.
    /// </summary>
    /// <param name="Name">Der Name des Dateisystems</param>
    /// <returns>Wahr wenn erfolgreich, sonst Falsch</returns>
    public virtual void Format(string Name)
    {
        if (isOpen)
        {
            _Header = new DFHeader(this as IDF) as T;
            _Header.Name = Name;

            _Cache = new DFCache<DFCluster>(this as IDF, 64);
            Flush();

            try
            {
                _FileHandle.SetLength(_Header.FirstClusterOffset());
            }
            catch (IOException IOe)
            {
                //IDXLogs.WriteException(IOe);
                throw new DFException("couldn't clear Cluster", IOe);
            }
        }
    }

    public ICryptoTransform CreateEncryptor()
    {
        using (var myAes = Aes.Create())
        {
            myAes.BlockSize = 128;
            myAes.KeySize = 128;
            myAes.Padding = PaddingMode.Zeros;
            myAes.Mode = CipherMode.CBC;
            myAes.FeedbackSize = 128;

            return myAes.CreateEncryptor(_DF_KEY, _DF_IV);
        }
    }

    public ICryptoTransform CreateDecryptor()
    {
        using (var myAes = Aes.Create())
        {
            myAes.BlockSize = 128;
            myAes.KeySize = 128;
            myAes.Padding = PaddingMode.Zeros;
            myAes.Mode = CipherMode.CBC;
            myAes.FeedbackSize = 128;

            return myAes.CreateEncryptor(_DF_KEY, _DF_IV);
        }
    }

    #endregion

    public DF(string Filename)
    {
        _Filename = Filename;

        _Log = Hosting.Hosting.CreateDefaultLogger<DF<T>>();
        _Laverna = new Laverna(_DF_KEY, _DF_IV);
    }

    public void Dispose()
    {
        Flush();
        Close();
    }
}


