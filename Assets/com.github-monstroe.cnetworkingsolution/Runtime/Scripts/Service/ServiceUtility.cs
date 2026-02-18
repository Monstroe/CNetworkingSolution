using System;
using System.Security.Cryptography;
using System.Text;

public class ServiceUtility
{
    protected uint GenerateServiceId(Type serviceType)
    {
        using MD5 md5 = MD5.Create();
        byte[] hash = md5.ComputeHash(Encoding.UTF8.GetBytes(serviceType.Name));
        return BitConverter.ToUInt32(hash, 0);
    }
}
