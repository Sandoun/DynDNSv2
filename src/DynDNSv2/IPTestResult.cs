using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace DynDNSv2;
internal record IPTestResult
{

    public bool WasChanged { get; set; }    

    public IPAddress? UpdatedIP { get; set; }    

}
