using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ReactiveTCPLibrary.ByteSegmentLocators
{
   public class NewArrayByteSegmentLocator: ByteSegmentLocatorBase
    {



        protected override void Locate(int count, out byte[] array, out int offset)
        {
            array = new byte[count];
            offset = 0;
        }

        public override void Dispose()
        {
           
        }
    }
}
