using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PushSubscribe
{
    public static class SubscribeManager
    {
        private static Dictionary<string, int> _subscribelist = new Dictionary<string, int>();

        private static object obj = new object();

        public static List<string> SubscribeList
        {
            get
            {
                _subscribelist = _subscribelist.Where(d => d.Value > 0).ToDictionary(key => key.Key,
         value => value.Value);
                return _subscribelist.Keys.ToList();
            }
        }

        public static void Add(string student)
        {
            lock(obj)
            {
                if (_subscribelist.Keys.Contains(student))
                {
                    _subscribelist[student] = _subscribelist[student] + 1;
                }
                else
                {
                    _subscribelist.Add(student, 1);
                }
            }
        }
        public static void Remove(string student)
        {
            if (_subscribelist.Keys.Contains(student))
            {
                lock(obj)
                {
                    if (_subscribelist.Keys.Contains(student))
                    {
                        _subscribelist[student] = _subscribelist[student]-1;
                    }
                }
            }

        }
    }
}