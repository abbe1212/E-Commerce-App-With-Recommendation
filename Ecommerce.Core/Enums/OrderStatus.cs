using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Ecommerce.Core.Enums
{
    public enum OrderStatus
    {
        pending,
        processing,
        confirmed,   
        shipped,     
        delivered,   
        cancelled,   
        returned     
    }
}
