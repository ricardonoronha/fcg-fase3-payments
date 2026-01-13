using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FIAP.MicroService.Payments.Domain.Constants;

public enum CheckoutStatus
{
    Processing = 0,
    Finished = 1,
    Failed = 2
}
