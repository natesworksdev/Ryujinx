using Ryujinx.Common.Configuration.Hid;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Ryujinx.Ava.Ui.Models
{
    public record ControllerModel(ControllerType Type, string Name);
}
