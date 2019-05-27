namespace RESTar.Meta
{
    /// <summary>
    /// Represents a getter for a property. This is an open delgate, taking a 
    /// target object.
    /// </summary>
    /// <param name="target">The target of the open delegate invocation</param>
    /// <returns>The value of the property</returns>
    public delegate dynamic Getter(object target);

    /// <summary>
    /// Represents a setter for a property. This is an open delgate, taking a 
    /// target object and a value to assign to the property.
    /// </summary>
    /// <param name="target">The target of the open delegate invocation</param>
    /// <param name="value">The value to set the property to</param>
    public delegate void Setter(object target, dynamic value);

    /// <summary>
    /// Creates a new instance of some object type
    /// </summary>
    /// <returns></returns>
    public delegate object Constructor();

    /// <summary>
    /// Creates a new instance of some object type
    /// </summary>
    /// <returns></returns>
    public delegate T Constructor<out T>();
}