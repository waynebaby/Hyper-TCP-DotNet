using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;

namespace ReactiveTCPLibrary.Packet
{
    public interface ISerializer<T>
    {
        void WriteObject(T obj ,Stream target );
        T Read(Stream target );
    }
}
