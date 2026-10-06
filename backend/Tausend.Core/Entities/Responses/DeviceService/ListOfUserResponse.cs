using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Web;
using Tausend.Core.Entities.Models;
using Tausend.Core.Responses;

namespace Tausend.Core.Entities.Responses.DeviceService
{
    [DataContract(Namespace = "http://Tausend.Wearelomo.com")]
    public class ListOfUserResponse : BaseResponse
    {
        [DataMember]
        public List<User> Users { get; set; }

        public ListOfUserResponse(List<User> users)
        {
            Users = users;
        }
    }
}