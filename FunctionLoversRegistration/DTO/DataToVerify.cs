﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FunctionLoversRegistration.DTO
{
    public class DataToVerify
    {
        public DataToVerify(string emailAddress)
        {
            EmailAddress = emailAddress;
        }
        public string EmailAddress { get;}
    }
}
