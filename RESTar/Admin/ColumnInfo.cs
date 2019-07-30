namespace RESTar.Admin
{
    /// <summary>
    /// Contains information about a column on which an index is registered.
    /// </summary>
    public class ColumnInfo
    {
        /// <summary>
        /// The name of the column (property name)
        /// </summary>
        public string Name { get; }

        /// <summary>
        /// Is this index descending? (otherwise ascending)
        /// </summary>
        public bool Descending { get; }

        /// <summary>
        /// Creates a new ColumnInfo instance
        /// </summary>
        public ColumnInfo(string name, bool descending)
        {
            Name = name.Trim();
            Descending = descending;
        }

        /// <summary>
        /// Creates a new ColumnInfo from a tuple describing a column name and direction
        /// </summary>
        /// <param name="column"></param>
        public static implicit operator ColumnInfo((string Name, bool Descending) column)
        {
            return new ColumnInfo(column.Name, column.Descending);
        }
    }
}