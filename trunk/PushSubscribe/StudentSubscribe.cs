using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PushSubscribe
{
    public class StudentSubscribe : ISubscribe
    {
        public void Unsubscribe(List<string> students)
        {
            foreach (var student in students)
            {
                SubscribeManager.Remove(student);
            }
        }

        public void Subscribe(List<string> students)
        {
            Utilities.GetLoger().Info("ok!");
            foreach (var student in students)
            {
                Utilities.GetLoger().Info(student);
                SubscribeManager.Add(student);
            }
        }

    }
}