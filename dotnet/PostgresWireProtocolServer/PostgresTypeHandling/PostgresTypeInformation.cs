using System;
using System.Collections.Generic;
using System.Linq;

namespace PostgresWireProtocolServer.PostgresTypeHandling
{
    /// <summary>
    /// Type mapping for some important C# types to Postgres types. Postgres type sizes are included for the most important types.
    /// The full list of Postgres types can be found in <see cref="PostgresTypeOID"/>.
    /// The Postgres documentation has more information on the datatypes in <seealso cref="https://www.postgresql.org/docs/13/datatype.html"/>.
    /// </summary>
    internal class PostgresTypeInformation
    {
        /// <summary>
        /// The Postgres type identifier.
        /// </summary>
        public PostgresTypeOID TypeOid { get; }
        public uint Oid => (uint) TypeOid;
        /// <summary>
        /// Type size information. This value might be null if the size is value-dependant (as for varchars) or the size is not defined in the 
        /// static code of this class.
        /// </summary>
        public int? Size { get; }

        /// <summary>
        /// Flag indicating if this type information contains size information.
        /// </summary>
        public bool HasSizeInformation => Size != null;

        public static Dictionary<PostgresTypeOID, PostgresTypeInformation> PredefinedTypes = new Dictionary<PostgresTypeOID, PostgresTypeInformation>()
        {
            // numeric types
            { PostgresTypeOID.Int2, new PostgresTypeInformation(PostgresTypeOID.Int2, 32) }, 
            { PostgresTypeOID.Int4, new PostgresTypeInformation(PostgresTypeOID.Int4, 4) }, 
            { PostgresTypeOID.Int8, new PostgresTypeInformation(PostgresTypeOID.Int8, 8) }, 
            { PostgresTypeOID.Float4, new PostgresTypeInformation(PostgresTypeOID.Float4, 4) }, 
            { PostgresTypeOID.Float8, new PostgresTypeInformation(PostgresTypeOID.Float8, 8) }, 
            { PostgresTypeOID.Numeric, new PostgresTypeInformation(PostgresTypeOID.Numeric, null) }, 
            { PostgresTypeOID.Money, new PostgresTypeInformation(PostgresTypeOID.Money, null) }, 

            // boolean 
            { PostgresTypeOID.Bool, new PostgresTypeInformation(PostgresTypeOID.Bool, null) }, 

            // geometric types
            { PostgresTypeOID.Box, new PostgresTypeInformation(PostgresTypeOID.Box, null) }, 
            { PostgresTypeOID.Circle, new PostgresTypeInformation(PostgresTypeOID.Circle, null) }, 
            { PostgresTypeOID.Line, new PostgresTypeInformation(PostgresTypeOID.Line, null) }, 
            { PostgresTypeOID.LSeg, new PostgresTypeInformation(PostgresTypeOID.LSeg, null) }, 
            { PostgresTypeOID.Path, new PostgresTypeInformation(PostgresTypeOID.Path, null) }, 
            { PostgresTypeOID.Point, new PostgresTypeInformation(PostgresTypeOID.Point, null) }, 
            { PostgresTypeOID.Polygon, new PostgresTypeInformation(PostgresTypeOID.Polygon, null) }, 

            // character types
            { PostgresTypeOID.Char, new PostgresTypeInformation(PostgresTypeOID.Char, 1) }, 
            { PostgresTypeOID.Varchar, new PostgresTypeInformation(PostgresTypeOID.Varchar, null) }, 
            { PostgresTypeOID.Text, new PostgresTypeInformation(PostgresTypeOID.Text, null) }, 
            { PostgresTypeOID.Name, new PostgresTypeInformation(PostgresTypeOID.Name, null) }, 

            // text search types
            { PostgresTypeOID.TsVector, new PostgresTypeInformation(PostgresTypeOID.TsVector, null) }, 
            { PostgresTypeOID.TsQuery, new PostgresTypeInformation(PostgresTypeOID.TsQuery, null) }, 
            { PostgresTypeOID.Regconfig, new PostgresTypeInformation(PostgresTypeOID.Regconfig, null) }, 
            
            // binary data
            { PostgresTypeOID.Bit, new PostgresTypeInformation(PostgresTypeOID.Bit, null) }, 
            { PostgresTypeOID.Varbit, new PostgresTypeInformation(PostgresTypeOID.Varbit, null) }, 
            { PostgresTypeOID.Bytea, new PostgresTypeInformation(PostgresTypeOID.Bytea, null) }, 

            // date/time types
            { PostgresTypeOID.Date, new PostgresTypeInformation(PostgresTypeOID.Date, null) }, 
            { PostgresTypeOID.Time, new PostgresTypeInformation(PostgresTypeOID.Time, null) }, 
            { PostgresTypeOID.Timestamp, new PostgresTypeInformation(PostgresTypeOID.Timestamp, null) }, 
            { PostgresTypeOID.TimestampTz, new PostgresTypeInformation(PostgresTypeOID.TimestampTz, null) }, 
            { PostgresTypeOID.Interval, new PostgresTypeInformation(PostgresTypeOID.Interval, null) }, 
            { PostgresTypeOID.TimeTz, new PostgresTypeInformation(PostgresTypeOID.TimeTz, null) }, 
            { PostgresTypeOID.Abstime, new PostgresTypeInformation(PostgresTypeOID.Abstime, null) }, 

            // network types
            { PostgresTypeOID.Inet, new PostgresTypeInformation(PostgresTypeOID.Inet, null) }, 
            { PostgresTypeOID.Cidr, new PostgresTypeInformation(PostgresTypeOID.Cidr, null) }, 
            { PostgresTypeOID.Macaddr, new PostgresTypeInformation(PostgresTypeOID.Macaddr, null) }, 
            { PostgresTypeOID.Macaddr8, new PostgresTypeInformation(PostgresTypeOID.Macaddr8, null) }, 

            // internal types
            { PostgresTypeOID.BPChar, new PostgresTypeInformation(PostgresTypeOID.BPChar, 1) } /* the internal name for "Char" */ , 
            { PostgresTypeOID.Refcursor, new PostgresTypeInformation(PostgresTypeOID.Refcursor, null) }, 
            { PostgresTypeOID.Oidvector, new PostgresTypeInformation(PostgresTypeOID.Oidvector, null) }, 
            { PostgresTypeOID.Int2vector, new PostgresTypeInformation(PostgresTypeOID.Int2vector, null) }, 
            { PostgresTypeOID.Cid, new PostgresTypeInformation(PostgresTypeOID.Cid, null) }, 
            { PostgresTypeOID.Oid, new PostgresTypeInformation(PostgresTypeOID.Oid, 4) }, 
            { PostgresTypeOID.Tid, new PostgresTypeInformation(PostgresTypeOID.Tid, null) }, 
            { PostgresTypeOID.Xid, new PostgresTypeInformation(PostgresTypeOID.Xid, null) }, 
            { PostgresTypeOID.Regtype, new PostgresTypeInformation(PostgresTypeOID.Regtype, null) }, 

            // special types
            { PostgresTypeOID.Uuid, new PostgresTypeInformation(PostgresTypeOID.Uuid, null) }, 
            { PostgresTypeOID.Xml, new PostgresTypeInformation(PostgresTypeOID.Xml, null) }, 
            { PostgresTypeOID.Json, new PostgresTypeInformation(PostgresTypeOID.Json, null) }, 
            { PostgresTypeOID.Jsonb, new PostgresTypeInformation(PostgresTypeOID.Jsonb, null) }, 
            { PostgresTypeOID.JsonPath, new PostgresTypeInformation(PostgresTypeOID.JsonPath, null) }, 
            { PostgresTypeOID.Unknown, new PostgresTypeInformation(PostgresTypeOID.Unknown, null) }
        };

        /// <summary>
        /// Mapping from .NET CLR types to Postgres types. Please be aware  the number of defined mappings is small.
        /// </summary>
        /// <typeparam name="System.Type">CLR type.</typeparam>
        /// <typeparam name="PostgresTypeInformation">Postgres type information.</typeparam>
        public static Dictionary<Type, PostgresTypeInformation> Mapping = new Dictionary<System.Type, PostgresTypeInformation>()
        {
            { typeof(short), new PostgresTypeInformation(PostgresTypeOID.Int2, 32) }, 
            { typeof(int), new PostgresTypeInformation(PostgresTypeOID.Int4, 4) }, 
            { typeof(long), new PostgresTypeInformation(PostgresTypeOID.Int8, 8) }, 
            { typeof(float), new PostgresTypeInformation(PostgresTypeOID.Float4, 4) }, 
            { typeof(double), new PostgresTypeInformation(PostgresTypeOID.Float8, 8) }, 
            { typeof(bool), new PostgresTypeInformation(PostgresTypeOID.Bool, null) }, 
            { typeof(char), new PostgresTypeInformation(PostgresTypeOID.Char, 1) }, 
            { typeof(string), new PostgresTypeInformation(PostgresTypeOID.Varchar, null) }, 
            { typeof(DateTime), new PostgresTypeInformation(PostgresTypeOID.Timestamp, null) }
        };


        /// <summary>
        /// Returns a list of CLR types for which a mapping to a Postgres type is defined.
        /// </summary>
        /// <returns>CLR types for which a mapping is defined.</returns>
        public static IEnumerable<Type> PredefinedTypeNames => Mapping.Select(pair => pair.Key);

        private static string _typeNameByReflection;
        public static string ExtendedPropertyName
        {
            get 
            {
                if (_typeNameByReflection == null)
                {
                    _typeNameByReflection = System.Reflection.MethodBase.GetCurrentMethod().DeclaringType.Name;
                }
                return _typeNameByReflection;
            }
        } 

        public PostgresTypeInformation(PostgresTypeOID typeOid, int? size)
        {
            TypeOid = typeOid;
            Size = size;
        }
    }
}