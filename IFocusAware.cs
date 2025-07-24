using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace desktop_assistant
{
    public interface IFocusAware
    {
        bool HasFocus { get; }
    }
}

