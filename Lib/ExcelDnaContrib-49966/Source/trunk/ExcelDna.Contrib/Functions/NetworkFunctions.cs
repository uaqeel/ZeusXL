/*
  Copyright (C) 2009 Hayden Smith

  This software is provided 'as-is', without any express or implied
  warranty.  In no event will the authors be held liable for any damages
  arising from the use of this software.

  Permission is granted to anyone to use this software for any purpose,
  including commercial applications, and to alter it and redistribute it
  freely, subject to the following restrictions:

  1. The origin of this software must not be misrepresented; you must not
     claim that you wrote the original software. If you use this software
     in a product, an acknowledgment in the product documentation would be
     appreciated but is not required.
  2. Altered source versions must be plainly marked as such, and must not be
     misrepresented as being the original software.
  3. This notice may not be removed or altered from any source distribution.

  Hayden Smith
  hayden.smith@gmail.com
*/

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net.NetworkInformation;
using ExcelDna.Integration;

namespace ExcelDna.Contrib.Functions
{
    /// <summary>
    /// Contains network functions
    /// </summary>
    public class NetworkFunctions
    {
        private const string CATEGORY = "ExcelDna.Contrib.Network";

        /// <summary>
        /// Pings the Given address and returns the results of the Ping
        /// </summary>
        /// <param name="Address">Address to Ping</param>
        /// <returns>Address, Status and Roundtrip time as an array</returns>
        [ExcelFunction(Description="Pings the given address and returns the ping results",IsVolatile=true,IsThreadSafe=true,IsMacroType=true,Category=CATEGORY)]
        public static object[,] Ping([ExcelArgument(Description="Address to Ping",AllowReference=false)]string Address,[ExcelArgument(Description="Timeout to wait for a response",AllowReference=false)] int TimeOut)
        {
            Ping pinger = new Ping();

            if (TimeOut == 0) { TimeOut = 1000; }

            PingReply reply = pinger.Send(Address,TimeOut);

            object[,] ret = new object[3,2];

            ret[0, 0] = "Address";
            ret[1, 0] = "Status";
            ret[2, 0] = "Roundtrip took (ms)";

            ret[1, 1] = reply.Status.ToString();

            if (reply.Address != null)
            {
                ret[0, 1] = reply.Address.ToString();
            }
            else
            {
                ret[0, 1] = ExcelError.ExcelErrorNA;
            }

            if (reply.RoundtripTime != 0)
            {
                ret[2, 1] = reply.RoundtripTime.ToString();
            }
            else
            {
                ret[2, 1] = 0;
            }
            return ret;

        }
    }
}
