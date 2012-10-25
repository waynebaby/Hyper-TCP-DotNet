using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ReactiveTCPLibrary.Packet
{
    public class NodoSerializer:ISerializer< ArraySegment<byte>>
    {

        public void WriteObject(ArraySegment<byte> obj, System.IO.Stream target)
        {
            target.Write(obj.Array, obj.Offset, obj.Count);
        }

        public ArraySegment<byte> Read(System.IO.Stream target)
        {
            var array=new byte[target.Length ];
            target.Read(array, 0, array.Length);
            return new ArraySegment<byte>(array, 0, array.Length);
        }
    }
}
