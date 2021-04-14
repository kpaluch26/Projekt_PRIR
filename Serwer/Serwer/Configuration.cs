using System;
using System.Collections.Generic;
using System.Text;

namespace Serwer
{
    class Configuration
    {
        private int PORT;
        private int BUFFER;
        private string IP;

        public Configuration(int _p, int _b, string _i)
        {
            this.PORT = _p;
            this.BUFFER = _b;
            this.IP = _i;
        }

        public int GetPort()
        {
            return PORT;
        }

        public int GetBuffer()
        {
            return BUFFER;
        }

        public string GetIP()
        {
            return IP;
        }
    }
}
