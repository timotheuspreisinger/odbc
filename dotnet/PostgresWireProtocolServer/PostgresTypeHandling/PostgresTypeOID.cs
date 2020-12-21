namespace PostgresWireProtocolServer.PostgresTypeHandling
{
    /// <summary>
    ///  Postgres' internal integer OIDs. <seealso cref="https://github.com/npgsql/npgsql/blob/51c31be6270b0f9108b1dec97ea60470443a0d1e/src/Npgsql/PostgresTypeOIDs.cs"/>.
    /// </summary>
    public enum PostgresTypeOID
    {
         // Numeric
        Int8        = 20,
        Float8      = 701,
        Int4        = 23,
        Numeric     = 1700,
        Float4      = 700,
        Int2        = 21,
        Money       = 790,

        // Boolean
        Bool        = 16,

        // Geometric
        Box         = 603,
        Circle      = 718,
        Line        = 628,
        LSeg        = 601,
        Path        = 602,
        Point       = 600,
        Polygon     = 604,

        // Character
        BPChar      = 1042,
        Text        = 25,
        Varchar     = 1043,
        Name        = 19,
        Char        = 18,

        // Binary data
        Bytea       = 17,

        // Date/Time
        Date        = 1082,
        Time        = 1083,
        Timestamp   = 1114,
        TimestampTz = 1184,
        Interval    = 1186,
        TimeTz      = 1266,
        Abstime     = 702,

        // Network address
        Inet        = 869,
        Cidr        = 650,
        Macaddr     = 829,
        Macaddr8    = 774,

        // Bit string
        Bit         = 1560,
        Varbit      = 1562,

        // Text search
        TsVector    = 3614,
        TsQuery     = 3615,
        Regconfig   = 3734,

        // UUID
        Uuid        = 2950,

        // XML
        Xml         = 142,

        // JSON
        Json        = 114,
        Jsonb       = 3802,
        JsonPath    = 4072,

        // Internal
        Refcursor   = 1790,
        Oidvector   = 30,
        Int2vector  = 22,
        Oid         = 26,
        Xid         = 28,
        Cid         = 29,
        Regtype     = 2206,
        Tid         = 27,

        // Special
        Unknown     = 705
    }
}