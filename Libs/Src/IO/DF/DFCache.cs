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

public class DFCache<T>
    : DFBase where T : DFCluster
{
    public const int FREE = -1;

    private T[] Clusters;
    private int Index;

    #region Cache-Methods
    /// <summary>
    /// Liefert die Position des gesuchten DataClusters zurück
    /// </summary>
    /// <param name="Cluster">Die Nummer des gesuchten DataClusters</param>
    /// <returns>Positiv wenn im Cache, sonst -1</returns>
    public int Exists(int Cluster)
    {
        //TODO:Optimieren der Cache-Suche ...
        int ItemsTested = Clusters.Length, i = Index;
        while (ItemsTested-- > 0)
        {
            if (Clusters[i] != null)
                if (Clusters[i].Cluster == Cluster)
                    return i;

            i = (++i) >= Clusters.Length ? 0 : i;
        }

        return -1;
    }

    /// <summary>
    /// Entfernt einen Element am angegebenen Index aus dem Cache und gibt das Feld frei
    /// </summary>
    /// <param name="Position">Der nullbasierte Index des zu entfernenden Elements</param>
    public void RemoveAt(int Position)
    {
        try
        {
            if (Clusters[Position] != null)
            {
                // Schreiben des Clusters auf die Festplatte
                if (Clusters[Position].Dirty)
                    Clusters[Position].Write();

                Clusters[Position] = null;
            }
        }
        catch (Exception)
        {
            // pass through
            throw;
        }
    }

    /// <summary>
    /// Sucht einen DataCluster im Cache und gibt ihn zurück
    /// </summary>
    /// <param name="Cluster">Die ClusterNummer die gesucht werden soll</param>
    /// <returns>FSDataCluster wenn vorhanden, sonst null</returns>
    public T Item(int Cluster)
    {
        int Position = Exists(Cluster);

        if (Position == FREE)
            return null;
        else
            return Clusters[Position];
    }

    /// <summary>
    /// Fügt einen DataCluster in den Cache ein
    /// </summary>
    /// <param name="Value">Der DataCluster der eingefügt werden soll</param>
    /// <returns>Wahr wenn erfolgreich, sonst Falsch</returns>
    public void Append(T Value)
    {
        try
        {
            RemoveAt(Index);

            Clusters[Index] = Value;
            Index = (++Index) >= Clusters.Length ? 0 : Index;
        }
        catch (DFException)
        {
            // pass through
            throw;
        }
    }

    /// <summary>
    /// Schreibt sämtliche gespeicherten DataClusters auf den Datenträger.
    /// </summary>
    /// <returns>Wahr wenn erfolgreich, sonst Falsch</returns>
    public void Flush()
    {
        try
        {
            int ItemsTested = Clusters.Length;
            for (int i = Index; ItemsTested > 0; ItemsTested--)
            {
                if (Clusters[i] != null)
                    Clusters[i].Write();

                i = (--i) < 0 ? Clusters.Length - 1 : i;
            }
        }
        catch (DFException)
        {
            // pass
            throw;
        }
    }
    #endregion

    public DFCache(IDF DF, int CacheSize)
        : base(DF)
    {
        Clusters = new T[CacheSize];
        for (int i = 0; i < CacheSize; i++)
            Clusters[i] = null;

        Index = 0;
    }
}