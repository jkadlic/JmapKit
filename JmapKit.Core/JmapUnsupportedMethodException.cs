namespace JmapKit;

/// <summary>
/// Thrown when constructing a <see cref="JmapMethodInvocation"/> for a <see cref="JmapMethod"/> that an
/// <see cref="IJmapObject"/> does not declare support for via <see cref="IJmapObject.SupportedMethods"/>.
/// </summary>
/// <param name="jmapName">The <see cref="IJmapObject.JmapName"/> of the object type the method was called on.</param>
/// <param name="method">The unsupported method.</param>
public class JmapUnsupportedMethodException(string jmapName, JmapMethod method)
    : JmapException($"'{jmapName}' does not support the '{method.MapToMethodString()}' method.");