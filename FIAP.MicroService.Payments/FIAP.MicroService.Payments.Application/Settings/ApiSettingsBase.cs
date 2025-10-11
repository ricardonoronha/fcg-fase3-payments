using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FIAP.MicroService.Payments.Application.Settings;

public class ApiSettingsBase
{
    [Required] public string BaseUrl { get; set; } = string.Empty;
    [Required] public string EndpointFormat { get; set; } = string.Empty;
}

public class UserApiSettings : ApiSettingsBase
{ }

public class GameApiSettings : ApiSettingsBase
{ }
