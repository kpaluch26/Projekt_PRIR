using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Serwer
{
    public sealed class SingletonSecured
    {
        private static SingletonSecured m_oInstance = null;
        private static readonly object m_oPadLock = new object();
        private int m_nCounter = 0;

        public static SingletonSecured Instance
        {
            get
            {
                lock (m_oPadLock)
                {
                    if (m_oInstance == null)
                    {
                        m_oInstance = new SingletonSecured();
                    }
                    return m_oInstance;
                }
            }
        }

        public Task AddTask(Task x)
        {
            return x;
        }

        private SingletonSecured()
        {
            m_nCounter = 1;
        }
    }
}
