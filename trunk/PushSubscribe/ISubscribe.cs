using System;
using System.Collections.Generic;
using System.Linq;
using System.ServiceModel;
using System.Text;
using System.Threading.Tasks;

namespace PushSubscribe
{
    [ServiceContract]
    public interface ISubscribe
    {
        [OperationContract(IsOneWay =true)]
        void Subscribe(List<string> students);

        [OperationContract(IsOneWay = true)]
        void Unsubscribe(List<string> students);


    }
}