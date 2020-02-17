using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Common;

namespace Client
{
    class ClientMessage : Message
    {
        public string DataAsString => Encoding.Unicode.GetString(Data);
    }
}
