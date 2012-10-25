using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using AsyncSocketEventOverlappedPool;
using System.Diagnostics.Contracts;

namespace ReactiveTCPLibrary.Packet
{

    /// <summary>
    /// A simple Serializer for string with encoding setting.
    /// </summary>
    public class SimpleStringSerializer:ISerializer<string>
    {
        
        public Encoding CurrentEncoding { get; set; }


        public SimpleStringSerializer():this(Encoding.UTF8)
        { 
        
        }
        public SimpleStringSerializer(Encoding encoding ) 
        {
            CurrentEncoding = encoding;
        }

        public void WriteObject(string obj, System.IO.Stream target)
        {
            using (var token=new Disposable<StreamWriter>(sw => sw.Flush(), new StreamWriter(target,CurrentEncoding)))
            {
                token.StateObject.Write(obj);
            }
            
        }
        [ContractInvariantMethod()]
        void CheckEncoding()
        {
            //Contract.Invariant(CurrentEncoding != null);
        }

        public string Read(System.IO.Stream target)
        {
            using (var token = new Disposable<StreamReader>(_ => { }, new StreamReader(target, CurrentEncoding)))
            {
               var rval= token.StateObject.ReadToEnd();
               return rval;
            }
        }
    }
}
