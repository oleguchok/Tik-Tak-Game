using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Tik_Tak
{
    public enum Protocol
    {
        Disconnected = 0,
        Connected = 1,
        Move = 2,
        Pause = 3,
        Continue = 4,
        GameOver = 5
    }
}
