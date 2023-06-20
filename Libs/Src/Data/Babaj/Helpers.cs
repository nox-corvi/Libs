/*
 * Copyright (c) 2014-2020 Anrá aka Nox
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
using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Collections.ObjectModel;
using System.Data;
using System.Data.SqlClient;
using System.Data.SqlTypes;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Reflection.Emit;
using Nox;
using Nox.Data;
using Nox.Data.SqlServer;
using System.Reflection;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace Nox.Libs.Data.Babaj
{

    public class DataColMapDescriptor
    {
        public PropertyInfo Property;

        public string Name { get; set; }

        //public SqlType<T> Type { get; }

        //public static DbColMapDesc Parse(DatabaseColumnAttribute Attribute)
        //{
        //    var Result = new DbColMapDesc();

        //    switch (Attribute.SqlDbType)
        //    {
        //        case SqlDbType.BigInt:
        //            Result.Type = new SqlType<SqlInt64>()
        //            {

        //            };
        //            break;
        //        case DbTypeEnum.Bool:
        //            Result.Type = new SqlTypeBoolean(Attribute.DbAllowNull);
        //            break;
        //        case DbTypeEnum.Int:
        //            Result.Type = new SqlTypeInt(Attribute.DbAllowNull);
        //            break;
        //        case DbTypeEnum.Float:
        //            Result.Type = new SqlTypeFloat(Attribute.DbAllowNull);
        //            break;
        //        case DbTypeEnum.Double:
        //            Result.Type = new SqlTypeDouble(Attribute.DbAllowNull);
        //            break;
        //        case DbTypeEnum.Numeric:
        //            Result.Type = new SqlTypeNumeric(Attribute.DbAllowNull, Precision: Attribute.Precision, Scale: Attribute.Scale);
        //            break;
        //        case DbTypeEnum.Decimal:
        //            Result.Type = new SqlTypeDecimal(Attribute.DbAllowNull, Precision: Attribute.Precision, Scale: Attribute.Scale);
        //            break;
        //        case DbTypeEnum.Date:
        //            Result.Type = new SqlTypeNumeric(Attribute.DbAllowNull, Precision: Attribute.Precision, Scale: Attribute.Scale);
        //            break;
        //        case DbTypeEnum.Time:
        //            Result.Type = new SqlTypeTime(Attribute.DbAllowNull);
        //        case DbTypeEnum.DateTime:

        //        case DbTypeEnum.String:
        //    }
        //}
    }
}
