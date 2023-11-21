/*
 * Copyright (c) 2014-2023 Anrá aka Nox
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
using Nox.Security;
using System.IO;
using System.Security.Cryptography;

namespace Nox.IO.DF;

//public interface IDFCRCSupport
//{
//    public uint CRC { get; }

//    public uint ReCRC();
//}

//public interface IDFCRCElement
//{
//    public void UserDataCRC(tinyCRC CRC);
//}

//public interface IDFContainer
//    : IDFCRCSupport
//{
//    bool Dirty { get; }

//    int UserDataSize();
//    int ContainerSize();

//    void Read();
//    void ReadUserData(BinaryReader Reader);

//    void Write();
//    void WriteUserData(BinaryWriter Writer);
////}
//public interface IDFHeader
//    : IDFCRCSupport
//{
//    string Name { get; set; }

//    int ClusterMapCount { get; }
//    int ClusterSize { get; }

//    int ContainerOffset(int Index = 0);
//    int ClusterMapOffset(int Index = 0);
//    int FirstClusterOffset();

//    void Read();

//    void Write();

//    //abstract static IHeader Create(int ClusterSize = 8192);
//}

//public interface IDF
//{
//    #region Properties
//    FileStream FileHandle { get; }
//    Laverna Laverna { get; }

//    IDFHeader Header { get; }

//    ILogger Log { get; }

//    #endregion

//    void Flush();
//}