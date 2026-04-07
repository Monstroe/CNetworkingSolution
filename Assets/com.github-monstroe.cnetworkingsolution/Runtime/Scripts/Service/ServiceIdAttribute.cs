using System;

[AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = false)]
public sealed class ServiceIdAttribute : Attribute
{
    public string ServiceId { get; }

    public ServiceIdAttribute(string serviceId)
    {
        ServiceId = serviceId;
    }
}
